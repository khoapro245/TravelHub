using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Model
{
    public class TourGuideProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProfileID { get; set; }

        public int UserID { get; set; }

        // Thông tin cá nhân nhập khi đăng ký HDV
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; } // male, female, other, prefer-not-to-say

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Experience { get; set; } // 0-1, 1-3, 3-5, 5+

        [StringLength(255)]
        public string? Languages { get; set; }

        [StringLength(500)]
        public string? Locations { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [StringLength(500)]
        public string? TourCategories { get; set; }

        [StringLength(500)]
        public string? IdFrontUrl { get; set; }

        [StringLength(500)]
        public string? IdBackUrl { get; set; }

        [StringLength(500)]
        public string? CertUrl { get; set; }

        [StringLength(500)]
        public string? GuideAvatarUrl { get; set; }

        [StringLength(20)]
        public string IsVerified { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}
