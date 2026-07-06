using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Model
{
    public class GuideApplication
    {
        [Key]
        public int ApplicationID { get; set; }

        public int GuideID { get; set; } // The user ID of the guide applying
        
        public int PostID { get; set; } // The GuideRequest Post ID

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined

        public string? Message { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        public decimal? ProposedPriceVND { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("GuideID")]
        public virtual User Guide { get; set; } = null!;

        [ForeignKey("PostID")]
        public virtual Post Post { get; set; } = null!;
    }
}
