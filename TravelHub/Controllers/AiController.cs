using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using System.Text.Json;
using System.Text;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AiController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("recommend")]
        public async Task<IActionResult> RecommendDestinations([FromBody] AiRecommendRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Gemini API Key is missing.");

            var prompt = $@"
You are an expert travel assistant. Based on the following user preferences, recommend the top 3 best matching destinations anywhere in the world (or specifically matching their criteria):
- Budget: {request.BudgetVND} VND
- Days: {request.Days}
- Interests: {request.Interests}
- Departure from: {request.Departure}
- Transportation: {request.TransportationPreference}
- Travel Group: {request.TravelGroup}
- Destination Type: {request.DestinationType}
- Main Goal: {request.MainTravelGoal}
- Preferred Weather: {request.PreferredWeather}
- Accommodation: {request.AccommodationType}
- Budget Style: {request.BudgetStyle}

For each destination, provide the Name, CityProvince (or Country), a MatchReason explaining why it's a good fit based on ALL their specific preferences, and an EstimatedCostVND (total estimated cost for {request.Days} days).
Also provide a `dailyCostBreakdown` object containing estimated daily cost ranges in VND (as strings, e.g., ""300.000đ - 500.000đ"") for the following categories: accommodation, food, transportation, activities, entertainment, shopping.

Return the response exactly as a JSON array matching this structure, without any markdown formatting or extra text:
[
  {{
    ""name"": ""Destination Name"",
    ""cityProvince"": ""City/Country Name"",
    ""matchReason"": ""Detailed reason here..."",
    ""estimatedCostVND"": 1000000,
    ""dailyCostBreakdown"": {{
      ""accommodation"": ""300.000đ - 500.000đ"",
      ""food"": ""200.000đ - 400.000đ"",
      ""transportation"": ""100.000đ - 200.000đ"",
      ""activities"": ""150.000đ - 300.000đ"",
      ""entertainment"": ""100.000đ - 200.000đ"",
      ""shopping"": ""100.000đ - 300.000đ""
    }}
  }}
]";

            string resultStr;
            try
            {
                resultStr = await CallGeminiApi(prompt, apiKey);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gemini API call failed: {ex.Message}");
            }

            try
            {
                var recommendations = JsonSerializer.Deserialize<List<AiRecommendResponse>>(resultStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (recommendations != null)
                {
                    foreach (var rec in recommendations)
                    {
                        var existingDest = await _context.Destinations.FirstOrDefaultAsync(d => d.Name == rec.Name);
                        if (existingDest != null)
                        {
                            rec.DestinationID = existingDest.DestinationID;
                        }
                        else
                        {
                            var newDest = new Destination
                            {
                                Name = rec.Name,
                                CityProvince = rec.CityProvince,
                                Description = "AI Suggested Destination",
                                EstimatedBaseCostVND = rec.EstimatedCostVND
                            };
                            _context.Destinations.Add(newDest);
                            await _context.SaveChangesAsync();
                            rec.DestinationID = newDest.DestinationID;
                        }
                    }
                }
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error parsing AI response: {ex.Message}. Response was: {resultStr}");
            }
        }

        [HttpPost("generate-itinerary")]
        public async Task<IActionResult> GenerateItinerary([FromBody] AiGenerateItineraryRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Gemini API Key is missing.");

            var destination = await _context.Destinations.FindAsync(request.DestinationID);
            if (destination == null)
                return NotFound("Destination not found.");

            var prompt = $@"
You are an expert travel planner. Create a day-by-day itinerary for a {request.Days}-day trip to {destination.Name}, {destination.CityProvince}.
Travel style: {request.TravelStyle ?? "General"}.

Return the response exactly as a JSON object matching this structure, without any markdown formatting or extra text:
{{
  ""title"": ""Your catchy title for the trip"",
  ""totalDays"": {request.Days},
  ""days"": [
    {{
      ""dayNumber"": 1,
      ""activities"": [
        {{
          ""time"": ""08:00 - 10:00"",
          ""description"": ""Activity description"",
          ""estimatedCostVND"": 50000
        }}
      ]
    }}
  ]
}}
Make sure to generate exactly {request.Days} days.";

            string resultStr;
            try
            {
                resultStr = await CallGeminiApi(prompt, apiKey);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gemini API call failed: {ex.Message}");
            }

            try
            {
                var itinerary = JsonSerializer.Deserialize<AiGenerateItineraryResponse>(resultStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(itinerary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error parsing AI response: {ex.Message}. Response was: {resultStr}");
            }
        }

        private async Task<string> CallGeminiApi(string prompt, string apiKey)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new 
                {
                    response_mime_type = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(url, content);
            
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error ({response.StatusCode}): {responseJson}");
            }

            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var text = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                    
                return text ?? throw new Exception("Parsed text is null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing Gemini response JSON: {ex.Message}. Raw response: {responseJson}");
            }
        }
    }
}
