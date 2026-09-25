using GenZCoders.Extensions;
using GenZCoders.DTOs.CourseRoundDto;
using GenZCoders.DTOs.ExamsDto;
using GenZCoders.Services.CourseRoundService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseRoundController : ControllerBase
    {
        private readonly ICourseRoundService _service;

        public CourseRoundController(ICourseRoundService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<CourseRoundDto>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [AllowAnonymous]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<CourseRoundDetailsDto>> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost]
        public async Task<ActionResult<CourseRoundDetailsDto>> Create([FromBody] CreateCourseRoundDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult> Update(long id, [FromBody] UpdateCourseRoundDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(id, dto);
                return success ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPatch("{id:long}")]
        public async Task<ActionResult> Patch(long id, [FromBody] PatchCourseRoundDto dto)
        {
            try
            {
                var success = await _service.PatchAsync(id, dto);
                return success ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        // ========== NEW: Exam Question Management ==========

        // POST: api/course-round/{id}/exam-questions
        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost("{id:long}/exam-questions")]
        public async Task<ActionResult> AddExamQuestions(long id, [FromBody] List<CreateExamQuestionDto> questions)
        {
            try
            {
                await _service.AddExamQuestionsAsync(id, questions);
                return Ok(new { message = "Exam questions added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/course-round/{id}/exam-questions
        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpDelete("{id:long}/exam-questions")]
        public async Task<ActionResult> RemoveExamQuestions(long id, [FromBody] List<long> questionIds)
        {
            try
            {
                await _service.RemoveExamQuestionsAsync(id, questionIds);
                return Ok(new { message = "Exam questions removed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}