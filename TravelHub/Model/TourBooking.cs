using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Model
{
    public class TourBooking
    {
        [Key]
        public int BookingID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User User { get; set; }

        public int TourID { get; set; }

        // Tour Snapshot data
        public string TourTitle { get; set; }
        public string Destination { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime DepartureDate { get; set; }

        // Booking details
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public int Guests { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPriceVND { get; set; }

        public DateTime BookingDate { get; set; }
        public string Status { get; set; } // Pending, Confirmed, Cancelled
    }
}
