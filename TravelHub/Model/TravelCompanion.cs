using System;
namespace TravelHub.Model
{
    public class TravelCompanion
    {
        public int CompanionID { get; set; }
        public int? PostID { get; set; }
        public int RequesterID { get; set; }
        public int ReceiverID { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime DateRequested { get; set; }

        public virtual Post? Post { get; set; }
        public virtual User Requester { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
    }
}