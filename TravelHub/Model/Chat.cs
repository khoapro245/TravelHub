using System;
using System.Collections.Generic;
namespace TravelHub.Model
{
    public class Chat
    {
        public int ChatID { get; set; }
        public string? ChatName { get; set; }
        public bool IsGroupChat { get; set; }
        public DateTime CreationDate { get; set; }

        public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}