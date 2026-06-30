using System;

namespace TravelHub.DTO
{
    public class TourBookingRequestDto
    {
        public int TourID { get; set; }
        public string TourTitle { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime DepartureDate { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public int Guests { get; set; }
        public decimal TotalPriceVND { get; set; }
    }

    public class TourBookingResponseDto
    {
        public int BookingID { get; set; }
        public int TourID { get; set; }
        public string TourTitle { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime DepartureDate { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public int Guests { get; set; }
        public decimal TotalPriceVND { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Optionally include user info for admin
        public int UserID { get; set; }
        public string? Username { get; set; }
    }
}
