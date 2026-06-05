using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTours([FromQuery] string? destination, [FromQuery] string? departureLocation, [FromQuery] DateTime? departureDate)
        {
            var tourQuery = _context.Tours.AsQueryable();
            var destQuery = _context.Destinations.AsQueryable();

            if (!string.IsNullOrEmpty(destination) && destination != "Tất cả")
            {
                tourQuery = tourQuery.Where(t => t.Destination.Contains(destination) || t.Title.Contains(destination));
                destQuery = destQuery.Where(d => d.CityProvince.Contains(destination) || d.Name.Contains(destination));
            }

            if (!string.IsNullOrEmpty(departureLocation) && departureLocation != "Tất cả")
            {
                tourQuery = tourQuery.Where(t => t.DepartureLocation.Contains(departureLocation));
                // DestQuery remains unfiltered for departure since it's a fixed location
            }

            if (departureDate.HasValue)
            {
                tourQuery = tourQuery.Where(t => t.DepartureDate.Date == departureDate.Value.Date);
            }

            var tours = await tourQuery.Select(t => new TourResponse
            {
                TourID = t.TourID,
                Title = t.Title,
                Destination = t.Destination,
                DepartureLocation = t.DepartureLocation,
                DepartureDate = t.DepartureDate,
                DurationDays = t.DurationDays,
                PriceVND = t.PriceVND,
                ImageUrl = t.ImageUrl,
                Description = t.Description,
                NumberOfBookings = t.NumberOfBookings
            }).ToListAsync();

            var dests = await destQuery.ToListAsync(); // Fetch into memory to use Random
            
            var random = new Random();
            var mappedDests = dests.Select(d => new TourResponse
            {
                TourID = d.DestinationID + 10000, 
                Title = d.Name,
                Destination = d.CityProvince,
                DepartureLocation = "Tự túc",
                DepartureDate = departureDate ?? DateTime.Now.AddDays(random.Next(1, 10)),
                DurationDays = 1,
                PriceVND = d.EstimatedBaseCostVND ?? 500000,
                ImageUrl = "https://images.unsplash.com/photo-1599839619722-39751411ea63?w=800",
                Description = d.Description ?? "Tham quan địa danh nổi tiếng",
                NumberOfBookings = random.Next(10, 200)
            }).ToList();

            tours.AddRange(mappedDests);

            return Ok(tours);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTourDetails(int id)
        {
            if (id >= 10000)
            {
                var destId = id - 10000;
                var d = await _context.Destinations.FirstOrDefaultAsync(x => x.DestinationID == destId);
                if (d == null) return NotFound();

                var random = new Random(id);
                return Ok(new TourResponse
                {
                    TourID = id,
                    Title = d.Name,
                    Destination = d.CityProvince,
                    DepartureLocation = "Tự túc",
                    DepartureDate = DateTime.Now.AddDays(random.Next(1, 10)),
                    DurationDays = 1,
                    PriceVND = d.EstimatedBaseCostVND ?? 500000,
                    ImageUrl = "https://images.unsplash.com/photo-1599839619722-39751411ea63?w=800",
                    Description = d.Description ?? "Tham quan địa danh nổi tiếng",
                    NumberOfBookings = random.Next(10, 200)
                });
            }
            else
            {
                var t = await _context.Tours.FindAsync(id);
                if (t == null) return NotFound();

                return Ok(new TourResponse
                {
                    TourID = t.TourID,
                    Title = t.Title,
                    Destination = t.Destination,
                    DepartureLocation = t.DepartureLocation,
                    DepartureDate = t.DepartureDate,
                    DurationDays = t.DurationDays,
                    PriceVND = t.PriceVND,
                    ImageUrl = t.ImageUrl,
                    Description = t.Description,
                    NumberOfBookings = t.NumberOfBookings
                });
            }
        }

        [HttpGet("destinations")]
        public async Task<IActionResult> GetPopularDestinations()
        {
            var dbDestinations = await _context.Destinations
                .Select(d => d.CityProvince)
                .Distinct()
                .ToListAsync();

            var tourDestinations = await _context.Tours
                .Select(t => t.Destination)
                .Distinct()
                .ToListAsync();

            var destinations = dbDestinations.Concat(tourDestinations).Distinct().Take(20).ToList();

            if (!destinations.Any())
            {
                destinations = new List<string> { "Trung Quốc", "Nhật Bản", "Châu Âu", "Singapore", "Thái Lan", "Hàn Quốc", "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Phú Quốc" };
            }

            return Ok(destinations);
        }

        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookTour([FromBody] TourBookingRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("User not found in token.");
            }

            var booking = new TourBooking
            {
                UserID = userId,
                TourID = request.TourID,
                TourTitle = request.TourTitle,
                Destination = request.Destination,
                ImageUrl = request.ImageUrl,
                DepartureDate = request.DepartureDate,
                FullName = request.FullName,
                Phone = request.Phone,
                Email = string.IsNullOrEmpty(request.Email) ? null : request.Email,
                Notes = string.IsNullOrEmpty(request.Notes) ? null : request.Notes,
                Guests = request.Guests,
                TotalPriceVND = request.TotalPriceVND,
                BookingDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.TourBookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking successful", bookingId = booking.BookingID });
        }

        [HttpGet("bookings/user")]
        [Authorize]
        public async Task<IActionResult> GetUserBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var bookings = await _context.TourBookings
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpGet("bookings")]
        [Authorize]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _context.TourBookings
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            return Ok(bookings);
        }

        [HttpPut("bookings/{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusDto dto)
        {
            if (string.IsNullOrEmpty(dto.Status)) return BadRequest("Status is required");
            
            var booking = await _context.TourBookings.FindAsync(id);
            if (booking == null) return NotFound("Booking not found");

            booking.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking status updated successfully" });
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedTours()
        {
            if (await _context.Tours.AnyAsync())
            {
                return BadRequest("Tours already exist in database.");
            }

            var seedTours = new List<Tour>
            {
                new Tour { Title = "Tour Trung Quốc 5N5Đ: HCM - Thượng Hải - Vô Tích", Destination = "Trung Quốc", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(5), DurationDays = 5, PriceVND = 16990000, ImageUrl = "https://images.unsplash.com/photo-1508804185872-d7bad890e092?w=800", Description = "Khám phá vẻ đẹp Trung Quốc", NumberOfBookings = 136 },
                new Tour { Title = "Tour Nhật Bản 5N4Đ: Tokyo - Phú Sĩ", Destination = "Nhật Bản", DepartureLocation = "Hà Nội", DepartureDate = DateTime.Now.AddDays(7), DurationDays = 5, PriceVND = 24990000, ImageUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=800", Description = "Ngắm hoa anh đào rực rỡ", NumberOfBookings = 41 },
                new Tour { Title = "Tour Đà Nẵng 3N2Đ: Bà Nà Hills - Hội An", Destination = "Đà Nẵng", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(2), DurationDays = 3, PriceVND = 3500000, ImageUrl = "https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800", Description = "Du lịch miền Trung trọn gói", NumberOfBookings = 32 },
                new Tour { Title = "Tour Thái Lan 4N3Đ: Bangkok - Pattaya", Destination = "Thái Lan", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(10), DurationDays = 4, PriceVND = 5990000, ImageUrl = "https://images.unsplash.com/photo-1506665531195-3566af2b4dfa?w=800", Description = "Thiên đường mua sắm", NumberOfBookings = 24 },
                new Tour { Title = "Tour Singapore 3N2Đ: Marina Bay Sands", Destination = "Singapore", DepartureLocation = "Hà Nội", DepartureDate = DateTime.Now.AddDays(15), DurationDays = 3, PriceVND = 8500000, ImageUrl = "https://images.unsplash.com/photo-1525625293386-3f8f99389edd?w=800", Description = "Đảo quốc sư tử", NumberOfBookings = 38 },
                new Tour { Title = "Tour Châu Âu 9N8Đ: Pháp - Thụy Sĩ - Ý", Destination = "Châu Âu", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(20), DurationDays = 9, PriceVND = 55990000, ImageUrl = "https://images.unsplash.com/photo-1499856871958-5b9627545d1a?w=800", Description = "Hành trình khám phá Châu Âu cổ kính", NumberOfBookings = 21 }
            };

            _context.Tours.AddRange(seedTours);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Seeded tours successfully!" });
        }
    }
}
