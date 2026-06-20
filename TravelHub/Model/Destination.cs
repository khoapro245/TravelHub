using System.Collections.Generic;
namespace TravelHub.Model
{
    public class Destination
    {
        public int DestinationID { get; set; }
        public string Name { get; set; } = null!;
        public string CityProvince { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? Rate { get; set; }
        public string? Image { get; set; }
        public string? KeyMain { get; set; }
        public decimal? EntranceFee { get; set; }
        public decimal? AccommodationCost { get; set; }
        public decimal? TotalTourCost { get; set; }
        public decimal? TourPricePerPerson { get; set; }

        // Quan hệ 1 - Nhiều với chi tiết lịch trình
        public virtual ICollection<ItineraryDetail> ItineraryDetails { get; set; } = new List<ItineraryDetail>();
    }
}