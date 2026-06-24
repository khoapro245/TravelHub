using System.Collections.Generic;

namespace TravelHub.DTO
{
    public class AdminOverviewResponse
    {
        public AdminStats Stats { get; set; } = new AdminStats();
        public List<UserGrowthData> UserGrowth { get; set; } = new List<UserGrowthData>();
        public List<DestinationData> DestinationDistribution { get; set; } = new List<DestinationData>();
        public List<RecentUserDto> RecentUsers { get; set; } = new List<RecentUserDto>();
    }

    public class AdminStats
    {
        public int TotalUsers { get; set; }
        public int ActiveDestinations { get; set; }
        public int TotalPosts { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class UserGrowthData
    {
        public string Month { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Active { get; set; }
    }

    public class DestinationData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = "#3B82F6";
    }

    public class RecentUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public string Joined { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }
}
