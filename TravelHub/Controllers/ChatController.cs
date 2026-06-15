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
                        ParticipantCount = c.ChatParticipants.Count,
                        OtherUserID = !c.IsGroupChat ? (int?)c.ChatParticipants.Where(cp => cp.UserID != userId).Select(cp => cp.UserID).FirstOrDefault() : null,
                        AvatarURL = !c.IsGroupChat ? c.ChatParticipants.Where(cp => cp.UserID != userId).Select(cp => cp.User.AvatarURL).FirstOrDefault() : null
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
                        AvatarURL = m.Sender.AvatarURL,
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

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatDto request)
        {
            try
            {
                int userId = GetCurrentUserId();

                var chat = new Chat
                {
                    ChatName = request.ChatName,
                    IsGroupChat = true
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Add creator
                _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = userId });
                
                // Add other participants
                if (request.ParticipantUserIDs != null)
                {
                    foreach (var pId in request.ParticipantUserIDs.Distinct())
                    {
                        if (pId != userId)
                        {
                            _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = pId });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Group chat created successfully.", ChatID = chat.ChatID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("{id}/participants")]
        public async Task<IActionResult> AddParticipant(int id, [FromBody] AddParticipantDto request)
        {
            try
            {
                int userId = GetCurrentUserId();

                // Verify the chat exists and is a group chat, and current user is in it
                var chat = await _context.Chats
                    .Include(c => c.ChatParticipants)
                    .FirstOrDefaultAsync(c => c.ChatID == id && c.IsGroupChat);

                if (chat == null)
                    return NotFound("Group chat not found.");

                if (!chat.ChatParticipants.Any(cp => cp.UserID == userId))
                    return Forbid("You are not a participant of this group.");

                if (chat.ChatParticipants.Any(cp => cp.UserID == request.UserID))
                    return BadRequest("User is already in the group.");

                _context.ChatParticipants.Add(new ChatParticipant { ChatID = id, UserID = request.UserID });
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Participant added successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
        {
            try
            {
                int senderId = GetCurrentUserId();
                Chat chat = null;

                if (request.ChatID.HasValue)
                {
                    // Sending to an existing chat
                    chat = await _context.Chats
                        .Include(c => c.ChatParticipants)
                        .FirstOrDefaultAsync(c => c.ChatID == request.ChatID.Value);

                    if (chat == null)
                        return NotFound("Chat not found.");

                    if (!chat.ChatParticipants.Any(cp => cp.UserID == senderId))
                        return Forbid("You are not a participant in this chat.");
                }
                else if (request.ReceiverID.HasValue)
                {
                    // Find an existing 1-on-1 chat between sender and receiver
                    chat = await _context.Chats
                        .Where(c => !c.IsGroupChat && 
                                    c.ChatParticipants.Any(cp => cp.UserID == senderId) && 
                                    c.ChatParticipants.Any(cp => cp.UserID == request.ReceiverID.Value))
                        .FirstOrDefaultAsync();

                    // If no chat exists, create a new one
                    if (chat == null)
                    {
                        chat = new Chat { IsGroupChat = false };
                        _context.Chats.Add(chat);
                        await _context.SaveChangesAsync(); // Get ChatID

                        _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = senderId });
                        _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = request.ReceiverID.Value });
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    return BadRequest("Either ChatID or ReceiverID must be provided.");
                }

                // Save the message
                var message = new Message
                {
                    ChatID = chat.ChatID,
                    SenderID = senderId,
                    Content = request.Content
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Message sent successfully.", MessageID = message.MessageID, ChatID = chat.ChatID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpDelete("groups/{id}")]
        public async Task<IActionResult> DisbandGroupChat(int id)
        {
            try
            {
                int userId = GetCurrentUserId();

                var chat = await _context.Chats
                    .Include(c => c.ChatParticipants)
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.ChatID == id && c.IsGroupChat);

                if (chat == null)
                    return NotFound("Group chat not found.");

                if (!chat.ChatParticipants.Any(cp => cp.UserID == userId))
                    return Forbid("You are not a participant of this group.");

                // Manual cascade delete just to be safe
                _context.Messages.RemoveRange(chat.Messages);
                _context.ChatParticipants.RemoveRange(chat.ChatParticipants);
                _context.Chats.Remove(chat);
                
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Group disbanded successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(long messageId)
        {
            try
            {
                int userId = GetCurrentUserId();

                var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageID == messageId);

                if (message == null)
                    return NotFound("Message not found.");

                if (message.SenderID != userId)
                    return Forbid("You can only delete your own messages.");

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                // Broadcast deletion via SignalR?
                // Ideally yes, but for now we just return Ok. The client can refresh or remove locally.

                return Ok(new { Message = "Message deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
