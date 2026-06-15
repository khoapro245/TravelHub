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
    public class BuddiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BuddiesController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user token.");
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations()
        {
            try
            {
                int userId = GetCurrentUserId();

                // Mock logic for AI Matchmaking:
                // We'll just return up to 5 random other users, scoring them arbitrarily.
                var candidates = await _context.Users
                    .Include(u => u.UserPreference)
                    .Where(u => u.UserID != userId)
                    .Take(5)
                    .ToListAsync();

                var recommendations = candidates.Select(c => new BuddyRecommendationDto
                {
                    UserID = c.UserID,
                    Username = c.Username,
                    AvatarURL = c.AvatarURL,
                    FullName = c.FullName,
                    MatchReason = "Similar travel style and preferred budget.",
                    MatchScore = new Random().Next(70, 99) // Mock score between 70% and 99%
                })
                .OrderByDescending(r => r.MatchScore)
                .ToList();

                return Ok(recommendations);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("requests")]
        public async Task<IActionResult> SendRequest([FromBody] CreateBuddyRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                if (userId == request.ReceiverID)
                    return BadRequest("You cannot send a request to yourself.");

                var exists = await _context.TravelCompanions
                    .AnyAsync(tc => tc.RequesterID == userId && tc.ReceiverID == request.ReceiverID && tc.Status == "Pending");

                if (exists)
                    return BadRequest("You already have a pending request with this user.");

                var companion = new TravelCompanion
                {
                    RequesterID = userId,
                    ReceiverID = request.ReceiverID,
                    PostID = request.PostID,
                    Status = "Pending"
                };

                _context.TravelCompanions.Add(companion);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Buddy request sent.", CompanionID = companion.CompanionID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPut("requests/{id}")]
        public async Task<IActionResult> RespondToRequest(int id, [FromBody] UpdateBuddyRequestStatus request)
        {
            try
            {
                int userId = GetCurrentUserId();
                var companion = await _context.TravelCompanions
                    .Include(tc => tc.Post)
                    .FirstOrDefaultAsync(tc => tc.CompanionID == id && tc.ReceiverID == userId);

                if (companion == null)
                    return NotFound("Request not found or access denied.");

                if (request.Status != "Accepted" && request.Status != "Declined")
                    return BadRequest("Invalid status. Must be 'Accepted' or 'Declined'.");

                companion.Status = request.Status;

                if (request.Status == "Accepted" && companion.Post != null)
                {
                    string groupName = $"Chuyến đi: {companion.Post.Title}";
                    
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
                        _context.ChatParticipants.Add(new ChatParticipant { ChatID = existingGroup.ChatID, UserID = companion.RequesterID });
                    }
                    else
                    {
                        if (!existingGroup.ChatParticipants.Any(cp => cp.UserID == companion.RequesterID))
                        {
                            _context.ChatParticipants.Add(new ChatParticipant { ChatID = existingGroup.ChatID, UserID = companion.RequesterID });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Request {request.Status.ToLower()}." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("requests/pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                int userId = GetCurrentUserId();

                var requests = await _context.TravelCompanions
                    .Include(tc => tc.Requester)
                    .Where(tc => tc.ReceiverID == userId && tc.Status == "Pending")
                    .Select(tc => new BuddyDto
                    {
                        CompanionID = tc.CompanionID,
                        BuddyUserID = tc.RequesterID,
                        BuddyUsername = tc.Requester.Username,
                        AvatarURL = tc.Requester.AvatarURL,
                        ConnectedDate = tc.DateRequested
                    })
                    .OrderByDescending(r => r.ConnectedDate)
                    .ToListAsync();

                return Ok(requests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAcceptedBuddies()
        {
            try
            {
                int userId = GetCurrentUserId();

                // A buddy connection exists if the user is either the requester or receiver and Status == "Accepted"
                var buddies = await _context.TravelCompanions
                    .Include(tc => tc.Requester)
                    .Include(tc => tc.Receiver)
                    .Where(tc => (tc.RequesterID == userId || tc.ReceiverID == userId) && tc.Status == "Accepted")
                    .Select(tc => new BuddyDto
                    {
                        CompanionID = tc.CompanionID,
                        BuddyUserID = tc.RequesterID == userId ? tc.ReceiverID : tc.RequesterID,
                        BuddyUsername = tc.RequesterID == userId ? tc.Receiver.Username : tc.Requester.Username,
                        AvatarURL = tc.RequesterID == userId ? tc.Receiver.AvatarURL : tc.Requester.AvatarURL,
                        ConnectedDate = tc.DateRequested // Using request date as connection date since there is no 'accepted date'
                    })
                    .ToListAsync();

                return Ok(buddies);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
