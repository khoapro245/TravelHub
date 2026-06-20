using System;

namespace TravelHub.DTO
{
    public class TourGuideRegistrationRequest
    {
        public string? Experience { get; set; }
        public string? Languages { get; set; }
        public string? Locations { get; set; }
        public string? Bio { get; set; }
        public string? TourCategories { get; set; }
        public string? IdFrontUrl { get; set; }
        public string? IdBackUrl { get; set; }
        public string? CertUrl { get; set; }
        public string? GuideAvatarUrl { get; set; }
    }

    public class TourGuideProfileDto
    {
        public int ProfileID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Experience { get; set; }
        public string? Languages { get; set; }
        public string? Locations { get; set; }
        public string? Bio { get; set; }
        public string? TourCategories { get; set; }
        public string? IdFrontUrl { get; set; }
        public string? IdBackUrl { get; set; }
        public string? CertUrl { get; set; }
        public string? GuideAvatarUrl { get; set; }
        public string IsVerified { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminApproveGuideRequest
    {
        public int ProfileID { get; set; }
        public bool Approve { get; set; } // true = Approve, false = Reject
    }
}
