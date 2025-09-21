using BookingTour.Controllers;
using BookingTour.Models;
using System.Security.Claims;
using BookingTour.Services.DTOs;

namespace BookingTour.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<User?> ValidateUserAsync(string username, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        ClaimsPrincipal CreateClaimsPrincipal(User user);
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
