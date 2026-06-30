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
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var now = DateTime.UtcNow;

            // 1. Calculate Stats
            var totalUsers = await _context.Users.CountAsync();
            var activeDestinations = await _context.Destinations.CountAsync();
            var totalPosts = await _context.Posts.CountAsync();
            
            // Assuming we only sum up Confirmed bookings for revenue
            var totalRevenue = await _context.TourBookings
                .Where(b => b.Status == "Confirmed")
                .SumAsync(b => b.TotalPriceVND);

            var stats = new AdminStats
            {
                TotalUsers = totalUsers,
                ActiveDestinations = activeDestinations,
                TotalPosts = totalPosts,
                TotalRevenue = totalRevenue
            };

            // 2. Generate User Growth Data (Last 6 months)
            // Fetch only the two date columns ONCE and compute every month in memory,
            // instead of running 3 DB round-trips per month (18 queries -> 1 query).
            var userDates = await _context.Users
                .Select(u => new { u.RegistrationDate, u.LastOnline })
                .ToListAsync();

            var userGrowth = new List<UserGrowthData>();
            var sixMonthsAgo = now.AddMonths(-5);
            var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);
            
            // Lấy toàn bộ user từ 6 tháng trước vào RAM (vì số lượng user mới trong 6 tháng thường không quá lớn để gây tràn RAM)
            // Nếu data lớn, ta nên dùng GroupBy SQL, nhưng SQLite/SQLServer có syntax khác nhau nên lấy về xử lý RAM là an toàn nhất.
            var recentUsersData = await _context.Users
                .Where(u => u.RegistrationDate >= startDate)
                .Select(u => new { u.RegistrationDate, u.LastOnline })
                .ToListAsync();

            var oldUsersCount = await _context.Users.CountAsync(u => u.RegistrationDate < startDate);
            int runningTotal = oldUsersCount;

            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var newUsersInMonth = recentUsersData.Count(u => u.RegistrationDate >= monthStart && u.RegistrationDate < monthEnd);
                runningTotal += newUsersInMonth;
                
                if (i == 5) activeUsers += (int)(oldUsersCount * 0.8); // Estimate active from old users

                userGrowth.Add(new UserGrowthData
                {
                    Month = "Tháng " + monthStart.Month,
                    Users = runningTotal,
                    Active = activeUsers > 0 ? activeUsers : (int)(runningTotal * 0.8)
                });
            }

            // 3. Generate Destination Distribution
            var colors = new[] { "#3B82F6", "#06B6D4", "#FB923C", "#8B5CF6", "#EC4899" };
            
            var destinationDistributionQuery = await _context.Destinations
                .GroupBy(d => d.CityProvince)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            var destinationDistribution = destinationDistributionQuery.Select((g, index) => new DestinationData
            {
                Name = string.IsNullOrEmpty(g.Name) ? "Khác" : g.Name,
                Value = g.Count,
                Color = colors[index % colors.Length]
            }).ToList();

            // 4. Generate Recent Users
            var recentUsersList = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(4)
                .ToListAsync();

            var recentUsers = recentUsersList.Select(u => new RecentUserDto
            {
                Id = u.UserID,
                Name = u.FullName ?? u.Username,
                Email = u.Email,
                University = u.StudentCode ?? "N/A", // Using StudentCode as a placeholder for university/info
                Joined = GetOfflineDurationText(u.RegistrationDate, now), // Reusing this for "Joined X time ago"
                Status = (!u.LastOnline.HasValue || (now - u.LastOnline.Value).TotalDays > 7) ? "không hoạt động" : "đang hoạt động",
                Avatar = string.IsNullOrEmpty(u.AvatarURL) ? "https://ui-avatars.com/api/?name=" + (u.FullName ?? u.Username) : u.AvatarURL
            }).ToList();

            var response = new AdminOverviewResponse
            {
                Stats = stats,
                UserGrowth = userGrowth,
                DestinationDistribution = destinationDistribution,
                RecentUsers = recentUsers
            };

            return Ok(response);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? offlineFilter = null, [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;

            var query = _context.Users.AsQueryable();

            // Apply search filter (Username, Email, FullName, UserCode)
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(u => 
                    u.Username.ToLower().Contains(lowerSearch) || 
                    (u.Email != null && u.Email.ToLower().Contains(lowerSearch)) || 
                    (u.FullName != null && u.FullName.ToLower().Contains(lowerSearch)) || 
                    (u.UserCode != null && u.UserCode.ToLower().Contains(lowerSearch))
                );
            }

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
                    LastOnline = u.LastOnline,
                    IsBlocked = u.IsBlocked,
                    Role = u.Role,
                    UserCode = u.UserCode,
                    TravelPoints = u.TravelPoints
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

        // Xem chi tiết một người dùng
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var dto = new AdminUserDetailDto
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                AvatarURL = user.AvatarURL,
                DateOfBirth = user.DateOfBirth,
                StudentCode = user.StudentCode,
                Gender = user.Gender,
                Role = user.Role,
                IsBlocked = user.IsBlocked,
                IsPremium = user.IsPremium,
                RegistrationDate = user.RegistrationDate,
                LastOnline = user.LastOnline
            };

            return Ok(dto);
        }

        // Chỉnh sửa thông tin người dùng
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (request.FullName != null) user.FullName = request.FullName;
            if (request.StudentCode != null) user.StudentCode = request.StudentCode;
            if (request.Gender != null) user.Gender = request.Gender;

            // Email phải là duy nhất
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserID != id);
                if (emailTaken)
                    return BadRequest(new { message = "Email already exists." });
                user.Email = request.Email;
            }

            // Chỉ chấp nhận các vai trò hợp lệ
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var validRoles = new[] { "Customer", "TourGuide", "Admin" };
                if (!validRoles.Contains(request.Role))
                    return BadRequest(new { message = "Invalid role." });
                user.Role = request.Role;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "User updated successfully." });
        }

        // Chặn / bỏ chặn người dùng. Khi bị chặn, xoá refresh token để buộc đăng xuất.
        [HttpPut("users/{id}/block")]
        public async Task<IActionResult> BlockUser(int id, [FromBody] AdminBlockUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Role == "Admin")
                return BadRequest(new { message = "Cannot block an admin account." });

            user.IsBlocked = request.IsBlocked;

            if (request.IsBlocked)
            {
                // Vô hiệu hoá phiên hiện tại để người dùng bị đẩy ra ngay khi token làm mới
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = request.IsBlocked ? "User blocked successfully." : "User unblocked successfully.",
                isBlocked = user.IsBlocked
            });
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
                                           DateOfBirth = p.DateOfBirth,
                                           Gender = p.Gender,
                                           Phone = p.Phone,
                                           Address = p.Address,
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

            // Lưu ghi chú của admin (null/rỗng nếu không nhập)
            profile.AdminNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = request.Approve ? "Guide approved successfully." : "Guide application rejected." });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.Reports
                .Include(r => r.Post)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.ReportDate)
                .Select(r => new ReportDto
                {
                    ReportID = r.ReportID,
                    PostID = r.PostID,
                    PostTitle = r.Post.Title,
                    PostContent = r.Post.Content ?? "",
                    ReporterName = r.Reporter.FullName ?? r.Reporter.Username,
                    Reason = r.Reason,
                    Status = r.Status,
                    ReportDate = r.ReportDate
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpPut("reports/{id}/status")]
        public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateReportStatusRequest request)
        {
            var report = await _context.Reports.Include(r => r.Post).FirstOrDefaultAsync(r => r.ReportID == id);
            if (report == null)
                return NotFound("Report not found.");

            if (request.Status == "Resolved")
            {
                report.Status = "Resolved";
                if (report.Post != null)
                {
                    report.Post.IsHidden = true;
                }
            }
            else if (request.Status == "Rejected")
            {
                report.Status = "Rejected";
            }
            else
            {
                return BadRequest("Invalid status. Must be 'Resolved' or 'Rejected'.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Report status updated successfully." });
        }

        [HttpPut("users/{userId}/points")]
        public async Task<IActionResult> UpdateUserPoints(int userId, [FromBody] UpdateUserPointsRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.TravelPoints = request.TravelPoints;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User points updated successfully.", TravelPoints = user.TravelPoints });
        }
    }

    public class UpdateUserPointsRequest
    {
        public int TravelPoints { get; set; }
    }
}
