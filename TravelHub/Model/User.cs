using System;
using System.Collections.Generic;

namespace TravelHub.Model
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PasswordHash { get; set; }
        public string? GoogleID { get; set; }
        public string? AvatarURL { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? StudentCode { get; set; }
        public string? Gender { get; set; }
        public DateTime RegistrationDate { get; set; }

        // Quan hệ 1 - 1
        public virtual UserPreference? UserPreference { get; set; }

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