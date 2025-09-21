using BookingTour.Models;
using BookingTour.Services.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BookingTour.Services
{
    public class AuthService : IAuthService
    {
        private readonly TourBookingSystemContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        public AuthService(
            TourBookingSystemContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await ValidateUserAsync(request.Username, request.Password);
                if (user == null || !user.IsActive.GetValueOrDefault())
                {
                    return null;
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token to database
                await SaveRefreshTokenAsync(user.UserId, refreshToken);

                return new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(
                        int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
                    User = new UserInfo
                    {
                        Id = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.DefaultRole.RoleName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return null;
            }
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await GetUserByUsernameAsync(request.Username);
                if (existingUser != null)
                {
                    return null; // User already exists
                }

                // Check if email already exists
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
                if (existingEmail != null)
                {
                    return null; // Email already exists
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DefaultRoleId = _context.Roles.FirstOrDefault(c=>c.RoleName.Equals("Customer")).RoleId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate tokens
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                await SaveRefreshTokenAsync(user.UserId, refreshToken);

                return new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(
                        int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
                    User = new UserInfo
                    {
                        Id = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.DefaultRole.RoleName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
                return null;
            }
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
        {
            //try
            //{
            //    var storedRefreshToken = await _context.RefreshTokens
            //        .Include(rt => rt.User)
            //        .FirstOrDefaultAsync(rt => rt.Token == refreshToken
            //            && rt.ExpiresAt > DateTime.UtcNow
            //            && rt.IsActive);

            //    if (storedRefreshToken == null)
            //    {
            //        return null;
            //    }

            //    // Generate new tokens
            //    var newToken = GenerateJwtToken(storedRefreshToken.User);
            //    var newRefreshToken = GenerateRefreshToken();

            //    // Revoke old refresh token
            //    storedRefreshToken.IsActive = false;
            //    storedRefreshToken.RevokedAt = DateTime.UtcNow;

            //    // Save new refresh token
            //    await SaveRefreshTokenAsync(storedRefreshToken.User.Id, newRefreshToken);

            //    return new AuthResponse
            //    {
            //        Token = newToken,
            //        RefreshToken = newRefreshToken,
            //        ExpiresAt = DateTime.UtcNow.AddMinutes(
            //            int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
            //        User = new UserInfo
            //        {
            //            Id = storedRefreshToken.User.Id,
            //            Username = storedRefreshToken.User.Username,
            //            Email = storedRefreshToken.User.Email,
            //            Role = storedRefreshToken.User.Role
            //        }
            //    };
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error during token refresh");
            //    return null;
            //}
            return null;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            //try
            //{
            //    var storedRefreshToken = await _context.RefreshTokens
            //        .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            //    if (storedRefreshToken == null)
            //    {
            //        return false;
            //    }

            //    storedRefreshToken.IsActive = false;
            //    storedRefreshToken.RevokedAt = DateTime.UtcNow;
            //    await _context.SaveChangesAsync();

            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error revoking refresh token");
            //    return false;
            //}
            return true;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null || !VerifyPassword(password, user.PasswordHash))
                {
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {Username}", username);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.DefaultRole)
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                return null;
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.DefaultRole.RoleName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Username", user.Username)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "YourAppIssuer";
            var audience = jwtSettings["Audience"] ?? "YourAppAudience";
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserId", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.DefaultRole.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false // We don't validate lifetime here
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        private async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            //var token = new RefreshToken
            //{
            //    Token = refreshToken,
            //    UserId = userId,
            //    ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token expires in 7 days
            //    CreatedAt = DateTime.UtcNow,
            //    IsActive = true
            //};

            ////_context.RefreshTokens.Add(token);
            //await _context.SaveChangesAsync();
        }
    }
}
