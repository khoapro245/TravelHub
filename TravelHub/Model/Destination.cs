using System.Collections.Generic;
namespace TravelHub.Model
{
    public class Destination
    {
        public int DestinationID { get; set; }
        public string Name { get; set; } = null!;
        public string CityProvince { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? EstimatedBaseCostVND { get; set; }
        public string? OpenWeatherMapCityID { get; set; }

        // Quan hệ 1 - Nhiều với chi tiết lịch trình
        public virtual ICollection<ItineraryDetail> ItineraryDetails { get; set; } = new List<ItineraryDetail>();
    }
}