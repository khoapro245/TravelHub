using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            // 1. Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Only JPG, PNG, and PDF are allowed." });
            }

            // 2. Validate file content type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "application/pdf" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                 return BadRequest(new { message = "Invalid file content type." });
            }

            // 3. Generate secure random file name
            var fileName = Guid.NewGuid().ToString() + extension;

            // 4. Check if WebRootPath is null (can happen if wwwroot doesn't exist yet)
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            // 5. Save to wwwroot/uploads/temp
            var tempFolder = Path.Combine(webRootPath, "uploads", "temp");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            var filePath = Path.Combine(tempFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL
            var fileUrl = $"/uploads/temp/{fileName}";
            return Ok(new { url = fileUrl });
        }
    }
}
