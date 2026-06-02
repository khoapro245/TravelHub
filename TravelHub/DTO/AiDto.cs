namespace TravelHub.DTO
{
    public class AiRecommendRequest
    {
        public decimal BudgetVND { get; set; }
        public int Days { get; set; }
        public string? Interests { get; set; } // e.g. "Nature, Food, History"
        public string? Departure { get; set; }
        public string? TransportationPreference { get; set; }
        public string? TravelGroup { get; set; }
        public string? DestinationType { get; set; }
        public string? MainTravelGoal { get; set; }
        public string? PreferredWeather { get; set; }
        public string? AccommodationType { get; set; }
        public string? BudgetStyle { get; set; }
    }

    public class AiRecommendResponse
    {
        public int DestinationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CityProvince { get; set; } = string.Empty;
        public string MatchReason { get; set; } = string.Empty;
        public decimal EstimatedCostVND { get; set; }
        public DailyCostBreakdown DailyCostBreakdown { get; set; } = new DailyCostBreakdown();
    }

    public class DailyCostBreakdown
    {
        public string Accommodation { get; set; } = string.Empty;
        public string Food { get; set; } = string.Empty;
        public string Transportation { get; set; } = string.Empty;
        public string Activities { get; set; } = string.Empty;
        public string Entertainment { get; set; } = string.Empty;
        public string Shopping { get; set; } = string.Empty;
    }

    public class AiGenerateItineraryRequest
    {
        public int DestinationID { get; set; }
        public int Days { get; set; }
        public string? TravelStyle { get; set; }
    }

    public class AiGenerateItineraryResponse
    {
        public string Title { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public List<AiDayItinerary> Days { get; set; } = new List<AiDayItinerary>();
    }

    public class AiDayItinerary
    {
        public int DayNumber { get; set; }
        public List<AiActivity> Activities { get; set; } = new List<AiActivity>();
    }

    public class AiActivity
    {
        public string Time { get; set; } = string.Empty; // e.g. "08:00 - 10:00"
        public string Description { get; set; } = string.Empty;
        public decimal EstimatedCostVND { get; set; }
    }
}
