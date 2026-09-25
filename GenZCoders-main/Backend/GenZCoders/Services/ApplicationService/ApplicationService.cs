using GenZCoders.Extensions;
using GenZCoders.DTOs.ApplicationDto;
using GenZCoders.DTOs.ExamsDto;
using GenZCoders.Models;
using GenZCoders.Repos.ApplicationRepo;
using GenZCoders.Repos.CourseRoundRepo;
using GenZCoders.Repos.ExamRepo;
using GenZCoders.Repos.MediaRepo;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services.ApplicationService
{
    public class ApplicationService : IApplicationService
    {
        private const long DefaultStatusId = 14;

        private readonly IApplicationRepo _repo;
        private readonly IMediaRepository _mediaRepo;
        private readonly ICourseRoundRepo _courseRoundRepo;
        private readonly IExamQuestionBankRepo _examBankRepo;
        private readonly IExamQuestionRepo _examQuestionRepo;
        private readonly IStudentExamAnswerRepo _studentAnswerRepo;
        private readonly SchoolDbContext _db;

        public ApplicationService(
            IApplicationRepo repo,
            IMediaRepository mediaRepo,
            ICourseRoundRepo courseRoundRepo,
            IExamQuestionBankRepo examBankRepo,
            IExamQuestionRepo examQuestionRepo,
            IStudentExamAnswerRepo studentAnswerRepo,
            SchoolDbContext db)
        {
            _db = db;
            _repo = repo;
            _mediaRepo = mediaRepo;
            _courseRoundRepo = courseRoundRepo;
            _examBankRepo = examBankRepo;
            _examQuestionRepo = examQuestionRepo;
            _studentAnswerRepo = studentAnswerRepo;
        }

        public async Task<ApplicationDto> CreateAsync(CreateApplicationDto dto)
        {
            var courseRound = await _courseRoundRepo.GetByIdAsync(dto.CourseRoundId)
                ?? throw new KeyNotFoundException("Course round not found.");

            if (await _db.Applications.AnyAsync(a => a.AccountId == dto.AccountId && a.CourseRoundId == dto.CourseRoundId))
                throw new InvalidOperationException("You have already applied to this course round.");

            if (courseRound.MaxStudents is > 0)
            {
                var enrolled = await _db.Applications.CountAsync(a =>
                    a.CourseRoundId == dto.CourseRoundId && a.StatusId == EnrollmentExtensions.PaidStatusId);

                if (enrolled >= courseRound.MaxStudents)
                    throw new InvalidOperationException("This course round is full.");
            }

            // The application and its exam answers are saved together or not at all,
            // otherwise a rejected answer set leaves an application behind and blocks a retry.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            long statusId = courseRound.AutomatedWorkFlowJump == 15 ? EnrollmentExtensions.AcceptedStatusId : DefaultStatusId;

            var application = new Application
            {
                CourseRoundId = dto.CourseRoundId,
                AccountId = dto.AccountId,
                ApplicationDate = DateTime.UtcNow,
                StatusId = statusId,
                Answer1 = dto.Answer1,
                Answer2 = dto.Answer2,
                Answer3 = dto.Answer3,
                Answer4 = dto.Answer4,
                Answer5 = dto.Answer5,
                Answer6 = dto.Answer6,
                Answer7 = dto.Answer7,
                Answer8 = dto.Answer8,
                Answer9 = dto.Answer9,
                Answer10 = dto.Answer10
            };

            await _repo.AddAsync(application);
            await _repo.SaveChangesAsync();

            if (dto.ExamAnswers?.Any() == true)
            {
                await SaveExamAnswersAsync(dto.AccountId, dto.CourseRoundId, dto.ExamAnswers);
            }

            await transaction.CommitAsync();

            return (await GetByIdAsync(application.Id))!;
        }

        public async Task<List<ExamQuestionDto>> GetExamQuestionsAsync(long courseRoundId)
        {
            var bankEntries = await _examBankRepo.GetByCourseRoundIdAsync(courseRoundId);

            if (!bankEntries.Any()) return new List<ExamQuestionDto>();

            var questionIds = bankEntries
                .Where(b => b.QuestionId.HasValue)
                .Select(b => b.QuestionId!.Value)
                .ToList();

            var questions = await _examQuestionRepo.GetByIdsAsync(questionIds);
            var questionDict = questions.ToDictionary(q => q.Id);

            return bankEntries
                .Where(b => b.QuestionId.HasValue && questionDict.ContainsKey(b.QuestionId.Value))
                .Select(b =>
                {
                    var q = questionDict[b.QuestionId!.Value];
                    return new ExamQuestionDto
                    {
                        Id = q.Id,
                        QuestionTitle = q.QuestionTitle,
                        Choice1 = q.Choice1,
                        Choice2 = q.Choice2,
                        Choice3 = q.Choice3,
                        Choice4 = q.Choice4
                    };
                })
                .ToList();
        }

        public async Task<bool> SubmitExamAnswersAsync(long accountId, long courseRoundId, List<ExamAnswerItemDto> answers)
        {
            await SaveExamAnswersAsync(accountId, courseRoundId, answers);
            return true;
        }

        private async Task SaveExamAnswersAsync(long accountId, long courseRoundId, List<ExamAnswerItemDto> answers)
        {
            var bankEntries = await _examBankRepo.GetByCourseRoundIdAsync(courseRoundId);
            if (!bankEntries.Any())
                throw new InvalidOperationException("No exam questions are configured for this course round.");

            // QuestionId -> QuestionbankId, only for questions that belong to this round's exam.
            var questionToBank = bankEntries
                .Where(b => b.QuestionId.HasValue && b.Id != 0)
                .GroupBy(b => b.QuestionId!.Value)
                .ToDictionary(g => g.Key, g => (long)g.First().Id);

            var bankIds = questionToBank.Values.ToList();
            var alreadySubmitted = await _db.StudentExamAnswers.AnyAsync(sa =>
                sa.AccountId == accountId && sa.QuestionbankId.HasValue && bankIds.Contains(sa.QuestionbankId.Value));

            if (alreadySubmitted)
                throw new InvalidOperationException("Exam answers were already submitted for this course round.");

            var validAnswers = answers
                .Where(a => questionToBank.ContainsKey(a.QuestionId))
                .GroupBy(a => a.QuestionId)
                .Select(g => g.First())
                .ToList();

            if (validAnswers.Count == 0)
                throw new ArgumentException("No valid answers were submitted for this course round's exam.");

            var questions = await _examQuestionRepo.GetByIdsAsync(validAnswers.Select(a => a.QuestionId).ToList());
            var questionDict = questions.ToDictionary(q => q.Id);

            var studentAnswers = validAnswers.Select(a =>
            {
                var question = questionDict.GetValueOrDefault(a.QuestionId);
                var isCorrect = question != null &&
                    string.Equals(a.ChoosedAnswer, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

                return new StudentExamAnswer
                {
                    AccountId = accountId,
                    ExamQuestionId = a.QuestionId,
                    QuestionbankId = questionToBank[a.QuestionId],
                    ChoosedAnswer = a.ChoosedAnswer,
                    Score = isCorrect
                };
            }).ToList();

            await _studentAnswerRepo.AddRangeAsync(studentAnswers);
            await _studentAnswerRepo.SaveChangesAsync();
        }

        public async Task<ApplicationDto?> GetByIdAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;

            var media = await _mediaRepo.GetByOwnerAsync("Application", id);

            var dto = await MapToDtoAsync(entity);

            dto.Media = media.Any()
                ? new MediaForApplicationDto { FilePath = media.First().FilePath }
                : null;

            return dto;
        }

        public async Task<List<ApplicationDto>> GetAllAsync(long? accountId = null)
        {
            var apps = (await _repo.GetAllAsync())
                .Where(a => accountId == null || a.AccountId == accountId)
                .ToList();
            var appIds = apps.Select(a => a.Id).ToList();
            var medias = await _mediaRepo.GetByOwnerAsync("Application", appIds);

            // Avoid parallel DB operations on the same scoped DbContext instance.
            // Map each application sequentially to prevent "second operation started" errors.
            var dtos = new List<ApplicationDto>(apps.Count);
            foreach (var app in apps)
            {
                dtos.Add(await MapToDtoAsync(app));
            }

            foreach (var dto in dtos)
            {
                var media = medias.FirstOrDefault(m => m.TableId == dto.Id);
                if (media != null)
                {
                    dto.Media = new MediaForApplicationDto { FilePath = media.FilePath };
                }
            }

            return dtos;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            _repo.Remove(entity);
            return await _repo.SaveChangesAsync();
        }

        public async Task<bool> PatchStatusAsync(long id, PatchApplicationStatusDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.StatusId = dto.StatusId;
            return await _repo.SaveChangesAsync();
        }

        public async Task<bool> PatchCourseRoundAsync(long id, PatchApplicationCourseRoundDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            var roundExists = await _courseRoundRepo.GetByIdAsync(dto.CourseRoundId) != null;
            if (!roundExists)
                throw new ArgumentException("Course round not found");

            entity.CourseRoundId = dto.CourseRoundId;
            return await _repo.SaveChangesAsync();
        }

        private async Task<ApplicationDto> MapToDtoAsync(Application a)
        {
            var dto = new ApplicationDto
            {
                Id = a.Id,
                CourseRoundId = a.CourseRoundId,
                AccountId = a.AccountId,
                ApplicationDate = a.ApplicationDate,
                Status = a.Status?.StatusName ?? "Unknown",
                FullNameEn = a.Account?.FullNameEn,
                Email = a.Account?.Email,
                Phone = a.Account?.Phone,
                Answer1 = a.Answer1,
                Answer2 = a.Answer2,
                Answer3 = a.Answer3,
                Answer4 = a.Answer4,
                Answer5 = a.Answer5,
                Answer6 = a.Answer6,
                Answer7 = a.Answer7,
                Answer8 = a.Answer8,
                Answer9 = a.Answer9,
                Answer10 = a.Answer10
            };

            // Fetch answers by AccountId + CourseRound (through QuestionbankId)
            var bankEntries = await _examBankRepo.GetByCourseRoundIdAsync(a.CourseRoundId);
            var bankIds = bankEntries.Select(b => (long)b.Id).ToList();

            if (bankIds.Any())
            {
                // Get all student answers for this account linked to these bank entries
                var allAnswers = await _studentAnswerRepo.GetByAccountIdAsync(a.AccountId);
                var studentAnswers = allAnswers
                    .Where(sa => sa.QuestionbankId.HasValue && bankIds.Contains(sa.QuestionbankId.Value))
                    .ToList();

                if (studentAnswers.Any())
                {
                    var questionIds = studentAnswers
                        .Where(sa => sa.ExamQuestionId.HasValue)
                        .Select(sa => sa.ExamQuestionId!.Value)
                        .ToList();

                    var questions = await _examQuestionRepo.GetByIdsAsync(questionIds);
                    var questionDict = questions.ToDictionary(q => q.Id);

                    dto.ExamAnswers = studentAnswers.Select(sa => new StudentExamAnswerDto
                    {
                        QuestionId = sa.ExamQuestionId,
                        QuestionTitle = sa.ExamQuestionId.HasValue && questionDict.ContainsKey(sa.ExamQuestionId.Value)
                            ? questionDict[sa.ExamQuestionId.Value].QuestionTitle
                            : null,
                        ChoosedAnswer = sa.ChoosedAnswer,
                        IsCorrect = sa.Score
                    }).ToList();
                }
            }

            return dto;
        }
    }
}