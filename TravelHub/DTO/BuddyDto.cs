using System;

namespace TravelHub.DTO
{
    public class BuddyRecommendationDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? FullName { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public int MatchScore { get; set; }
    }

    public class CreateBuddyRequest
    {
        public int ReceiverID { get; set; }
        public int? PostID { get; set; } // Optional if connecting through a specific post
    }

    public class BuddyRequestResponseDto
    {
        public int CompanionID { get; set; }
        public int RequesterID { get; set; }
        public string RequesterUsername { get; set; } = string.Empty;
        public int ReceiverID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateRequested { get; set; }
    }

    public class UpdateBuddyRequestStatus
    {
        public string Status { get; set; } = string.Empty; // "Accepted" or "Declined"
    }

    public class BuddyDto
    {
        public int CompanionID { get; set; }
        public int BuddyUserID { get; set; }
        public string BuddyUsername { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public DateTime ConnectedDate { get; set; }
    }
}
