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
    public class DestinationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DestinationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDestinations(
            [FromQuery] string? search,
            [FromQuery] string? budget,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Destinations.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.Name.Contains(search) || d.CityProvince.Contains(search));
            }

            // Mocks for budget/category as the Destination model might not have them explicitly
            if (!string.IsNullOrEmpty(budget))
            {
                if (budget.ToLower() == "low")
                {
                    query = query.Where(d => d.TotalTourCost <= 2000000);
                }
                else if (budget.ToLower() == "medium")
                {
                    query = query.Where(d => d.TotalTourCost > 2000000 && d.TotalTourCost <= 5000000);
                }
                else if (budget.ToLower() == "high")
                {
                    query = query.Where(d => d.TotalTourCost > 5000000);
                }
            }

            // Assuming Description contains category keywords as a fallback
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.Description != null && d.Description.Contains(category));
            }

            var totalCount = await query.CountAsync();
            var destinations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DestinationDto
                {
                    DestinationID = d.DestinationID,
                    Name = d.Name,
                    CityProvince = d.CityProvince,
                    Description = d.Description,
                    Rate = d.Rate,
                    Image = d.Image,
                    KeyMain = d.KeyMain,
                    EntranceFee = d.EntranceFee,
                    AccommodationCost = d.AccommodationCost,
                    TotalTourCost = d.TotalTourCost,
                    TourPricePerPerson = d.TourPricePerPerson
                })
                .ToListAsync();

            var result = new PaginatedList<DestinationDto>
            {
                Items = destinations,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingDestinations([FromQuery] int limit = 5)
        {
            // For now, mock trending by grabbing random or top destinations
            // In a real app, this could order by count of ItineraryDetails
            
            var trendingDestinations = await _context.Destinations
                .Include(d => d.ItineraryDetails)
                .OrderByDescending(d => d.ItineraryDetails.Count)
                .Take(limit)
                .Select(d => new DestinationDto
                {
                    DestinationID = d.DestinationID,
                    Name = d.Name,
                    CityProvince = d.CityProvince,
                    Description = d.Description,
                    Rate = d.Rate,
                    Image = d.Image,
                    KeyMain = d.KeyMain,
                    EntranceFee = d.EntranceFee,
                    AccommodationCost = d.AccommodationCost,
                    TotalTourCost = d.TotalTourCost,
                    TourPricePerPerson = d.TourPricePerPerson
                })
                .ToListAsync();

            return Ok(trendingDestinations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDestinationDetails(int id)
        {
            var destination = await _context.Destinations
                .FirstOrDefaultAsync(d => d.DestinationID == id);

            if (destination == null)
            {
                return NotFound("Destination not found.");
            }

            var result = new DestinationDto
            {
                DestinationID = destination.DestinationID,
                Name = destination.Name,
                CityProvince = destination.CityProvince,
                Description = destination.Description,
                Rate = destination.Rate,
                Image = destination.Image,
                KeyMain = destination.KeyMain,
                EntranceFee = destination.EntranceFee,
                AccommodationCost = destination.AccommodationCost,
                TotalTourCost = destination.TotalTourCost,
                TourPricePerPerson = destination.TourPricePerPerson
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDestination([FromBody] CreateDestinationRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var destination = new Destination
                {
                    Name = request.Name,
                    CityProvince = request.CityProvince,
                    Description = request.Description,
                    Rate = request.Rate,
                    Image = request.Image,
                    KeyMain = request.KeyMain,
                    EntranceFee = request.EntranceFee,
                    AccommodationCost = request.AccommodationCost,
                    TotalTourCost = request.TotalTourCost,
                    TourPricePerPerson = request.TourPricePerPerson
                };

                _context.Destinations.Add(destination);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Destination created successfully", destinationId = destination.DestinationID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDestination(int id, [FromBody] UpdateDestinationRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var destination = await _context.Destinations.FindAsync(id);
                if (destination == null) return NotFound("Destination not found");

                destination.Name = request.Name;
                destination.CityProvince = request.CityProvince;
                destination.Description = request.Description;
                destination.Rate = request.Rate;
                destination.Image = request.Image;
                destination.KeyMain = request.KeyMain;
                destination.EntranceFee = request.EntranceFee;
                destination.AccommodationCost = request.AccommodationCost;
                destination.TotalTourCost = request.TotalTourCost;
                destination.TourPricePerPerson = request.TourPricePerPerson;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Destination updated successfully", destinationId = destination.DestinationID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, details = ex.InnerException?.Message });
            }
        }
    }
}
