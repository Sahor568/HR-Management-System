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
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ManagementContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ManagementContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            _logger.LogInformation("Getting all users. Requested by admin.");
            var users = await _context.Users.ToListAsync();
            _logger.LogInformation("Retrieved {UserCount} users from database.", users.Count);
            return users;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AllRoles")]
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> PutUser(long id, User user)
        {
            _logger.LogInformation("Updating user with ID {UserId}. Request data: {@UserData}", id, new { user.Email, user.Role });
            
            if (id != user.Id)
            {
                _logger.LogWarning("User ID mismatch. Path ID: {PathId}, Body ID: {BodyId}", id, user.Id);
                return BadRequest();
            }

            // Check if email already exists for another user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email && u.Id != id);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists for another user", user.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            // Get existing user to preserve password hash if not changing password
            var existing = await _context.Users.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for update", id);
                return NotFound();
            }

            // Only update fields that should be changed
            existing.Email = user.Email;
            existing.Role = user.Role;

            // Only update password if a new one is provided (not empty)
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _logger.LogDebug("Updating password for user ID {UserId}", id);
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _logger.LogInformation("Creating new user with email {Email} and role {Role}", user.Email, user.Role);
            
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists", user.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
            
            // Hide password hash from response
            user.PasswordHash = "";
            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
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
}