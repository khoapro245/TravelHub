using System;

namespace TravelHub.DTO
{
    public class ConversationDto
    {
        public int ChatID { get; set; }
        public string? ChatName { get; set; }
        public bool IsGroupChat { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
        public int ParticipantCount { get; set; }
        public int? OtherUserID { get; set; }
        public string? AvatarURL { get; set; }
    }

    public class MessageDto
    {
        public long MessageID { get; set; }
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? Content { get; set; }
        public DateTime SentDate { get; set; }
    }

    public class SendMessageDto
    {
        public int ReceiverID { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
