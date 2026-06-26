using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TravelHub.Model;

namespace TravelHub.Middleware
{
    /// <summary>
    /// Chặn ngay lập tức các request đã xác thực đến từ tài khoản bị khoá,
    /// kể cả khi access token chưa hết hạn.
    /// </summary>
    public class BlockedUserMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    var isBlocked = await dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.UserID == userId)
                        .Select(u => u.IsBlocked)
                        .FirstOrDefaultAsync();

                    if (isBlocked)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"message\":\"Your account has been blocked.\"}");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
