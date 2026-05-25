using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("recommend")]
        public async Task<IActionResult> RecommendDestinations([FromBody] AiRecommendRequest request)
        {
            // MOCK LOGIC: In a real app, this would call OpenAI or a similar service
            // passing the user's budget, days, and interests to get recommendations.
            // For now, we return dummy matching destinations from the DB.

            var allDestinations = await _context.Destinations.Take(3).ToListAsync();

            var recommendations = allDestinations.Select(d => new AiRecommendResponse
            {
                DestinationID = d.DestinationID,
                Name = d.Name,
                CityProvince = d.CityProvince,
                MatchReason = $"Perfect for {request.Interests ?? "your preferences"} on a {(request.BudgetVND < 5000000 ? "budget" : "premium")} trip.",
                EstimatedCostVND = d.EstimatedBaseCostVND ?? request.BudgetVND
            }).ToList();

            return Ok(recommendations);
        }

        [HttpPost("generate-itinerary")]
        public async Task<IActionResult> GenerateItinerary([FromBody] AiGenerateItineraryRequest request)
        {
            // MOCK LOGIC: In a real app, this would prompt an LLM to generate a day-by-day plan
            // based on the DestinationID, Days, and TravelStyle.

            var destination = await _context.Destinations.FindAsync(request.DestinationID);
            if (destination == null)
                return NotFound("Destination not found.");

            var response = new AiGenerateItineraryResponse
            {
                Title = $"{request.Days}-Day {request.TravelStyle ?? "Adventure"} in {destination.Name}",
                TotalDays = request.Days,
                Days = new List<AiDayItinerary>()
            };

            for (int i = 1; i <= request.Days; i++)
            {
                response.Days.Add(new AiDayItinerary
                {
                    DayNumber = i,
                    Activities = new List<AiActivity>
                    {
                        new AiActivity
                        {
                            Time = "09:00 - 12:00",
                            Description = $"Morning exploration of {destination.Name} main attractions.",
                            EstimatedCostVND = 200000
                        },
                        new AiActivity
                        {
                            Time = "12:30 - 14:00",
                            Description = "Lunch featuring local cuisine.",
                            EstimatedCostVND = 150000
                        },
                        new AiActivity
                        {
                            Time = "14:30 - 18:00",
                            Description = $"Afternoon {request.TravelStyle ?? "leisure"} activities.",
                            EstimatedCostVND = 300000
                        }
                    }
                });
            }

            return Ok(response);
        }
    }
}
