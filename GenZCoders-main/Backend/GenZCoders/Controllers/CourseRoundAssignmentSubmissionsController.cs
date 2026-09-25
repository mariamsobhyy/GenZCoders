using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;
using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.DTOs.CourseRoundAssignmentSubmissionDto;
using GenZCoders.Services.CourseRoundAssignmentSubmissionService;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CourseRoundAssignmentSubmissionsController : ControllerBase
{
    private readonly ICourseRoundAssignmentSubmissionService _service;
    private readonly SchoolDbContext _context;

    public CourseRoundAssignmentSubmissionsController(ICourseRoundAssignmentSubmissionService service, SchoolDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRoundAssignmentSubmissionRequestDto dto, CancellationToken cancellationToken)
    {
        if (!User.IsSelfOrStaff(dto.StudentId)) return Forbid();

        if (!SafeUrl.IsSafe(dto.SubmissionLink))
            return BadRequest(new { message = "SubmissionLink must be an http(s) URL." });

        // Only students enrolled in the assignment's round can submit to it.
        var roundId = await _context.CourseRoundAssignments
            .Where(a => a.Id == dto.AssignmentId)
            .Select(a => (long?)a.CourseRoundId)
            .FirstOrDefaultAsync(cancellationToken);

        if (roundId == null) return NotFound(new { message = "Assignment not found." });
        if (!await _context.IsEnrolledAsync(dto.StudentId, roundId.Value)) return Forbid();

        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        if (!User.IsSelfOrStaff(item.StudentId)) return Forbid();
        return Ok(item);
    }

    [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
    [HttpGet("by-assignment/{assignmentId:long}")]
    public async Task<IActionResult> GetByAssignment(long assignmentId, CancellationToken cancellationToken)
    {
        var items = await _service.GetByAssignmentIdAsync(assignmentId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("by-student/{studentId:long}")]
    public async Task<IActionResult> GetByStudent(long studentId, CancellationToken cancellationToken)
    {
        if (!User.IsSelfOrStaff(studentId)) return Forbid();

        var items = await _service.GetByStudentIdAsync(studentId, cancellationToken);
        return Ok(items);
    }

    [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Patch(long id, [FromBody] PatchCourseRoundAssignmentSubmissionRequestDto dto, CancellationToken cancellationToken)
    {
        await _service.PatchAsync(id, dto, cancellationToken);
        return NoContent();
    }
}
