using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost("upgrade-premium")]
        public async Task<IActionResult> UpgradePremium()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("User not found.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (user.TravelPoints < 50000)
            {
                return BadRequest(new 
                { 
                    Error = "InsufficientPoints", 
                    Message = "Rất tiếc, bạn cần 50,000 Travel Point để thực hiện hành động này.", 
                    CurrentBalance = user.TravelPoints 
                });
            }

            user.TravelPoints -= 50000;
            user.IsPremium = true;
            user.PremiumExpiryDate = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Upgraded to Premium successfully.", IsPremium = true, ExpiryDate = user.PremiumExpiryDate, TravelPoints = user.TravelPoints });
        }

        [Authorize]
        [HttpPost("recharge-points")]
        public async Task<IActionResult> RechargePoints([FromBody] RechargeRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("User not found.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.TravelPoints += request.Points;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Nạp thành công {request.Points} Travel Point.", TravelPoints = user.TravelPoints });
        }
    }

    public class RechargeRequest
    {
        public int Points { get; set; }
    }
}
