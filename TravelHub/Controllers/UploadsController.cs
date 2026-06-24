using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UploadsController(IConfiguration configuration)
        {
            _configuration = configuration;
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

            // 3. Configure Cloudinary
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret) || apiKey == "YOUR_API_KEY_HERE")
            {
                return StatusCode(500, new { message = "Cloudinary configuration is missing or invalid. Please check appsettings.json" });
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new Cloudinary(account);
            
            // 4. Upload to Cloudinary
            using var stream = file.OpenReadStream();
            
            if (extension == ".pdf")
            {
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "travelhub_uploads"
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);
                
                if (uploadResult.Error != null)
                {
                    return StatusCode(500, new { message = "Upload failed: " + uploadResult.Error.Message });
                }

                return Ok(new { url = uploadResult.SecureUrl.ToString() });
            }
            else
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "travelhub_uploads",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return StatusCode(500, new { message = "Upload failed: " + uploadResult.Error.Message });
                }

                return Ok(new { url = uploadResult.SecureUrl.ToString() });
            }
        }
    }
}
