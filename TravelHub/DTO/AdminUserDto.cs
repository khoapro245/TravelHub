using System;

namespace TravelHub.DTO
{
    public class AdminUserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? AvatarURL { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastOnline { get; set; }
        public string OfflineDurationText { get; set; } = string.Empty;
    }

    public class AdminUserResponse
    {
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<AdminUserDto> Users { get; set; } = new List<AdminUserDto>();
    }
}
