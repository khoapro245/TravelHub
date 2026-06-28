using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using System.Text.Json;
using System.Text;
using System.Security.Claims;

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
            
            // Lấy địa điểm tìm kiếm (ưu tiên Destination, fallback sang Departure nếu UI cũ chưa cập nhật)
            var searchLocation = !string.IsNullOrWhiteSpace(request.Destination) ? request.Destination : request.Departure;
            
            Console.WriteLine($"[AiController] Received request - Destination: '{request.Destination}', Departure: '{request.Departure}'");
            Console.WriteLine($"[AiController] Evaluated searchLocation: '{searchLocation}'");

            // Lọc theo CityProvince hoặc Name (chỉ lấy Name nếu trùng khớp hoàn toàn để tránh lỗi gõ 'Hồ Chí Minh' lại ra 'Lăng Chủ tịch Hồ Chí Minh')
            if (!string.IsNullOrWhiteSpace(searchLocation))
            {
                Console.WriteLine($"[AiController] Applying filter for searchLocation: {searchLocation}");
                query = query.Where(d => d.CityProvince.Contains(searchLocation) || d.Name == searchLocation);
            }
            else
            {
                Console.WriteLine($"[AiController] searchLocation is EMPTY! Skipping name/province filter.");
            }

            // Lọc theo ngân sách (nếu user nhập > 0)
            if (request.BudgetVND > 0)
            {
                // Cho phép độ chênh lệch ngân sách +/- 20%
                var minBudget = request.BudgetVND * 0.8m; 
                var maxBudget = request.BudgetVND * 1.2m; 
                query = query.Where(d => d.TotalTourCost >= minBudget && d.TotalTourCost <= maxBudget);
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
                var minBudget = request.BudgetVND * 0.8m;
                var maxBudget = request.BudgetVND * 1.2m;
                tourQuery = tourQuery.Where(t => t.PriceVND >= minBudget && t.PriceVND <= maxBudget);
            }
            var dbTours = await tourQuery.ToListAsync();

            // Lọc theo Sở thích (Interests) - Ép buộc phải có từ khóa
            if (!string.IsNullOrWhiteSpace(request.Interests))
            {
                var interests = request.Interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                dbDestinations = dbDestinations.Where(d => 
                    interests.Any(i => 
                        (d.KeyMain != null && d.KeyMain.Contains(i, StringComparison.OrdinalIgnoreCase)) ||
                        (d.Description != null && d.Description.Contains(i, StringComparison.OrdinalIgnoreCase))
                    )
                ).ToList();

                dbTours = dbTours.Where(t => 
                    interests.Any(i => 
                        (t.Title != null && t.Title.Contains(i, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Description != null && t.Description.Contains(i, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Destination != null && t.Destination.Contains(i, StringComparison.OrdinalIgnoreCase))
                    )
                ).ToList();
            }

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
                
                decimal estimatedCost = d.TotalTourCost ?? d.AccommodationCost ?? request.BudgetVND;
                int days = request.Days > 0 ? request.Days : 3;
                decimal dailyBudget = estimatedCost / days;
                
                var response = new AiRecommendResponse
                {
                    DestinationID = d.DestinationID,
                    Name = d.Name,
                    CityProvince = d.CityProvince,
                    MatchReason = score > 50 ? "Địa điểm lý tưởng cực kỳ phù hợp với sở thích của bạn." : "Một trong những địa điểm tuyệt vời có trong hệ thống.",
                    Distance = "Tùy vị trí",
                    EstimatedCostVND = estimatedCost,
                    ImageUrl = d.Image ?? string.Empty,
                    DailyCostBreakdown = new DailyCostBreakdown
                    {
                        Accommodation = $"~{Math.Round(dailyBudget * 0.4m):N0} VNĐ",
                        Food = $"~{Math.Round(dailyBudget * 0.3m):N0} VNĐ",
                        Transportation = $"~{Math.Round(dailyBudget * 0.1m):N0} VNĐ",
                        Activities = $"~{Math.Round(dailyBudget * 0.1m):N0} VNĐ",
                        Entertainment = $"~{Math.Round(dailyBudget * 0.05m):N0} VNĐ",
                        Shopping = $"~{Math.Round(dailyBudget * 0.05m):N0} VNĐ"
                    }
                };
                
                scoredItems.Add(new { Response = response, Score = score });
            }

            // Sắp xếp ưu tiên Tour (Score cao nhất), sau đó sắp xếp theo Giá (từ cao xuống thấp)
            var finalSortedItems = scoredItems
                .OrderByDescending(x => (int)x.Score)
                .ThenByDescending(x => ((AiRecommendResponse)x.Response).EstimatedCostVND)
                .ToList();

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
Also provide a `dailyCostBreakdown` object containing estimated daily cost ranges in VND (as strings, e.g., ""300.000đ""). 
CRITICAL RULE 1: The sum of the dailyCostBreakdown values multiplied by the number of days ({request.Days}) MUST roughly equal the EstimatedCostVND.
{(request.BudgetVND > 0 ? $"CRITICAL RULE 2: The EstimatedCostVND MUST strictly be within +/- 20% of the user's Budget ({request.BudgetVND} VND)." : "")}
CRITICAL RULE 3: If 'Destination to' is specified and not empty, you MUST ONLY recommend places within that specific destination.
CRITICAL RULE 4: The recommendations MUST strictly feature the user's Interests ({request.Interests}).

Return the response exactly as a JSON array matching this structure, without any markdown formatting or extra text:
[
  {{
    ""name"": ""Destination Name"",
    ""cityProvince"": ""City/Country Name"",
    ""matchReason"": ""Detailed reason here..."",
    ""distance"": ""1200 km"",
    ""estimatedCostVND"": 1000000,
    ""dailyCostBreakdown"": {{
      ""accommodation"": ""150.000đ"",
      ""food"": ""100.000đ"",
      ""transportation"": ""50.000đ"",
      ""activities"": ""30.000đ"",
      ""entertainment"": ""0đ"",
      ""shopping"": ""0đ""
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

        [Authorize]
        [HttpPost("generate-itinerary")]
        public async Task<IActionResult> GenerateItinerary([FromBody] AiGenerateItineraryRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Bạn cần đăng nhập để sử dụng tính năng này.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized("Tài khoản không tồn tại.");
            }

            if (user.Role != "Admin" && user.AiGenerationCount >= 5)
            {
                return StatusCode(403, "Bạn đã sử dụng hết 5 lượt tạo lịch trình bằng AI.");
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Gemini API Key is missing.");

            var destination = await _context.Destinations.FindAsync(request.DestinationID);
            if (destination == null)
                return NotFound("Destination not found.");

            var prompt = $@"
You are an expert travel planner. Create a day-by-day itinerary for a {request.Days}-day trip to {destination.Name}, {destination.CityProvince}.
Travel style: {request.TravelStyle ?? "General"}.
{(request.BudgetVND > 0 ? $"\nCRITICAL RULE: The user has a total budget of {request.BudgetVND} VND for this {request.Days}-day itinerary. You MUST strictly ensure that the sum of `estimatedCostVND` for ALL activities across ALL days is approximately equal to {request.BudgetVND} VND. Distribute this budget logically across meals, attractions, and activities." : "")}

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
                
                if (user.Role != "Admin")
                {
                    user.AiGenerationCount += 1;
                    await _context.SaveChangesAsync();
                }

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
            var baseUrl = _configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com";
            var url = $"{baseUrl.TrimEnd('/')}/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            
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
