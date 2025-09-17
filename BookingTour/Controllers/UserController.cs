using BookingTour.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BookingTour.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly TourBookingSystemContext _context; // Replace with your actual DbContext name

        public UserController(TourBookingSystemContext context)
        {
            _context = context;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDeleted = false)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.DefaultRole)
                    .AsQueryable();

                if (!includeDeleted)
                {
                    query = query.Where(u => u.IsDelete != true);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Address = u.Address,
                        DateOfBirth = u.DateOfBirth,
                        CreatedDate = u.CreatedDate,
                        ModifyDate = u.ModifyDate,
                        IsActive = u.IsActive,
                        DefaultRoleId = u.DefaultRoleId,
                        DefaultRoleName = u.DefaultRole != null ? u.DefaultRole.RoleName : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Data = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.DefaultRole)
                    .FirstOrDefaultAsync(u => u.UserId == id && u.IsDelete != true);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                var userDto = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth,
                    CreatedDate = user.CreatedDate,
                    ModifyDate = user.ModifyDate,
                    IsActive = user.IsActive,
                    DefaultRoleId = user.DefaultRoleId,
                    DefaultRoleName = user.DefaultRole?.RoleName
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == createUserDto.Username);

                if (existingUser != null)
                {
                    return BadRequest(new { Message = "Username already exists" });
                }

                // Check if email already exists
                if (!string.IsNullOrEmpty(createUserDto.Email))
                {
                    var existingEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == createUserDto.Email);

                    if (existingEmail != null)
                    {
                        return BadRequest(new { Message = "Email already exists" });
                    }
                }

                var user = new User
                {
                    Username = createUserDto.Username,
                    PasswordHash = HashPassword(createUserDto.Password),
                    FullName = createUserDto.FullName,
                    Email = createUserDto.Email,
                    Phone = createUserDto.Phone,
                    Address = createUserDto.Address,
                    DateOfBirth = createUserDto.DateOfBirth,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsDelete = false,
                    DefaultRoleId = createUserDto.DefaultRoleId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Load the created user with role information
                var createdUser = await _context.Users
                    .Include(u => u.DefaultRole)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                var userDto = new UserDto
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    FullName = createdUser.FullName,
                    Email = createdUser.Email,
                    Phone = createdUser.Phone,
                    Address = createdUser.Address,
                    DateOfBirth = createdUser.DateOfBirth,
                    CreatedDate = createdUser.CreatedDate,
                    ModifyDate = createdUser.ModifyDate,
                    IsActive = createdUser.IsActive,
                    DefaultRoleId = createdUser.DefaultRoleId,
                    DefaultRoleName = createdUser.DefaultRole?.RoleName
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // PUT: api/User/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null || user.IsDelete == true)
                {
                    return NotFound(new { Message = "User not found" });
                }

                // Check if username is being changed and if it already exists
                if (updateUserDto.Username != user.Username)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == updateUserDto.Username && u.UserId != id);

                    if (existingUser != null)
                    {
                        return BadRequest(new { Message = "Username already exists" });
                    }
                }

                // Check if email is being changed and if it already exists
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    var existingEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == updateUserDto.Email && u.UserId != id);

                    if (existingEmail != null)
                    {
                        return BadRequest(new { Message = "Email already exists" });
                    }
                }

                // Update user properties
                user.Username = updateUserDto.Username;
                user.FullName = updateUserDto.FullName;
                user.Email = updateUserDto.Email;
                user.Phone = updateUserDto.Phone;
                user.Address = updateUserDto.Address;
                user.DateOfBirth = updateUserDto.DateOfBirth;
                user.ModifyDate = DateTime.Now;
                user.IsActive = updateUserDto.IsActive;
                user.DefaultRoleId = updateUserDto.DefaultRoleId;

                // Update password if provided
                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    user.PasswordHash = HashPassword(updateUserDto.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // DELETE: api/User/{id} (Soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null || user.IsDelete == true)
                {
                    return NotFound(new { Message = "User not found" });
                }

                // Soft delete
                user.IsDelete = true;
                user.IsActive = false;
                user.ModifyDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // PUT: api/User/{id}/activate
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null || user.IsDelete == true)
                {
                    return NotFound(new { Message = "User not found" });
                }

                user.IsActive = true;
                user.ModifyDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // PUT: api/User/{id}/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null || user.IsDelete == true)
                {
                    return NotFound(new { Message = "User not found" });
                }

                user.IsActive = false;
                user.ModifyDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // GET: api/User/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers(
            [FromQuery] string? keyword,
            [FromQuery] int? roleId,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.DefaultRole)
                    .Where(u => u.IsDelete != true)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u =>
                        u.Username.Contains(keyword) ||
                        u.FullName.Contains(keyword) ||
                        u.Email.Contains(keyword) ||
                        u.Phone.Contains(keyword));
                }

                if (roleId.HasValue)
                {
                    query = query.Where(u => u.DefaultRoleId == roleId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Address = u.Address,
                        DateOfBirth = u.DateOfBirth,
                        CreatedDate = u.CreatedDate,
                        ModifyDate = u.ModifyDate,
                        IsActive = u.IsActive,
                        DefaultRoleId = u.DefaultRoleId,
                        DefaultRoleName = u.DefaultRole != null ? u.DefaultRole.RoleName : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Data = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        // Private method to hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

// DTOs for the API
public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifyDate { get; set; }
    public bool? IsActive { get; set; }
    public int? DefaultRoleId { get; set; }
    public string? DefaultRoleName { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? DefaultRoleId { get; set; }
}

public class UpdateUserDto
{
    public string Username { get; set; } = null!;
    public string? Password { get; set; } // Optional for updates
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool? IsActive { get; set; } = true;
    public int? DefaultRoleId { get; set; }
}
    
