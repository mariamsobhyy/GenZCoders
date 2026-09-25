using GenZCoders.Extensions;
using GenZCoders.DTOs.ApplicationDto;
using GenZCoders.DTOs.ExamsDto;
using GenZCoders.Services.ApplicationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _service;

        public ApplicationController(IApplicationService service)
        {
            _service = service;
        }

        [HttpGet("exam-questions/{courseRoundId:long}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ExamQuestionDto>>> GetExamQuestions(long courseRoundId)
        {
            var questions = await _service.GetExamQuestionsAsync(courseRoundId);

            if (!questions.Any())
                return NotFound(new { message = "No questions found for this course round" });

            return Ok(questions);
        }
        [HttpPost]
        public async Task<ActionResult<ApplicationDto>> Create([FromBody] CreateApplicationDto dto)
        {
            // The applicant is always the logged-in user, never an id sent by the client.
            dto.AccountId = User.GetAccountId();

            var result = await _service.CreateAsync(dto);
            return Ok(HideExamResults(result));
        }

        [HttpPost("{applicationId:long}/exam-answers")]
        public async Task<ActionResult> SubmitExamAnswers(long applicationId, [FromBody] List<ExamAnswerItemDto> answers)
        {
            var accountId = User.GetAccountId();
            var app = await _service.GetByIdAsync(applicationId);

            if (app == null) return NotFound(new { message = "Application not found" });
            if (app.AccountId != accountId) return Forbid();

            await _service.SubmitExamAnswersAsync(accountId, app.CourseRoundId, answers);
            return Ok(new { message = "Answers submitted successfully" });
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApplicationDto>> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            if (!User.IsSelfOrStaff(result.AccountId)) return Forbid();
            return Ok(HideExamResults(result));
        }

        [HttpGet]
        public async Task<ActionResult<List<ApplicationDto>>> GetAll()
        {
            // Staff see every application; students only see their own.
            var results = await _service.GetAllAsync(User.IsStaff() ? null : User.GetAccountId());
            return Ok(results.Select(HideExamResults).ToList());
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        // Accepting, rejecting and confirming payment (status 42) are manager decisions.
        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpPatch("{id:long}/status")]
        public async Task<ActionResult> PatchStatus(long id, [FromBody] PatchApplicationStatusDto dto)
        {
            var success = await _service.PatchStatusAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpPatch("{id:long}/course-round")]
        public async Task<ActionResult> PatchCourseRound(long id, [FromBody] PatchApplicationCourseRoundDto dto)
        {
            var success = await _service.PatchCourseRoundAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        // Applicants must not learn which exam answers were correct (they could resubmit elsewhere or share them).
        private ApplicationDto HideExamResults(ApplicationDto dto)
        {
            if (!User.IsStaff() && dto.ExamAnswers != null)
            {
                foreach (var answer in dto.ExamAnswers)
                    answer.IsCorrect = null;
            }

            return dto;
        }
    }
}