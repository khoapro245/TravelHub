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

            // Giả lập thanh toán thành công và cấp Premium 30 ngày
            user.IsPremium = true;
            user.PremiumExpiryDate = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Upgraded to Premium successfully.", IsPremium = true, ExpiryDate = user.PremiumExpiryDate });
        }
    }
}
