using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.DTOs.CourseMaterialDto;
using GenZCoders.Models;
using GenZCoders.Services.CourseMaterialService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseMaterialController : ControllerBase
    {
        private const long PdfMaterialTypeId = 32;
        private const int MaxPdfSizeBytes = 25 * 1024 * 1024; // 25 MB

        private readonly ICourseMaterialService _service;
        private readonly SchoolDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public CourseMaterialController(
            ICourseMaterialService service,
            SchoolDbContext context,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _service = service;
            _context = context;
            _env = env;
            _config = config;
        }

        [HttpGet]
        public async Task<ActionResult<List<ReadCourseMaterialDto>>> GetAll([FromQuery] long? courseRoundId)
        {
            var materials = await _service.GetAllAsync();

            if (courseRoundId != null)
                materials = materials.Where(m => m.CourseRoundId == courseRoundId).ToList();

            await HideLinksFromNonEnrolledAsync(materials);
            return Ok(materials);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadCourseMaterialDto>> GetById(long id)
        {
            var material = await _service.GetByIdAsync(id);
            if (material == null) return NotFound();

            await HideLinksFromNonEnrolledAsync(new[] { material });
            return Ok(material);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost]
        public async Task<ActionResult<ReadCourseMaterialDto>> Create(CreateCourseMaterialDto dto)
        {
            if (!SafeUrl.IsSafe(dto.Link))
                return BadRequest(new { message = "Link must be an http(s) URL." });

            dto.CreatedByAccountId = User.GetAccountId();
            var material = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost("zoom")]
        public async Task<ActionResult<ReadCourseMaterialDto>> CreateZoom(CreateZoomCourseMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Meeting topic (Title) is required" });
            if (dto.DurationMinutes < 15 || dto.DurationMinutes > 300)
                return BadRequest(new { message = "Duration must be between 15 and 300 minutes" });

            dto.CreatedByAccountId = User.GetAccountId();

            try
            {
                var material = await _service.CreateZoomMaterialAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
            }
            catch (InvalidOperationException ex)
            {
                // Zoom configuration / Zoom API errors carry a message meant for the instructor.
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPost("pdf")]
        [RequestSizeLimit(MaxPdfSizeBytes + 1024 * 1024)] // headroom for multipart overhead
        public async Task<ActionResult<ReadCourseMaterialDto>> CreatePdf([FromForm] CreatePdfCourseMaterialDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "A PDF file is required." });
            if (!Path.GetExtension(dto.File.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only PDF files are allowed." });
            if (dto.File.Length > MaxPdfSizeBytes)
                return BadRequest(new { message = "File too large. Max 25 MB." });
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Title is required." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "materials");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}.pdf";
            await using (var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";

            var material = await _service.CreateAsync(new CreateCourseMaterialDto
            {
                CourseRoundId = dto.CourseRoundId,
                CreatedByAccountId = User.GetAccountId(),
                WeekId = dto.WeekId,
                ParentMaterialId = dto.ParentMaterialId,
                MaterialTypeStatusId = dto.MaterialTypeStatusId ?? PdfMaterialTypeId,
                Title = dto.Title.Trim(),
                Description = dto.Description,
                Link = $"{baseUrl}/uploads/materials/{fileName}"
            });

            return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, UpdateCourseMaterialDto dto)
        {
            if (!SafeUrl.IsSafe(dto.Link))
                return BadRequest(new { message = "Link must be an http(s) URL." });

            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(long id, PatchCourseMaterialDto dto)
        {
            if (!SafeUrl.IsSafe(dto.Link))
                return BadRequest(new { message = "Link must be an http(s) URL." });

            var updated = await _service.PatchAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }

        [Authorize(Policy = ClaimsPrincipalExtensions.StaffPolicy)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // Students can browse material titles of any round (course preview), but links and Zoom
        // credentials are only visible for rounds they are enrolled (paid) in.
        private async Task HideLinksFromNonEnrolledAsync(IEnumerable<ReadCourseMaterialDto> materials)
        {
            if (User.IsStaff()) return;

            var enrolledRoundIds = await _context.GetEnrolledRoundIdsAsync(User.GetAccountId());

            foreach (var material in materials)
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
