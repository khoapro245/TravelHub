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
        public bool IsBlocked { get; set; }
        public string Role { get; set; } = "Customer";
    }

    public class AdminUserResponse
    {
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<AdminUserDto> Users { get; set; } = new List<AdminUserDto>();
    }

    // Chi tiết người dùng cho màn hình xem của Admin
    public class AdminUserDetailDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? AvatarURL { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? StudentCode { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = "Customer";
        public bool IsBlocked { get; set; }
        public bool IsPremium { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastOnline { get; set; }
    }

    // Body cho thao tác chỉnh sửa người dùng của Admin
    public class AdminUpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? StudentCode { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; }
    }

    // Body cho thao tác chặn / bỏ chặn người dùng
    public class AdminBlockUserRequest
    {
        public bool IsBlocked { get; set; }
    }
}
