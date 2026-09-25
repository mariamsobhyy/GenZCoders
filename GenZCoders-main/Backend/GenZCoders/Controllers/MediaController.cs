using GenZCoders.DTOs.MediaDto;
using GenZCoders.Extensions;
using GenZCoders.Models;
using GenZCoders.Services.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers
{
    [ApiController]
    [Route("api/medias")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly SchoolDbContext _context;
        private const string UploadFolder = "uploads";
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedFolders = { "payment_proofs" };
        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public MediaController(IMediaService mediaService, IWebHostEnvironment env, IConfiguration config, SchoolDbContext context)
        {
            _context = context;
            _mediaService = mediaService;
            _env = env;
            _config = config;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSizeBytes + 1024 * 1024)] // headroom for multipart overhead
        public async Task<IActionResult> Upload(IFormFile? file, [FromQuery] string folder = "payment_proofs")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            // Only known folder names are accepted, so "folder" can't point outside wwwroot/uploads (e.g. "../..").
            if (!AllowedFolders.Contains(folder))
                return BadRequest(new { message = "Invalid upload folder." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { message = "File too large. Max 10 MB." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, UploadFolder, folder);
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/')
                ?? $"{Request.Scheme}://{Request.Host}";
            var relativePath = $"/{UploadFolder}/{folder}/{fileName}";
            var fullUrl = $"{baseUrl}{relativePath}";

            return Ok(new { url = fullUrl, filePath = relativePath });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MediaCreateDto dto)
        {
            if (dto is null)
                return BadRequest(new { message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(dto.TableName))
                return BadRequest(new { message = "TableName is required." });

            if (dto.TableId <= 0)
                return BadRequest(new { message = "TableId must be greater than 0." });

            if (string.IsNullOrWhiteSpace(dto.FilePath))
                return BadRequest(new { message = "FilePath is required." });

            if (!SafeUrl.IsSafe(dto.FilePath))
                return BadRequest(new { message = "FilePath must be an http(s) URL." });

            if (!await CanManageAsync(dto.TableName, dto.TableId, forWrite: true))
                return Forbid();

            try
            {
                await _mediaService.AddMediaAsync(dto);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string tableName,
            [FromQuery] long tableId)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return BadRequest(new { message = "tableName is required." });

            if (tableId <= 0)
                return BadRequest(new { message = "tableId must be greater than 0." });

            // Course media is public to any logged-in user; application media (payment proofs) is private.
            var isCourseMedia = tableName.Trim().Equals("Course", StringComparison.OrdinalIgnoreCase);
            if (!isCourseMedia && !await CanManageAsync(tableName, tableId))
                return Forbid();

            try
            {
                var result = await _mediaService.GetMediasAsync(tableName, tableId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "id must be greater than 0." });

            var media = await _context.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (media == null)
                return NotFound(new { message = "Media not found" });

            if (!await CanManageAsync(media.TableName, media.TableId, forWrite: true))
                return Forbid();

            try
            {
                await _mediaService.DeleteMediaAsync(id);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Staff can manage any media. A student can only see the receipt on their own application, and can
        // only add/replace it while the application is accepted and waiting for payment confirmation.
        private async Task<bool> CanManageAsync(string tableName, long tableId, bool forWrite = false)
        {
            if (User.IsStaff()) return true;
            if (!tableName.Trim().Equals("Application", StringComparison.OrdinalIgnoreCase)) return false;

            var accountId = User.GetAccountId();
            return await _context.Applications.AnyAsync(a =>
                a.Id == tableId
                && a.AccountId == accountId
                && (!forWrite || a.StatusId == EnrollmentExtensions.AcceptedStatusId));
        }
    }

}
