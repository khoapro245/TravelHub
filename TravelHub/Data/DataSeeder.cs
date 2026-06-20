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
    }
}
