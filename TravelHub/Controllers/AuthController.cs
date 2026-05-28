using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.DTO;
using TravelHub.Model;
using TravelHub.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using System.Security.Cryptography;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, ITokenService tokenService, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
            _configuration = configuration;
            _emailService = emailService;
        }

        private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
            
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserID = user.UserID,
                Username = user.Username
            };
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Username already exists.");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email already exists.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                StudentCode = request.StudentCode
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

            // Nếu không tìm thấy hoặc password sai thì báo lỗi
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            var response = await GenerateAuthResponseAsync(user);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(tokenRequest.AccessToken);
            if (principal == null)
                return BadRequest("Invalid access token.");

            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("Invalid token claims.");

            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.RefreshToken != tokenRequest.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid refresh token.");

            var response = await GenerateAuthResponseAsync(user);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("User not found.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Logged out successfully." });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var clientId = _configuration["GoogleAuth:ClientId"];
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { clientId ?? string.Empty }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                // Find user by GoogleID or Email
                var user = await _context.Users.SingleOrDefaultAsync(u => u.GoogleID == payload.Subject || u.Email == payload.Email);

                if (user == null)
                {
                    // Create new user if not exists
                    var baseUsername = payload.Email.Split('@')[0];
                    var newUsername = baseUsername;
                    int counter = 1;
                    while (await _context.Users.AnyAsync(u => u.Username == newUsername))
                    {
                        newUsername = $"{baseUsername}{counter}";
                        counter++;
                    }

                    user = new User
                    {
                        Username = newUsername,
                        Email = payload.Email,
                        GoogleID = payload.Subject,
                        FullName = payload.Name,
                        AvatarURL = payload.Picture,
                        RegistrationDate = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                }
                else if (string.IsNullOrEmpty(user.GoogleID))
                {
                    // Update existing user with GoogleID
                    user.GoogleID = payload.Subject;
                }

                var response = await GenerateAuthResponseAsync(user);
                return Ok(response);
            }
            catch (InvalidJwtException)
            {
                return BadRequest("Invalid Google token.");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return NotFound("Email not found.");
            }

            // Generate 6-digit random password securely
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var randomNumber = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
            var newPassword = randomNumber.ToString();

            // Hash and save
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Send email
            var subject = "Your New Password for TravelHub";
            var body = $"<p>Your password has been reset.</p><p>Your new password is: <strong>{newPassword}</strong></p><p>Please log in and change your password immediately.</p>";
            
            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error sending email: " + ex.Message);
            }

            return Ok(new { Message = "A new password has been sent to your email." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("User not found.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest("Incorrect old password.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
    }
}
