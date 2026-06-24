using System;
using System.Collections.Generic;

namespace TravelHub.DTO
{
    public class TourResponse
    {
        public int TourID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DepartureLocation { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public int DurationDays { get; set; }
        public string? DurationText { get; set; }
        public decimal PriceVND { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int NumberOfBookings { get; set; }
    }

    public class TourSearchRequest
    {
        public string? Destination { get; set; }
        public string? DepartureLocation { get; set; }
        public DateTime? DepartureDate { get; set; }
    }

    public class CreateTourRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DepartureLocation { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public int DurationDays { get; set; }
        public string? DurationText { get; set; }
        public decimal PriceVND { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
