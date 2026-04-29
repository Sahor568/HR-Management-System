using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Management.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
    private readonly ManagementContext _context;
    private readonly ILogger<UsersApiController> _logger;

    public UsersApiController(ManagementContext context, ILogger<UsersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            _logger.LogInformation("Getting all users. Requested by admin.");
            var users = await _context.Users.ToListAsync();
            _logger.LogInformation("Retrieved {UserCount} users from database.", users.Count);
            return users;
        }

        // GET: api/Users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id)
        {
            _logger.LogDebug("Getting user with ID {UserId}", id);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return NotFound();
            }

            // Hide password hash from response
            user.PasswordHash = "";
            _logger.LogInformation("User with ID {UserId} retrieved successfully", id);
            return user;
        }

        // PUT: api/Users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(long id, UpdateUserRequest request)
        {
            if (request == null)
                return BadRequest("Invalid data");
                
            _logger.LogInformation("Updating user with ID {UserId}. Request data: {@UserData}", id, new { request.Email, request.Role });
            
            // Check if email already exists for another user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists for another user", request.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            // Get existing user to preserve password hash if not changing password
            var existing = await _context.Users.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for update", id);
                return NotFound();
            }

            // Validate role if provided
            if (!string.IsNullOrEmpty(request.Role) && !Models.User.ValidRoles.Contains(request.Role))
                return BadRequest($"Role must be one of: {string.Join(", ", Models.User.ValidRoles)}");

            // Only update fields that should be changed
            existing.Email = request.Email ?? existing.Email;
            existing.Role = request.Role ?? existing.Role;

            // Only update password if a new one is provided (not empty)
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogDebug("Updating password for user ID {UserId}", id);
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User with ID {UserId} updated successfully", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!UserExists(id))
                {
                    _logger.LogWarning("User with ID {UserId} no longer exists during concurrency update", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating user with ID {UserId}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                throw;
            }

            return NoContent();
        }

        // POST: api/Users
    [HttpPost]
    public async Task<ActionResult<User>> PostUser(CreateUserRequest request)
        {
            if (request == null)
                return BadRequest("Invalid data");
                
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required");
            if (string.IsNullOrEmpty(request.Password))
                return BadRequest("Password is required");
            if (string.IsNullOrEmpty(request.Role))
                return BadRequest("Role is required");
                
            // Validate role
            if (!Models.User.ValidRoles.Contains(request.Role))
                return BadRequest($"Role must be one of: {string.Join(", ", Models.User.ValidRoles)}");
            
            _logger.LogInformation("Creating new user with email {Email} and role {Role}", request.Email, request.Role);
            
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists", request.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            // Create user with hashed password
            var user = new User
            {
                Email = request.Email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
            
            // Hide password hash from response
            user.PasswordHash = "";
            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/Users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(long id)
        {
            _logger.LogInformation("Deleting user with ID {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User with ID {UserId} deleted successfully", id);
            return NoContent();
        }

    private bool UserExists(long id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }
}
