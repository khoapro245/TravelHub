using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelHub.DTO;
using TravelHub.Model;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TourGuideController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourGuideController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsGuide([FromBody] TourGuideRegistrationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var user = await _context.Users.Include(u => u.TourGuideProfile)
                                           .FirstOrDefaultAsync(u => u.UserID == userId);
            
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.TourGuideProfile != null)
            {
                // Nếu đã bị reject thì cho phép đăng ký lại bằng cách update, nếu đang pending thì báo chờ
                if (user.TourGuideProfile.IsVerified == "Pending")
                {
                    return BadRequest(new { message = "Your application is already pending." });
                }
                if (user.TourGuideProfile.IsVerified == "Approved")
                {
                    return BadRequest(new { message = "You are already an approved Tour Guide." });
                }
                
                // Update if rejected
                user.TourGuideProfile.DateOfBirth = ParseDate(request.DateOfBirth);
                user.TourGuideProfile.Gender = request.Gender;
                user.TourGuideProfile.Phone = request.Phone;
                user.TourGuideProfile.Address = request.Address;
                user.TourGuideProfile.Experience = request.Experience;
                user.TourGuideProfile.Languages = request.Languages;
                user.TourGuideProfile.Locations = request.Locations;
                user.TourGuideProfile.Bio = request.Bio;
                user.TourGuideProfile.TourCategories = request.TourCategories;
                user.TourGuideProfile.IdFrontUrl = request.IdFrontUrl;
                user.TourGuideProfile.IdBackUrl = request.IdBackUrl;
                user.TourGuideProfile.CertUrl = request.CertUrl;
                user.TourGuideProfile.GuideAvatarUrl = request.GuideAvatarUrl;
                user.TourGuideProfile.IsVerified = "Pending";
                user.TourGuideProfile.CreatedAt = DateTime.UtcNow;

                _context.TourGuideProfiles.Update(user.TourGuideProfile);
            }
            else
            {
                // Create new
                var profile = new TourGuideProfile
                {
                    UserID = userId,
                    DateOfBirth = ParseDate(request.DateOfBirth),
                    Gender = request.Gender,
                    Phone = request.Phone,
                    Address = request.Address,
                    Experience = request.Experience,
                    Languages = request.Languages,
                    Locations = request.Locations,
                    Bio = request.Bio,
                    TourCategories = request.TourCategories,
                    IdFrontUrl = request.IdFrontUrl,
                    IdBackUrl = request.IdBackUrl,
                    CertUrl = request.CertUrl,
                    GuideAvatarUrl = request.GuideAvatarUrl,
                    IsVerified = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.TourGuideProfiles.Add(profile);
            }

            // Cập nhật họ tên tài khoản từ form đăng ký nếu có nhập
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration submitted successfully. Please wait for admin approval." });
        }

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var profile = await _context.TourGuideProfiles
                                        .Include(p => p.User)
                                        .FirstOrDefaultAsync(p => p.UserID == userId);

            if (profile == null)
            {
                return NotFound(new { message = "Tour guide profile not found." });
            }

            var dto = new TourGuideProfileDto
            {
                ProfileID = profile.ProfileID,
                UserID = profile.UserID,
                Username = profile.User.Username,
                Email = profile.User.Email,
                FullName = profile.User.FullName,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Phone = profile.Phone,
                Address = profile.Address,
                Experience = profile.Experience,
                Languages = profile.Languages,
                Locations = profile.Locations,
                Bio = profile.Bio,
                TourCategories = profile.TourCategories,
                IdFrontUrl = profile.IdFrontUrl,
                IdBackUrl = profile.IdBackUrl,
                CertUrl = profile.CertUrl,
                GuideAvatarUrl = profile.GuideAvatarUrl,
                IsVerified = profile.IsVerified,
                CreatedAt = profile.CreatedAt
            };

            return Ok(dto);
        }

        // Parse chuỗi ngày (yyyy-MM-dd) từ form thành DateTime?, trả null nếu rỗng/không hợp lệ
        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            return DateTime.TryParse(value, out var parsed) ? parsed : (DateTime?)null;
        }
    }
}
