using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravelHub.Model
{
    public class User
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(50)] // Giới hạn độ dài để tối ưu index tìm kiếm
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        // 🟢 GIẢI PHÁP: Cấp đủ dung lượng để chứa trọn vẹn chuỗi băm BCrypt (60 ký tự)
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [StringLength(100)]
        public string? GoogleID { get; set; }

        [StringLength(500)] // URL ảnh có thể rất dài nên để rộng rãi
        public string? AvatarURL { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? StudentCode { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(255)]
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastOnline { get; set; }
        
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Customer, TourGuide, Admin

        public bool IsPremium { get; set; } = false;
        public DateTime? PremiumExpiryDate { get; set; }

        // Khi bị chặn, người dùng không thể đăng nhập hoặc truy cập các API yêu cầu xác thực
        public bool IsBlocked { get; set; } = false;

        // Quan hệ 1 - 1
        public virtual UserPreference? UserPreference { get; set; }
        public virtual TourGuideProfile? TourGuideProfile { get; set; }

        // Quan hệ 1 - Nhiều
        public virtual ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        // Quan hệ bạn đồng hành (Người gửi và Người nhận)
        public virtual ICollection<TravelCompanion> SentRequests { get; set; } = new List<TravelCompanion>();
        public virtual ICollection<TravelCompanion> ReceivedRequests { get; set; } = new List<TravelCompanion>();
    }
}