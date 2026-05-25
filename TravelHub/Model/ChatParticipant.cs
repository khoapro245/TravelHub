using System;
namespace TravelHub.Model
{
    public class ChatParticipant
    {
        public int ChatParticipantID { get; set; }
        public int ChatID { get; set; }
        public int UserID { get; set; }
        public DateTime JoinedDate { get; set; }

        public virtual Chat Chat { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}