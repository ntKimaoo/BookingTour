using BookingTour.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookingTour.Models;
using BookingTour.Services.DTOs;

namespace BookingTour.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizeController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthorizeController> _logger;

        public AuthorizeController(IAuthService authService, ILogger<AuthorizeController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RegisterAsync(request);
            if (response == null)
            {
                return BadRequest(new { message = "User registration failed. Username or email may already exist." });
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            return Ok(response);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            if (!result)
            {
                return BadRequest(new { message = "Failed to revoke token" });
            }

            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userInfo = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Fullname=user.FullName,
                Email = user.Email,
                Role = user.DefaultRole.RoleName
            };

            return Ok(userInfo);
        }
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Nếu token dùng cookie, xóa cookie
            Response.Cookies.Delete("jwtToken");
            return Ok(new { message = "Đã đăng xuất thành công" });
        }
        [Authorize]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Đã authorize" });
        }
    }
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}


