using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.DTOs.CourseDto;
using GenZCoders.Services.CourseService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _service;

        public CoursesController(ICourseService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
       => Ok(await _service.GetAllAsync());

        [AllowAnonymous]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var course = await _service.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateCourseDto dto)
        {
            var course = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, UpdateCourseDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpPatch("{id:long}")]
        public async Task<IActionResult> Patch(long id, PatchCourseDto dto)
        {
            var updated = await _service.PatchAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        
  [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
  [HttpPut("{id:long}/accept")]
public async Task<IActionResult> Accept(long id)
{
    var accepted = await _service.AcceptAsync(id);

    if (!accepted)
        return NotFound(new { message = "Course not found" });

    return Ok(new { message = "Course accepted successfully" });
}
    }

}
