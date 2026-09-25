using GenZCoders.Models;
using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.DTOs.WeekDto;
using GenZCoders.Services.WeekService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeekController : ControllerBase
    {
        private readonly IWeekService _service;
        private readonly SchoolDbContext _context;

        public WeekController(IWeekService service, SchoolDbContext context)
        {
            _service = service;
            _context = context;
        }

        // GET: api/Week?courseRoundId=5
        [HttpGet]
        public async Task<ActionResult<List<WeekDto>>> GetAll([FromQuery] long? courseRoundId)
        {
            var weeks = await _service.GetAllAsync();

            if (courseRoundId != null)
                weeks = weeks.Where(w => w.CourseRoundId == courseRoundId).ToList();

            await HideLinksFromNonEnrolledAsync(weeks);
            return Ok(weeks);
        }

        // GET: api/Week/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WeekDto>> GetById(int id)
        {
            var week = await _service.GetByIdAsync(id);
            if (week == null) return NotFound();

            await HideLinksFromNonEnrolledAsync(new[] { week });
            return Ok(week);
        }

        // POST: api/Week
        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost]
        public async Task<ActionResult<WeekDto>> Create([FromBody] CreateWeekDto dto)
        {
            if (dto == null) return BadRequest();

            dto.AccountId = User.GetAccountId();
            var week = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = week.Id }, week);
        }

        // PUT: api/Week/5
        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWeekDto dto)
        {
            if (dto == null) return BadRequest();

            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();

            return NoContent();
        }

        // DELETE: api/Week/5
        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }

        // Same rule as CourseMaterialController: titles are public to students, links only when enrolled.
        private async Task HideLinksFromNonEnrolledAsync(IEnumerable<WeekDto> weeks)
        {
            if (User.IsStaff()) return;

            var enrolledRoundIds = await _context.GetEnrolledRoundIdsAsync(User.GetAccountId());

            foreach (var material in weeks.SelectMany(w => w.CourseMaterials))
            {
                if (material.CourseRoundId is long roundId && enrolledRoundIds.Contains(roundId))
                    continue;

                material.Link = null;
                material.MeetingId = null;
                material.MeetingPassword = null;
            }
        }
    }
}
