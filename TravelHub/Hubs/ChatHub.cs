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
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatID == conversationId && cp.UserID == userId);

            if (isParticipant)
            {
                // Join the SignalR group for this conversation
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            }
            else
            {
                throw new HubException("You are not a participant in this chat.");
            }
        }

        // Client calls: SendMessage(receiverId, message) - Note: The spec says receiverId, but usually it's conversationId. 
        // We will implement logic to find or create the 1-on-1 chat if passing receiverId, or send to conversation if it's conversationId.
        // Let's implement based on receiverId as per spec.
        public async Task SendMessage(int receiverId, string content)
        {
            int senderId = GetCurrentUserId();

            // Find an existing 1-on-1 chat between sender and receiver
            var chat = await _context.Chats
                .Where(c => !c.IsGroupChat && 
                            c.ChatParticipants.Any(cp => cp.UserID == senderId) && 
                            c.ChatParticipants.Any(cp => cp.UserID == receiverId))
                .FirstOrDefaultAsync();

            // If no chat exists, create a new one
            if (chat == null)
            {
                chat = new Chat { IsGroupChat = false };
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync(); // Get ChatID

                _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = senderId });
                _context.ChatParticipants.Add(new ChatParticipant { ChatID = chat.ChatID, UserID = receiverId });
                await _context.SaveChangesAsync();
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
            
            // Also explicitly send to the receiver if they are connected (useful if they haven't joined the specific group yet)
            // SignalR allows sending to specific users via UserIdentifier
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messageDto);
            
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
