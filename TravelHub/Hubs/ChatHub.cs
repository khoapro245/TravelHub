using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelHub.DTO;
using TravelHub.Model;

namespace TravelHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = Context.UserIdentifier; // SignalR uses UserIdentifier which maps to ClaimTypes.NameIdentifier by default if configured
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user token.");
        }

        // Client calls: JoinChat(conversationId)
        public async Task JoinChat(int conversationId)
        {
            int userId = GetCurrentUserId();

            // Verify the user is a participant
            var participants = await _context.ChatParticipants.Where(cp => cp.ChatID == conversationId).ToListAsync();
            var isParticipant = participants.Any(cp => cp.UserID == userId);

            if (isParticipant)
            {
                // Join the SignalR group for this conversation
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            }
            else
            {
                Console.WriteLine($"[JoinChat Error] User {userId} is NOT a participant in Chat {conversationId}. Current Participants: {string.Join(", ", participants.Select(p => p.UserID))}");
                throw new HubException("You are not a participant in this chat.");
            }
        }

        // Client calls: SendMessage(receiverId, chatId, message)
        public async Task SendMessage(int? receiverId, int? chatId, string content)
        {
            int senderId = GetCurrentUserId();
            Chat? chat = null;

            if (chatId.HasValue)
            {
                chat = await _context.Chats
                    .Include(c => c.ChatParticipants)
                    .FirstOrDefaultAsync(c => c.ChatID == chatId.Value);

                if (chat == null || !chat.ChatParticipants.Any(cp => cp.UserID == senderId))
                {
                    throw new HubException("Chat not found or access denied.");
                }
            }
            else if (receiverId.HasValue)
            {
                // Find an existing 1-on-1 chat between sender and receiver
                chat = await _context.Chats
                    .Where(c => !c.IsGroupChat && 
                                c.ChatParticipants.Any(cp => cp.UserID == senderId) && 
                                c.ChatParticipants.Any(cp => cp.UserID == receiverId.Value))
                    .FirstOrDefaultAsync();

                // If no chat exists, create a new one
                if (chat == null)
                {
                    chat = new Chat { IsGroupChat = false };
                    _context.Chats.Add(chat);
                    await _context.SaveChangesAsync(); // Get ChatID

                    _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = senderId });
                    _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = receiverId.Value });
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                throw new HubException("Must provide either receiverId or chatId.");
            }

            // Save the message
            var message = new Message
            {
                ChatID = chat.ChatID,
                SenderID = senderId,
                Content = content,
                SentDate = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Get sender info for DTO
            var sender = await _context.Users.FindAsync(senderId);

            var messageDto = new MessageDto
            {
                MessageID = message.MessageID,
                ChatID = message.ChatID,
                SenderID = message.SenderID,
                SenderUsername = sender?.Username ?? "Unknown",
                Content = message.Content,
                SentDate = message.SentDate
            };

            // Broadcast to the group (conversation ID)
            await Clients.Group(chat.ChatID.ToString()).SendAsync("ReceiveMessage", messageDto);
            
            // Send explicitly to users in case they haven't joined the SignalR group yet
            var participantsToNotify = chat.ChatParticipants.Where(p => p.UserID != senderId).ToList();
            foreach (var participant in participantsToNotify)
            {
                await Clients.User(participant.UserID.ToString()).SendAsync("ReceiveMessage", messageDto);
            }
            
            // Send to the sender as well to confirm
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
        }

        // Client calls: UserTyping(userId)
        // Wait, spec says UserTyping(userId), but normally they would send conversationId or receiverId. Let's pass receiverId.
        public async Task UserTyping(int receiverId)
        {
            int senderId = GetCurrentUserId();
            await Clients.User(receiverId.ToString()).SendAsync("UserTyping", senderId);
        }
    }
}
