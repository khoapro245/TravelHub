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
    // [Authorize]
    public class AiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiController> _logger;

        public AiController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AiController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("recommend")]
        public async Task<IActionResult> RecommendDestinations([FromBody] AiRecommendRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Gemini API Key is missing.");

            // 1. Quét Database trước
            var query = _context.Destinations.AsQueryable();
            
            // Lấy địa điểm tìm kiếm (từ trường Destination hoặc Departure vì UI gửi qua Departure)
            var searchLocation = !string.IsNullOrWhiteSpace(request.Destination) ? request.Destination : request.Departure;
            
            // Lọc theo CityProvince hoặc Name
            if (!string.IsNullOrWhiteSpace(searchLocation))
            {
                query = query.Where(d => d.CityProvince.Contains(searchLocation) || d.Name.Contains(searchLocation));
            }

            // Lọc theo ngân sách (nếu user nhập > 0)
            if (request.BudgetVND > 0)
            {
                // Cho phép độ chênh lệch ngân sách khoảng 10%
                var maxBudget = request.BudgetVND * 1.1m; 
                query = query.Where(d => d.TotalTourCost <= maxBudget);
            }

            var dbDestinations = await query.ToListAsync();
            
            // Query Tours
            var tourQuery = _context.Tours.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchLocation))
            {
                tourQuery = tourQuery.Where(t => t.Destination.Contains(searchLocation) || t.Title.Contains(searchLocation));
            }
            if (request.BudgetVND > 0)
            {
                var maxBudget = request.BudgetVND * 1.1m;
                tourQuery = tourQuery.Where(t => t.PriceVND <= maxBudget);
            }
            var dbTours = await tourQuery.ToListAsync();

            var scoredItems = new List<dynamic>();

            foreach (var t in dbTours)
            {
                var response = new AiRecommendResponse
                {
                    DestinationID = t.TourID + 20000,
                    Name = t.Title,
                    CityProvince = t.Destination,
                    MatchReason = "⭐ Tour đặc biệt từ Hướng dẫn viên TravelHub rất phù hợp với bạn!",
                    Distance = "Tùy vị trí",
                    EstimatedCostVND = t.PriceVND,
                    ImageUrl = t.ImageUrl ?? string.Empty,
                    DailyCostBreakdown = new DailyCostBreakdown
                    {
                        Accommodation = "Bao gồm trong Tour",
                        Activities = "Bao gồm trong Tour",
                        Food = "Bao gồm trong Tour",
                        Transportation = "Bao gồm trong Tour",
                        Entertainment = "Tùy chọn",
                        Shopping = "Tùy chọn"
                    }
                };
                
                scoredItems.Add(new { Response = response, Score = 100 });
            }

            // Chấm điểm ưu tiên theo KeyMain
            foreach (var d in dbDestinations)
            {
                int score = 50; // default score for destination matching search
                if (!string.IsNullOrWhiteSpace(request.Interests) && !string.IsNullOrWhiteSpace(d.KeyMain))
                {
                    var interests = request.Interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var interest in interests)
                    {
                        if (d.KeyMain.Contains(interest, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 10;
                        }
                    }
                }
                
                var response = new AiRecommendResponse
                {
                    DestinationID = d.DestinationID,
                    Name = d.Name,
                    CityProvince = d.CityProvince,
                    MatchReason = score > 50 ? "Địa điểm lý tưởng cực kỳ phù hợp với sở thích của bạn." : "Một trong những địa điểm tuyệt vời có trong hệ thống.",
                    Distance = "Tùy vị trí",
                    EstimatedCostVND = d.TotalTourCost ?? d.AccommodationCost ?? 0,
                    ImageUrl = d.Image ?? string.Empty,
                    DailyCostBreakdown = new DailyCostBreakdown
                    {
                        Accommodation = d.AccommodationCost?.ToString() ?? "N/A",
                        Activities = d.EntranceFee?.ToString() ?? "N/A",
                        Food = "Tự túc",
                        Transportation = "Tự túc",
                        Entertainment = "Tùy chọn",
                        Shopping = "Tùy chọn"
                    }
                };
                
                scoredItems.Add(new { Response = response, Score = score });
            }

            var finalSortedItems = scoredItems.OrderByDescending(x => x.Score).ToList();

            if (finalSortedItems.Any())
            {
                int totalCount = finalSortedItems.Count;
                var pagedItems = finalSortedItems
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => (AiRecommendResponse)x.Response)
                    .ToList();

                return Ok(new PaginatedAiResponse
                {
                    Items = pagedItems,
                    TotalCount = totalCount,
                    Page = request.Page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                });
            }

            // 2. Nếu Database rỗng (không tìm thấy gì), fallback gọi AI
            var prompt = $@"
You are an expert travel assistant. Based on the following user preferences, recommend the top {request.PageSize} best matching destinations anywhere in the world (or specifically matching their criteria):
- Budget: {request.BudgetVND} VND
- Days: {request.Days}
- Interests: {request.Interests}
- Departure from: {request.Departure}
- Destination to: {request.Destination}
- Transportation: {request.TransportationPreference}
- Travel Group: {request.TravelGroup}
- Destination Type: {request.DestinationType}
- Main Goal: {request.MainTravelGoal}
- Preferred Weather: {request.PreferredWeather}
- Accommodation: {request.AccommodationType}
- Budget Style: {request.BudgetStyle}

For each destination, provide the Name, CityProvince (or Country), a MatchReason explaining why it's a good fit based on ALL their specific preferences, and an EstimatedCostVND (total estimated cost for {request.Days} days).
Also provide a `distance` field which is the estimated distance (e.g. ""1200 km"") from {request.Departure} to the recommended destination.
Also provide a `dailyCostBreakdown` object containing estimated daily cost ranges in VND (as strings, e.g., ""300.000đ - 500.000đ"") for the following categories: accommodation, food, transportation, activities, entertainment, shopping.

Return the response exactly as a JSON array matching this structure, without any markdown formatting or extra text:
[
  {{
    ""name"": ""Destination Name"",
    ""cityProvince"": ""City/Country Name"",
    ""matchReason"": ""Detailed reason here..."",
    ""distance"": ""1200 km"",
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
                _logger.LogError(ex, "Gemini API call failed");
                return StatusCode(500, "Unable to process AI response.");
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
                                TotalTourCost = rec.EstimatedCostVND
                            };
                            _context.Destinations.Add(newDest);
                            await _context.SaveChangesAsync();
                            rec.DestinationID = newDest.DestinationID;
                        }
                    }

                    // Wrap the AI results in PaginatedAiResponse
                    return Ok(new PaginatedAiResponse
                    {
                        Items = recommendations,
                        TotalCount = recommendations.Count,
                        Page = 1,
                        TotalPages = 1
                    });
                }
                
                return Ok(new PaginatedAiResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response. Response was: {ResultStr}", resultStr);
                return StatusCode(500, "Unable to process AI response.");
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
                _logger.LogError(ex, "Gemini API call failed for itinerary generation");
                return StatusCode(500, "Unable to process AI response.");
            }

            try
            {
                var itinerary = JsonSerializer.Deserialize<AiGenerateItineraryResponse>(resultStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(itinerary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI itinerary response. Response was: {ResultStr}", resultStr);
                return StatusCode(500, "Unable to process AI response.");
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
                    
                if (text != null)
                {
                    text = text.Trim();
                    if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                    {
                        text = text.Substring(7);
                    }
                    else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                    {
                        text = text.Substring(3);
                    }
                    
                    if (text.EndsWith("```"))
                    {
                        text = text.Substring(0, text.Length - 3);
                    }
                    text = text.Trim();
                }

                return text ?? throw new Exception("Parsed text is null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing Gemini response JSON: {ex.Message}. Raw response: {responseJson}");
            }
        }
    }
}
