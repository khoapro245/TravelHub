namespace TravelHub.Model
{
    public class UserPreference
    {
        public int PreferenceID { get; set; }
        public int UserID { get; set; }
        public decimal? PreferredBudgetVND { get; set; }
        public string? TravelStyle { get; set; }
        public string? FavoriteActivities { get; set; }
        public int? MaxDurationDays { get; set; }
        public string? PreferredDestinations { get; set; }

        // Liên kết ngược về User
        public virtual User User { get; set; } = null!;
    }
}
