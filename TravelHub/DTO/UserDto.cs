using System;

namespace TravelHub.DTO
{
    public class UserProfileDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? StudentCode { get; set; }
        public string? Gender { get; set; }
        public DateTime RegistrationDate { get; set; }
        
        // Preferences embedded
        public decimal? PreferredBudgetVND { get; set; }
        public string? TravelStyle { get; set; }
        public string? FavoriteActivities { get; set; }
        public int? MaxDurationDays { get; set; }
        public string? PreferredDestinations { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? AvatarURL { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        
        // Preferences to update
        public decimal? PreferredBudgetVND { get; set; }
        public string? TravelStyle { get; set; }
        public string? FavoriteActivities { get; set; }
        public int? MaxDurationDays { get; set; }
        public string? PreferredDestinations { get; set; }
    }

    public class PublicUserProfileDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        
        // Public preferences
        public string? TravelStyle { get; set; }
        public string? FavoriteActivities { get; set; }
        public string? PreferredDestinations { get; set; }
    }

    public class DashboardDto
    {
        public int UpcomingTripsCount { get; set; }
        public int PendingBuddyRequestsCount { get; set; }
        public int SavedDestinationsCount { get; set; }
    }
}
