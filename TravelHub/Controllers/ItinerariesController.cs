using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelHub.DTO;
using TravelHub.Model;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItinerariesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItinerariesController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user token.");
        }

        [HttpGet]
        public async Task<IActionResult> GetMyItineraries()
        {
            try
            {
                int userId = GetCurrentUserId();
                var itineraries = await _context.Itineraries
                    .Where(i => i.UserID == userId)
                    .Select(i => new ItineraryDto
                    {
                        ItineraryID = i.ItineraryID,
                        UserID = i.UserID,
                        TripName = i.TripName,
                        StartDate = i.StartDate,
                        EndDate = i.EndDate,
                        TotalBudgetEstimatedVND = i.TotalBudgetEstimatedVND,
                        Status = i.Status
                    })
                    .ToListAsync();

                return Ok(itineraries);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItinerary(int id)
        {
            try
            {
                int userId = GetCurrentUserId();
                var itinerary = await _context.Itineraries
                    .Include(i => i.ItineraryDetails)
                        .ThenInclude(d => d.Destination)
                    .FirstOrDefaultAsync(i => i.ItineraryID == id && i.UserID == userId);

                if (itinerary == null)
                    return NotFound("Itinerary not found or access denied.");

                var dto = new ItineraryDto
                {
                    ItineraryID = itinerary.ItineraryID,
                    UserID = itinerary.UserID,
                    TripName = itinerary.TripName,
                    StartDate = itinerary.StartDate,
                    EndDate = itinerary.EndDate,
                    TotalBudgetEstimatedVND = itinerary.TotalBudgetEstimatedVND,
                    Status = itinerary.Status,
                    Details = itinerary.ItineraryDetails.Select(d => new ItineraryDetailDto
                    {
                        DetailID = d.DetailID,
                        DestinationID = d.DestinationID,
                        DestinationName = d.Destination.Name,
                        DayNumber = d.DayNumber,
                        TimeSlot = d.TimeSlot,
                        ActivityDescription = d.ActivityDescription,
                        EstimatedCostVND = d.EstimatedCostVND
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateItinerary([FromBody] CreateItineraryRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                var itinerary = new Itinerary
                {
                    UserID = userId,
                    TripName = request.TripName,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalBudgetEstimatedVND = request.TotalBudgetEstimatedVND,
                    Status = "Planned"
                };

                if (request.Details != null && request.Details.Any())
                {
                    foreach (var detail in request.Details)
                    {
                        itinerary.ItineraryDetails.Add(new ItineraryDetail
                        {
                            DestinationID = detail.DestinationID,
                            DayNumber = detail.DayNumber,
                            TimeSlot = detail.TimeSlot,
                            ActivityDescription = detail.ActivityDescription,
                            EstimatedCostVND = detail.EstimatedCostVND
                        });
                    }
                }

                _context.Itineraries.Add(itinerary);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetItinerary), new { id = itinerary.ItineraryID }, new { Message = "Itinerary created successfully", ItineraryID = itinerary.ItineraryID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItinerary(int id, [FromBody] UpdateItineraryRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();
                var itinerary = await _context.Itineraries
                    .FirstOrDefaultAsync(i => i.ItineraryID == id && i.UserID == userId);

                if (itinerary == null)
                    return NotFound("Itinerary not found or access denied.");

                itinerary.TripName = request.TripName ?? itinerary.TripName;
                itinerary.StartDate = request.StartDate ?? itinerary.StartDate;
                itinerary.EndDate = request.EndDate ?? itinerary.EndDate;
                itinerary.TotalBudgetEstimatedVND = request.TotalBudgetEstimatedVND ?? itinerary.TotalBudgetEstimatedVND;
                itinerary.Status = request.Status ?? itinerary.Status;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Itinerary updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItinerary(int id)
        {
            try
            {
                int userId = GetCurrentUserId();
                var itinerary = await _context.Itineraries
                    .Include(i => i.ItineraryDetails)
                    .Include(i => i.Budgets)
                    .FirstOrDefaultAsync(i => i.ItineraryID == id && i.UserID == userId);

                if (itinerary == null)
                    return NotFound("Itinerary not found or access denied.");

                // EF Core might handle cascade delete if configured, but we can explicitly remove them
                _context.ItineraryDetails.RemoveRange(itinerary.ItineraryDetails);
                _context.Budgets.RemoveRange(itinerary.Budgets);
                _context.Itineraries.Remove(itinerary);

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Itinerary deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
