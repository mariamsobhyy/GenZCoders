using GenZCoders.Extensions;
using Microsoft.EntityFrameworkCore;
using GenZCoders.DTOs.CourseRoundDto;
using GenZCoders.DTOs.ExamsDto;
using GenZCoders.Models;
using GenZCoders.Repos.CourseRoundRepo;
using GenZCoders.Repos.ExamRepo;

namespace GenZCoders.Services.CourseRoundService
{
    public class CourseRoundService : ICourseRoundService
    {
        private readonly ICourseRoundRepo _repo;
        private readonly IExamQuestionRepo _examQuestionRepo;
        private readonly IExamQuestionBankRepo _examBankRepo;
        private readonly SchoolDbContext _db;

        public CourseRoundService(
            ICourseRoundRepo repo,
            IExamQuestionRepo examQuestionRepo,
            IExamQuestionBankRepo examBankRepo,
            SchoolDbContext db)
        {
            _db = db;
            _repo = repo;
            _examQuestionRepo = examQuestionRepo;
            _examBankRepo = examBankRepo;
        }

        // ========== GET ALL ==========
        public async Task<List<CourseRoundDto>> GetAllAsync()
        {
            var rounds = await _repo.GetAllAsync();

            var enrolledCounts = await _db.Applications
                .Where(a => a.StatusId == EnrollmentExtensions.PaidStatusId)
                .GroupBy(a => a.CourseRoundId)
                .Select(g => new { RoundId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoundId, x => x.Count);

            var result = new List<CourseRoundDto>();

            foreach (var r in rounds)
            {
                var examQuestions = await GetExamQuestionsForRoundAsync(r.Id);

                result.Add(new CourseRoundDto
                {
                    Id = r.Id,
                    CourseId = r.CourseId,
                    RoundNumber = r.RoundNumber,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    MinStudents = r.MinStudents,
                    MaxStudents = r.MaxStudents,
                    Price = r.Price,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status.StatusName,
                    StatusId = r.StatusId,
                    CourseName = r.Course?.Title,
                    EnrolledCount = enrolledCounts.GetValueOrDefault(r.Id),
                    CourseRoundGroupId = r.CourseRoundGroupId,

                    // Legacy
                    Question1 = r.Question1,
                    Question2 = r.Question2,
                    Question3 = r.Question3,
                    Question4 = r.Question4,
                    Question5 = r.Question5,
                    Question6 = r.Question6,
                    Question7 = r.Question7,
                    Question8 = r.Question8,
                    Question9 = r.Question9,
                    Question10 = r.Question10,

                    InstructorId = r.CourseRoundInstructors
                        .OrderByDescending(x => x.AssignedDate)
                        .Select(x => x.InstructorId)
                        .FirstOrDefault(),

                    InstructorName = r.CourseRoundInstructors
                        .OrderByDescending(x => x.AssignedDate)
                        .Select(x => x.Instructor.FullNameEn)
                        .FirstOrDefault(),

                    Groups = r.GroupedRounds.Select(gr => new CourseRoundGroupItemDto
                    {
                        Id = gr.Id,
                        RoundNumber = gr.RoundNumber,
                        StartDate = gr.StartDate,
                        EndDate = gr.EndDate,
                        Price = gr.Price
                    }).ToList(),

                    // NEW
                    ExamQuestions = examQuestions
                });
            }

            return result;
        }

        public async Task<CourseRoundDetailsDto?> GetByIdAsync(long id)
        {
            var round = await _repo.GetByIdAsync(id);
            if (round == null) return null;

            var examQuestions = await GetExamQuestionsForRoundAsync(id);

            return new CourseRoundDetailsDto
            {
                Id = round.Id,
                RoundNumber = round.RoundNumber,
                CourseId = round.CourseId,
                CourseName = round.Course.Title,
                StartDate = round.StartDate,
                EndDate = round.EndDate,
                Price = round.Price,
                CourseRoundGroupId = round.CourseRoundGroupId,
                MinStudents = round.MinStudents,
                MaxStudents = round.MaxStudents,
                Status = round.Status?.StatusName,
                StatusId = round.StatusId,
                AutomatedWorkFlowJump = round.AutomatedWorkFlowJump,

                Question1 = round.Question1,
                Question2 = round.Question2,
                Question3 = round.Question3,
                Question4 = round.Question4,
                Question5 = round.Question5,
                Question6 = round.Question6,
                Question7 = round.Question7,
                Question8 = round.Question8,
                Question9 = round.Question9,
                Question10 = round.Question10,

                ExamQuestions = examQuestions,

                InstructorId = round.CourseRoundInstructors
                    .OrderByDescending(x => x.AssignedDate)
                    .Select(x => (long?)x.InstructorId)
                    .FirstOrDefault(),

                InstructorName = round.CourseRoundInstructors
                    .OrderByDescending(x => x.AssignedDate)
                    .Select(x => x.Instructor.FullNameEn)
                    .FirstOrDefault(),

                WeekTitles = round.CourseMaterials
                    .Where(cm => cm.Week != null)
                    .Select(cm => cm.Week!.WeekTitle)
                    .Distinct()
                    .ToList(),

                Groups = round.GroupedRounds.Select(gr => new CourseRoundGroupItemDto
                {
                    Id = gr.Id,
                    RoundNumber = gr.RoundNumber,
                    StartDate = gr.StartDate,
                    EndDate = gr.EndDate,
                    Price = gr.Price
                }).ToList()
            };
        }

        public async Task<CourseRoundDetailsDto> CreateAsync(CreateCourseRoundDto dto)
        {
            ValidateRound(dto.StartDate, dto.EndDate, dto.Price, dto.MinStudents, dto.MaxStudents);
            await EnsureGroupExistsAsync(dto.CourseRoundGroupId, null);

            var round = new CourseRound
            {
                CourseId = dto.CourseId,
                RoundNumber = dto.RoundNumber,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MinStudents = dto.MinStudents,
                MaxStudents = dto.MaxStudents,
                Price = dto.Price,
                StatusId = 38,
                CreatedAt = DateTime.UtcNow,
                CourseRoundGroupId = dto.CourseRoundGroupId,

                Question1 = dto.Question1,
                Question2 = dto.Question2,
                Question3 = dto.Question3,
                Question4 = dto.Question4,
                Question5 = dto.Question5,
                Question6 = dto.Question6,
                Question7 = dto.Question7,
                Question8 = dto.Question8,
                Question9 = dto.Question9,
                Question10 = dto.Question10,

                AutomatedWorkFlowJump = dto.AutomatedWorkFlowJump,
            };

            await _repo.AddAsync(round);
            await _repo.SaveChangesAsync();

            if (dto.ExamQuestions?.Any() == true)
            {
                await CreateExamQuestionsForRoundAsync(round.Id, dto.ExamQuestions);
            }

            return (await GetByIdAsync(round.Id))!;
        }

        public async Task<bool> UpdateAsync(long id, UpdateCourseRoundDto dto)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return false;

            ValidateRound(dto.StartDate, dto.EndDate, dto.Price, dto.MinStudents, dto.MaxStudents);
            await EnsureGroupExistsAsync(dto.CourseRoundGroupId, id);

            r.CourseId = dto.CourseId;
            r.CourseRoundGroupId = dto.CourseRoundGroupId;
            if (dto.AutomatedWorkFlowJump.HasValue) r.AutomatedWorkFlowJump = dto.AutomatedWorkFlowJump;
            r.RoundNumber = dto.RoundNumber;
            r.StartDate = dto.StartDate;
            r.EndDate = dto.EndDate;
            r.MinStudents = dto.MinStudents;
            r.MaxStudents = dto.MaxStudents;
            r.Price = dto.Price;
            r.StatusId = dto.StatusId;

            r.Question1 = dto.Question1;
            r.Question2 = dto.Question2;
            r.Question3 = dto.Question3;
            r.Question4 = dto.Question4;
            r.Question5 = dto.Question5;
            r.Question6 = dto.Question6;
            r.Question7 = dto.Question7;
            r.Question8 = dto.Question8;
            r.Question9 = dto.Question9;
            r.Question10 = dto.Question10;

            _repo.Update(r);
            var saved = await _repo.SaveChangesAsync();

            if (dto.NewExamQuestions?.Any() == true)
            {
                await CreateExamQuestionsForRoundAsync(id, dto.NewExamQuestions);
            }

            if (dto.RemoveExamQuestionIds?.Any() == true)
            {
                await RemoveExamQuestionsAsync(id, dto.RemoveExamQuestionIds);
            }

            return saved;
        }

        public async Task<bool> PatchAsync(long id, PatchCourseRoundDto dto)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return false;

            ValidateRound(
                dto.StartDate ?? r.StartDate,
                dto.EndDate ?? r.EndDate,
                dto.Price ?? r.Price,
                dto.MinStudents ?? r.MinStudents,
                dto.MaxStudents ?? r.MaxStudents);
            await EnsureGroupExistsAsync(dto.CourseRoundGroupId, id);

            if (dto.CourseId.HasValue) r.CourseId = dto.CourseId.Value;
            if (dto.CourseRoundGroupId.HasValue) r.CourseRoundGroupId = dto.CourseRoundGroupId;
            if (dto.AutomatedWorkFlowJump.HasValue) r.AutomatedWorkFlowJump = dto.AutomatedWorkFlowJump;
            if (dto.RoundNumber.HasValue) r.RoundNumber = dto.RoundNumber.Value;
            if (dto.StartDate.HasValue) r.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) r.EndDate = dto.EndDate.Value;
            if (dto.MinStudents.HasValue) r.MinStudents = dto.MinStudents.Value;
            if (dto.MaxStudents.HasValue) r.MaxStudents = dto.MaxStudents.Value;
            if (dto.Price.HasValue) r.Price = dto.Price.Value;
            if (dto.StatusId.HasValue) r.StatusId = dto.StatusId.Value;

            if (dto.Question1 != null) r.Question1 = dto.Question1;
            if (dto.Question2 != null) r.Question2 = dto.Question2;
            if (dto.Question3 != null) r.Question3 = dto.Question3;
            if (dto.Question4 != null) r.Question4 = dto.Question4;
            if (dto.Question5 != null) r.Question5 = dto.Question5;
            if (dto.Question6 != null) r.Question6 = dto.Question6;
            if (dto.Question7 != null) r.Question7 = dto.Question7;
            if (dto.Question8 != null) r.Question8 = dto.Question8;
            if (dto.Question9 != null) r.Question9 = dto.Question9;
            if (dto.Question10 != null) r.Question10 = dto.Question10;

            _repo.Update(r);
            var saved = await _repo.SaveChangesAsync();

            if (dto.NewExamQuestions?.Any() == true)
            {
                await CreateExamQuestionsForRoundAsync(id, dto.NewExamQuestions);
            }

            return saved;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return false;

            _repo.Remove(r);
            return await _repo.SaveChangesAsync();
        }

        // ========== ADD EXAM QUESTIONS (separate endpoint) ==========
        public async Task<bool> AddExamQuestionsAsync(long courseRoundId, List<CreateExamQuestionDto> questions)
        {
            var round = await _repo.GetByIdAsync(courseRoundId);
            if (round == null) throw new KeyNotFoundException("Course round not found");

            await CreateExamQuestionsForRoundAsync(courseRoundId, questions);
            return true;
        }

        // ========== REMOVE EXAM QUESTIONS (separate endpoint) ==========
        public async Task<bool> RemoveExamQuestionsAsync(long courseRoundId, List<long> questionIds)
        {
            var bankEntries = await _examBankRepo.GetByCourseRoundIdAsync(courseRoundId);

            var bankIds = bankEntries
                .Where(b => b.QuestionId.HasValue && questionIds.Contains(b.QuestionId.Value))
                .Select(b => b.Id)
                .ToList();

            if (bankIds.Count == 0) return true;

            // Answers already given keep their link to the exam, so those questions can't be removed.
            var bankIdsAsLong = bankIds.Select(id => (long)id).ToList();
            if (await _db.StudentExamAnswers.AnyAsync(sa => sa.QuestionbankId.HasValue && bankIdsAsLong.Contains(sa.QuestionbankId.Value)))
                throw new InvalidOperationException("Some of these questions were already answered by applicants and can't be removed.");

            // Only the round's link to the question is removed; the question itself stays in the question pool.
            var entries = await _db.ExamQuestionBanks.Where(b => bankIds.Contains(b.Id)).ToListAsync();
            _db.ExamQuestionBanks.RemoveRange(entries);
            await _db.SaveChangesAsync();
            return true;
        }

        // ========== PRIVATE HELPERS ==========

        private static void ValidateRound(DateTime? start, DateTime? end, decimal? price, long? minStudents, long? maxStudents)
        {
            if (start.HasValue && end.HasValue && start > end)
                throw new ArgumentException("Start date must be before end date.");
            if (price < 0)
                throw new ArgumentException("Price can't be negative.");
            if (minStudents < 0 || maxStudents < 0)
                throw new ArgumentException("Student limits can't be negative.");
            if (minStudents.HasValue && maxStudents.HasValue && minStudents > maxStudents)
                throw new ArgumentException("Minimum students can't be more than maximum students.");
        }

        private async Task EnsureGroupExistsAsync(long? groupId, long? roundId)
        {
            if (!groupId.HasValue) return;
            if (groupId == roundId)
                throw new ArgumentException("A round can't be grouped with itself.");
            if (await _repo.GetByIdAsync(groupId.Value) == null)
                throw new ArgumentException("Invalid CourseRoundGroupId");
        }

        private async Task<List<ExamQuestionDto>> GetExamQuestionsForRoundAsync(long courseRoundId)
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

        private async Task CreateExamQuestionsForRoundAsync(long courseRoundId, List<CreateExamQuestionDto> questions)
        {
            foreach (var qDto in questions)
            {
                // 1. Create the ExamQuestion
                var question = new ExamQuestion
                {
                    QuestionTitle = qDto.QuestionTitle,
                    Choice1 = qDto.Choice1,
                    Choice2 = qDto.Choice2,
                    Choice3 = qDto.Choice3,
                    Choice4 = qDto.Choice4,
                    CorrectAnswer = qDto.CorrectAnswer,
                    SectionId = qDto.SectionId
                };

                await _examQuestionRepo.AddAsync(question);
                await _examQuestionRepo.SaveChangesAsync();

                // 2. Create bank entry — ExamId can be null or CourseRoundId
                var bankEntry = new ExamQuestionBank
                {
                    ExamId = courseRoundId,  // <-- Use CourseRoundId as ExamId (optional)
                    QuestionId = question.Id,
                    CourseRoundId = courseRoundId
                };

                await _examBankRepo.AddAsync(bankEntry);
            }

            await _examBankRepo.SaveChangesAsync();
        }
    }
}