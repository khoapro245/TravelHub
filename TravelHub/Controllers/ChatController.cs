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
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
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

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                int userId = GetCurrentUserId();

                var conversations = await _context.Chats
                    .Include(c => c.ChatParticipants)
                    .Include(c => c.Messages)
                    .Where(c => c.ChatParticipants.Any(cp => cp.UserID == userId))
                    .Select(c => new ConversationDto
                    {
                        ChatID = c.ChatID,
                        ChatName = c.IsGroupChat ? c.ChatName : c.ChatParticipants.FirstOrDefault(cp => cp.UserID != userId)!.User.Username,
                        IsGroupChat = c.IsGroupChat,
                        LastMessage = c.Messages.OrderByDescending(m => m.SentDate).Select(m => m.Content).FirstOrDefault(),
                        LastMessageDate = c.Messages.OrderByDescending(m => m.SentDate).Select(m => (DateTime?)m.SentDate).FirstOrDefault(),
                        ParticipantCount = c.ChatParticipants.Count
                    })
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();

                return Ok(conversations);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("conversations/{id}/messages")]
        public async Task<IActionResult> GetMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                int userId = GetCurrentUserId();

                // Check if user is part of the chat
                var isParticipant = await _context.ChatParticipants
                    .AnyAsync(cp => cp.ChatID == id && cp.UserID == userId);

                if (!isParticipant)
                    return Forbid("You do not have access to this conversation.");

                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ChatID == id)
                    .OrderByDescending(m => m.SentDate);

                var totalCount = await query.CountAsync();

                var messages = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new MessageDto
                    {
                        MessageID = m.MessageID,
                        ChatID = m.ChatID,
                        SenderID = m.SenderID,
                        SenderUsername = m.Sender.Username,
                        Content = m.Content,
                        SentDate = m.SentDate
                    })
                    .ToListAsync();

                // Reverse back to chronological order for the client UI
                messages.Reverse();

                var result = new PaginatedList<MessageDto>
                {
                    Items = messages,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
