namespace TravelHub.Model
{
    public class ItineraryDetail
    {
        public int DetailID { get; set; }
        public int ItineraryID { get; set; }
        public int DestinationID { get; set; }
        public int DayNumber { get; set; }
        public string? TimeSlot { get; set; }
        public string? ActivityDescription { get; set; }
        public decimal? EstimatedCostVND { get; set; }

        public virtual Itinerary Itinerary { get; set; } = null!;
        public virtual Destination Destination { get; set; } = null!;
    }
}
