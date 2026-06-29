using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelHub.Model;

namespace TravelHub.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDestinationsAsync(AppDbContext context, string contentRootPath)
        {
            var jsonPath = Path.Combine(contentRootPath, "Data", "places.json");
            
            if (!File.Exists(jsonPath))
            {
                return;
            }

            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var destinations = JsonSerializer.Deserialize<List<Destination>>(jsonData, options);

            // Xóa dữ liệu lỗi do string replace sai từ các lần chạy trước
            var corruptedDestinations = await context.Destinations
                .Where(d => d.CityProvince.Contains("000") || d.Name.Contains("000") || d.Description.Contains("000"))
                .ToListAsync();
            
            if (corruptedDestinations.Any())
            {
                context.Destinations.RemoveRange(corruptedDestinations);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DataSeeder] Removed {corruptedDestinations.Count} corrupted destinations.");
            }

            if (destinations == null || !destinations.Any())
            {
                return;
            }

            int addedCount = 0;

            foreach (var dest in destinations)
            {
                bool exists = await context.Destinations.AnyAsync(d => d.Name == dest.Name && d.CityProvince == dest.CityProvince);
                if (!exists)
                {
                    context.Destinations.Add(dest);
                    addedCount++;
                }
                else
                {
                    // Cập nhật giá cho các dòng đã bị insert với giá NULL ở lần trước
                    var existing = await context.Destinations.FirstOrDefaultAsync(d => d.Name == dest.Name && d.CityProvince == dest.CityProvince);
                    if (existing != null)
                    {
                        bool isUpdated = false;
                        if (existing.EntranceFee == null && dest.EntranceFee != null) { existing.EntranceFee = dest.EntranceFee; isUpdated = true; }
                        if (existing.AccommodationCost == null && dest.AccommodationCost != null) { existing.AccommodationCost = dest.AccommodationCost; isUpdated = true; }
                        if (existing.TotalTourCost == null && dest.TotalTourCost != null) { existing.TotalTourCost = dest.TotalTourCost; isUpdated = true; }
                        if (existing.TourPricePerPerson == null && dest.TourPricePerPerson != null) { existing.TourPricePerPerson = dest.TourPricePerPerson; isUpdated = true; }
                        

                        
                        if (isUpdated) 
                        {
                            context.Destinations.Update(existing);
                            addedCount++;
                        }
                    }
                }
            }

            if (addedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[DataSeeder] Seeded {addedCount} new destinations.");
            }
            else
            {
                Console.WriteLine("[DataSeeder] No new destinations to seed.");
            }
        }
        public static async Task SeedDefaultTourGuideAsync(AppDbContext context)
        {
            var guideEmail = "guide@travelhub.com";
            var guidePassword = "Password123!"; // Will be hashed

            if (!await context.Users.AnyAsync(u => u.Email == guideEmail))
            {
                var guideUser = new User
                {
                    Username = "DefaultGuide",
                    Email = guideEmail,
                    FullName = "Nguyễn Hướng Dẫn",
                    Role = "TourGuide",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(guidePassword),
                    RegistrationDate = DateTime.UtcNow
                };

                context.Users.Add(guideUser);
                await context.SaveChangesAsync(); // To get the UserID

                var guideProfile = new TourGuideProfile
                {
                    UserID = guideUser.UserID,
                    Experience = "3-5",
                    Languages = "Tiếng Việt, Tiếng Anh",
                    Locations = "Hà Nội, Sapa, Ninh Bình",
                    Bio = "Tôi là một hướng dẫn viên đam mê khám phá văn hóa và ẩm thực.",
                    TourCategories = "Tour Văn Hóa, Tour Ẩm Thực",
                    IsVerified = "Approved",
                    CreatedAt = DateTime.UtcNow
                };

                context.TourGuideProfiles.Add(guideProfile);
                await context.SaveChangesAsync();

                Console.WriteLine($"[DataSeeder] Seeded default Tour Guide: {guideEmail}");
            }
        }
    }
}
