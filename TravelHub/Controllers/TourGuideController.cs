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
        private readonly IWebHostEnvironment _env;

        public TourGuideController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                user.TourGuideProfile.IdFrontUrl = MoveTempFile(request.IdFrontUrl);
                user.TourGuideProfile.IdBackUrl = MoveTempFile(request.IdBackUrl);
                user.TourGuideProfile.CertUrl = MoveTempFile(request.CertUrl);
                user.TourGuideProfile.GuideAvatarUrl = MoveTempFile(request.GuideAvatarUrl);
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
                    IdFrontUrl = MoveTempFile(request.IdFrontUrl),
                    IdBackUrl = MoveTempFile(request.IdBackUrl),
                    CertUrl = MoveTempFile(request.CertUrl),
                    GuideAvatarUrl = MoveTempFile(request.GuideAvatarUrl),
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
                AdminNote = profile.AdminNote,
                CreatedAt = profile.CreatedAt
            };

            return Ok(dto);
        }

        [HttpGet("guide-requests/available")]
        public async Task<IActionResult> GetAvailableGuideRequests()
        {
            // For guides to see all available requests on the feed/portal
            var query = _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsHidden && p.PostType == "GuideRequest")
                .OrderByDescending(p => p.CreationDate);
            
            var posts = await query.Select(p => new PostDto
            {
                PostID = p.PostID,
                UserID = p.UserID,
                Username = p.User.Username,
                AvatarURL = p.User.AvatarURL,
                PostType = p.PostType,
                Title = p.Title,
                Content = p.Content,
                CreationDate = p.CreationDate
            }).ToListAsync();

            return Ok(posts);
        }

        [HttpPost("guide-requests/apply")]
        public async Task<IActionResult> ApplyForGuideRequest([FromBody] ApplyGuideRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int guideId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var post = await _context.Posts.FindAsync(request.PostID);
            if (post == null || post.PostType != "GuideRequest")
            {
                return NotFound(new { message = "Guide request post not found." });
            }

            if (post.UserID == guideId)
            {
                return BadRequest(new { message = "You cannot apply for your own request." });
            }

            var existing = await _context.GuideApplications
                .FirstOrDefaultAsync(a => a.PostID == request.PostID && a.GuideID == guideId);
            
            if (existing != null)
            {
                return BadRequest(new { message = "You have already applied for this request." });
            }

            var application = new GuideApplication
            {
                PostID = request.PostID,
                GuideID = guideId,
                Message = request.Message,
                ProposedPriceVND = request.ProposedPriceVND,
                Status = "Pending",
                AppliedDate = DateTime.UtcNow
            };

            _context.GuideApplications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Application submitted successfully." });
        }

        [HttpGet("guide-requests/my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int guideId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var applications = await _context.GuideApplications
                .Include(a => a.Post)
                .ThenInclude(p => p.User)
                .Where(a => a.GuideID == guideId)
                .OrderByDescending(a => a.AppliedDate)
                .Select(a => new 
                {
                    Application = new GuideApplicationDto
                    {
                        ApplicationID = a.ApplicationID,
                        GuideID = a.GuideID,
                        PostID = a.PostID,
                        Status = a.Status,
                        Message = a.Message,
                        ProposedPriceVND = a.ProposedPriceVND,
                        AppliedDate = a.AppliedDate
                    },
                    Post = new PostDto
                    {
                        PostID = a.Post.PostID,
                        UserID = a.Post.UserID,
                        Username = a.Post.User.Username,
                        AvatarURL = a.Post.User.AvatarURL,
                        PostType = a.Post.PostType,
                        Title = a.Post.Title,
                        Content = a.Post.Content,
                        CreationDate = a.Post.CreationDate
                    }
                })
                .ToListAsync();

            return Ok(applications);
        }

        [HttpGet("guide-requests/my-posts")]
        public async Task<IActionResult> GetMyGuideRequests()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var posts = await _context.Posts
                .Include(p => p.GuideApplications)
                .ThenInclude(ga => ga.Guide)
                .Where(p => p.UserID == userId && p.PostType == "GuideRequest" && !p.IsHidden)
                .OrderByDescending(p => p.CreationDate)
                .Select(p => new
                {
                    Post = new PostDto
                    {
                        PostID = p.PostID,
                        Title = p.Title,
                        Content = p.Content,
                        CreationDate = p.CreationDate
                    },
                    Applications = p.GuideApplications.Select(ga => new GuideApplicationDto
                    {
                        ApplicationID = ga.ApplicationID,
                        GuideID = ga.GuideID,
                        GuideUsername = ga.Guide.Username,
                        GuideAvatarURL = ga.Guide.AvatarURL,
                        PostID = ga.PostID,
                        Status = ga.Status,
                        Message = ga.Message,
                        ProposedPriceVND = ga.ProposedPriceVND,
                        AppliedDate = ga.AppliedDate
                    }).ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpPost("guide-requests/accept-application/{applicationId}")]
        public async Task<IActionResult> AcceptGuideApplication(int applicationId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var application = await _context.GuideApplications
                .Include(a => a.Post)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);
            
            if (application == null)
            {
                return NotFound(new { message = "Application not found." });
            }

            if (application.Post.UserID != userId)
            {
                return Forbid();
            }

            // Accept this one, decline others
            var allApplications = await _context.GuideApplications
                .Where(a => a.PostID == application.PostID)
                .ToListAsync();

            foreach(var app in allApplications)
            {
                if (app.ApplicationID == applicationId)
                {
                    app.Status = "Accepted";
                }
                else
                {
                    app.Status = "Declined";
                }
            }

            // Create a chat group with the guide
            string groupName = $"Chuyến đi: {application.Post.Title}";
            var existingGroup = await _context.Chats
                .Include(c => c.ChatParticipants)
                .FirstOrDefaultAsync(c => c.IsGroupChat && c.ChatName == groupName && c.ChatParticipants.Any(cp => cp.UserID == userId));

            if (existingGroup == null)
            {
                existingGroup = new Chat
                {
                    ChatName = groupName,
                    IsGroupChat = true
                };
                _context.Chats.Add(existingGroup);
                await _context.SaveChangesAsync();

                _context.ChatParticipants.Add(new ChatParticipant { ChatID = existingGroup.ChatID, UserID = userId });
                _context.ChatParticipants.Add(new ChatParticipant { ChatID = existingGroup.ChatID, UserID = application.GuideID });
            }
            else
            {
                if (!existingGroup.ChatParticipants.Any(cp => cp.UserID == application.GuideID))
                {
                    _context.ChatParticipants.Add(new ChatParticipant { ChatID = existingGroup.ChatID, UserID = application.GuideID });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Guide accepted and chat room created." });
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

        private string? MoveTempFile(string? tempUrl)
        {
            if (string.IsNullOrWhiteSpace(tempUrl) || !tempUrl.StartsWith("/uploads/temp/"))
            {
                return tempUrl;
            }

            var fileName = Path.GetFileName(tempUrl);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var tempPath = Path.Combine(webRootPath, "uploads", "temp", fileName);
            var guidesFolder = Path.Combine(webRootPath, "uploads", "guides");

            if (!Directory.Exists(guidesFolder))
            {
                Directory.CreateDirectory(guidesFolder);
            }

            var newPath = Path.Combine(guidesFolder, fileName);

            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Move(tempPath, newPath);
                return $"/uploads/guides/{fileName}";
            }

            return tempUrl;
        }
    }
}
