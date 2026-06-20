using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Optionally: [Authorize(Roles = "Admin")]
    // Since we assigned role in AuthController, you might want to uncomment it later.
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? offlineFilter = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;

            var query = _context.Users.AsQueryable();

            // Apply filter based on LastOnline
            var now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(offlineFilter))
            {
                if (offlineFilter == "1_24_hours")
                {
                    var maxDate = now.AddHours(-1);
                    var minDate = now.AddHours(-24);
                    query = query.Where(u => u.LastOnline >= minDate && u.LastOnline <= maxDate);
                }
                else if (offlineFilter == "1_30_days")
                {
                    var maxDate = now.AddDays(-1);
                    var minDate = now.AddDays(-30);
                    query = query.Where(u => u.LastOnline >= minDate && u.LastOnline <= maxDate);
                }
            }

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    AvatarURL = u.AvatarURL,
                    RegistrationDate = u.RegistrationDate,
                    LastOnline = u.LastOnline
                })
                .ToListAsync();

            // Calculate formatted offline duration text
            foreach (var user in users)
            {
                user.OfflineDurationText = GetOfflineDurationText(user.LastOnline, now);
            }

            var response = new AdminUserResponse
            {
                TotalUsers = totalUsers,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Users = users
            };

            return Ok(response);
        }

        private string GetOfflineDurationText(DateTime? lastOnline, DateTime now)
        {
            if (!lastOnline.HasValue)
                return "Chưa từng online";

            var timeSpan = now - lastOnline.Value;

            if (timeSpan.TotalMinutes < 5)
                return "Vừa mới online";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            
            int months = (int)(timeSpan.TotalDays / 30);
            return $"{months} tháng trước";
        }

        [HttpGet("guides/pending")]
        public async Task<IActionResult> GetPendingGuides()
        {
            var guides = await _context.TourGuideProfiles
                                       .Include(p => p.User)
                                       .Where(p => p.IsVerified == "Pending")
                                       .Select(p => new TourGuideProfileDto
                                       {
                                           ProfileID = p.ProfileID,
                                           UserID = p.UserID,
                                           Username = p.User.Username,
                                           Email = p.User.Email,
                                           FullName = p.User.FullName,
                                           Experience = p.Experience,
                                           Languages = p.Languages,
                                           Locations = p.Locations,
                                           Bio = p.Bio,
                                           TourCategories = p.TourCategories,
                                           IdFrontUrl = p.IdFrontUrl,
                                           IdBackUrl = p.IdBackUrl,
                                           CertUrl = p.CertUrl,
                                           GuideAvatarUrl = p.GuideAvatarUrl,
                                           IsVerified = p.IsVerified,
                                           CreatedAt = p.CreatedAt
                                       })
                                       .ToListAsync();
            return Ok(guides);
        }

        [HttpPost("guides/approve")]
        public async Task<IActionResult> ApproveGuide([FromBody] AdminApproveGuideRequest request)
        {
            var profile = await _context.TourGuideProfiles
                                        .Include(p => p.User)
                                        .FirstOrDefaultAsync(p => p.ProfileID == request.ProfileID);
            if (profile == null)
            {
                return NotFound(new { message = "Tour guide profile not found." });
            }

            if (request.Approve)
            {
                profile.IsVerified = "Approved";
                profile.User.Role = "TourGuide"; // Update Role
            }
            else
            {
                profile.IsVerified = "Rejected";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = request.Approve ? "Guide approved successfully." : "Guide application rejected." });
        }
    }
}
