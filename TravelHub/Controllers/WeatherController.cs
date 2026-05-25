using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WeatherController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WeatherController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{destinationId}")]
        public async Task<IActionResult> GetForecast(int destinationId, [FromQuery] int days = 7)
        {
            try
            {
                var destination = await _context.Destinations.FindAsync(destinationId);
                if (destination == null)
                {
                    return NotFound("Destination not found.");
                }

                // In a production application, this endpoint would make an HTTP call to OpenWeatherMap API
                // using the OpenWeatherMapCityID or latitude/longitude.
                // For this implementation, we will generate mock forecast data based on the spec.

                var random = new Random();
                var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Thunderstorm" };
                
                var response = new WeatherForecastDto
                {
                    DestinationID = destination.DestinationID,
                    DestinationName = destination.Name,
                    OpenWeatherMapCityID = destination.OpenWeatherMapCityID
                };

                for (int i = 0; i < days; i++)
                {
                    var condition = conditions[random.Next(conditions.Length)];
                    var temp = random.Next(20, 38) + random.NextDouble(); // Mock temp for Vietnam

                    response.Forecasts.Add(new DailyWeatherDto
                    {
                        Date = DateTime.UtcNow.Date.AddDays(i),
                        TemperatureCelsius = Math.Round(temp, 1),
                        Condition = condition,
                        IconURL = $"https://openweathermap.org/img/wn/10d@2x.png", // Mock icon URL
                        HumidityPercentage = random.Next(60, 95)
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching weather data: {ex.Message}");
            }
        }
    }
}
