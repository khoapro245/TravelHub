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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdOpt = GetCurrentUserId();
            if (userIdOpt == null) return Unauthorized("Invalid user token.");
            int userId = userIdOpt.Value;

            var user = await _context.Users
                .Include(u => u.UserPreference)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound("User not found.");

            // Update LastOnline
            user.LastOnline = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var profile = new UserProfileDto
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                AvatarURL = user.AvatarURL,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                StudentCode = user.StudentCode,
                Gender = user.Gender,
                RegistrationDate = user.RegistrationDate,
                PreferredBudgetVND = user.UserPreference?.PreferredBudgetVND,
                TravelStyle = user.UserPreference?.TravelStyle,
                FavoriteActivities = user.UserPreference?.FavoriteActivities,
                MaxDurationDays = user.UserPreference?.MaxDurationDays,
                PreferredDestinations = user.UserPreference?.PreferredDestinations
            };

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdOpt = GetCurrentUserId();
            if (userIdOpt == null) return Unauthorized("Invalid user token.");
            int userId = userIdOpt.Value;

            var user = await _context.Users
                .Include(u => u.UserPreference)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound("User not found.");

            // Chỉ cập nhật các trường được truyền lên (khác null)
            if (request.AvatarURL != null) user.AvatarURL = request.AvatarURL;
            if (request.FullName != null) user.FullName = request.FullName;
            if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
            if (request.Gender != null) user.Gender = request.Gender;

            // Khởi tạo Preferences nếu tài khoản mới chưa từng thiết lập cấu hình sở thích
            if (user.UserPreference == null)
            {
                user.UserPreference = new UserPreference { UserID = userId };
            }

            if (request.PreferredBudgetVND.HasValue) user.UserPreference.PreferredBudgetVND = request.PreferredBudgetVND;
            if (request.TravelStyle != null) user.UserPreference.TravelStyle = request.TravelStyle;
            if (request.FavoriteActivities != null) user.UserPreference.FavoriteActivities = request.FavoriteActivities;
            if (request.MaxDurationDays.HasValue) user.UserPreference.MaxDurationDays = request.MaxDurationDays;
            if (request.PreferredDestinations != null) user.UserPreference.PreferredDestinations = request.PreferredDestinations;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully." });
        }

        [AllowAnonymous] // Cho phép khách vãng lai xem Profile công khai của người khác mà không cần Token
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPublicProfile(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserPreference)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
                return NotFound("User not found.");

            var publicProfile = new PublicUserProfileDto
            {
                UserID = user.UserID,
                Username = user.Username,
                AvatarURL = user.AvatarURL,
                FullName = user.FullName,
                Gender = user.Gender,
                TravelStyle = user.UserPreference?.TravelStyle,
                FavoriteActivities = user.UserPreference?.FavoriteActivities,
                PreferredDestinations = user.UserPreference?.PreferredDestinations
            };

            return Ok(publicProfile);
        }

        [HttpGet("me/dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userIdOpt = GetCurrentUserId();
            if (userIdOpt == null) return Unauthorized("Invalid user token.");
            int userId = userIdOpt.Value;

            var upcomingTripsCount = await _context.Itineraries
                .CountAsync(i => i.UserID == userId && i.Status == "Planned");

            var pendingRequestsCount = await _context.TravelCompanions
                .CountAsync(tc => tc.ReceiverID == userId && tc.Status == "Pending");

            int savedDestinationsCount = 0;

            var dashboard = new DashboardDto
            {
                UpcomingTripsCount = upcomingTripsCount,
                PendingBuddyRequestsCount = pendingRequestsCount,
                SavedDestinationsCount = savedDestinationsCount
            };

            return Ok(dashboard);
        }
    }
}