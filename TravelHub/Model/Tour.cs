using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Model
{
    public class Tour
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TourID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DepartureLocation { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureDate { get; set; }

        [Required]
        public int DurationDays { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceVND { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }
        
        public int NumberOfBookings { get; set; } = 0;
    }
}
