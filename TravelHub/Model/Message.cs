using System;
namespace TravelHub.Model
{
    public class Message
    {
        public long MessageID { get; set; } // Dùng kiểu long tương ứng với BIGINT
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string? Content { get; set; }
        public DateTime SentDate { get; set; }

        public virtual Chat Chat { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
    }
}