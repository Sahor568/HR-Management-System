using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/Login")]
    [ApiController]
    public class LoginApiController : ControllerBase
    {
    private readonly ManagementContext _context;
    private readonly IConfiguration _configuration;

    public LoginApiController(ManagementContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Helper method to generate JWT token
    private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong";
            var issuer = jwtSettings["Issuer"] ?? "HRManagementSystem";
            var audience = jwtSettings["Audience"] ?? "HRManagementClients";
            var expiryInMinutes = Convert.ToInt32(jwtSettings["ExpiryInMinutes"] ?? "60");

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Register - accessible to all (or maybe only Admin? We'll keep open for now)
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest == null)
                return BadRequest("Invalid data");

            // Validate required fields
            if (string.IsNullOrEmpty(registerRequest.Email))
                return BadRequest("Email is required");
            if (string.IsNullOrEmpty(registerRequest.Password))
                return BadRequest("Password is required");
            if (string.IsNullOrEmpty(registerRequest.Role))
                return BadRequest("Role is required");

            // Validate role
            if (!Models.User.ValidRoles.Contains(registerRequest.Role))
                return BadRequest($"Role must be one of: {string.Join(", ", Models.User.ValidRoles)}");

            // check duplicate email
            var exists = await _context.Users.AnyAsync(u => u.Email == registerRequest.Email);
            if (exists)
                return BadRequest("Email already exists");

            // Create new user
            var user = new User
            {
                Email = registerRequest.Email,
                Role = registerRequest.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return user without password hash - create a new object to avoid modifying the entity
            var responseUser = new User
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                PasswordHash = "" // Empty for response
            };
            return Ok(responseUser);
        }

        // Login - accessible to all
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                return BadRequest("Email and password are required");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null)
                return Unauthorized("Invalid email or password");

            // Check if password hash is null or empty
            if (string.IsNullOrEmpty(user.PasswordHash))
                return Unauthorized("Invalid email or password");

            bool isValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isValid)
                return Unauthorized("Invalid email or password");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                user.Id,
                user.Email,
                user.Role
            });
        }

        // Get all users - only Admin can view
    [HttpGet]
            public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            // Remove password hash from response by creating new objects
            var responseUsers = users.Select(u => new User
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                PasswordHash = "" // Empty for response
            }).ToList();
            return Ok(responseUsers);
        }

        // Update user - only Admin can update
    [HttpPut("{id}")]
            public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest updateRequest)
        {
            if (updateRequest == null)
                return BadRequest("Invalid data");

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            // Validate role if provided
            if (!string.IsNullOrEmpty(updateRequest.Role) && !Models.User.ValidRoles.Contains(updateRequest.Role))
                return BadRequest($"Role must be one of: {string.Join(", ", Models.User.ValidRoles)}");

            user.Email = updateRequest.Email ?? user.Email;
            user.Role = updateRequest.Role ?? user.Role;

            // If password is being updated, hash it
            if (!string.IsNullOrEmpty(updateRequest.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateRequest.Password);

            await _context.SaveChangesAsync();

            // Return user without password hash - create a new object to avoid modifying the entity
            var responseUser = new User
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                PasswordHash = "" // Empty for response
            };
            return Ok(responseUser);
        }

        // Delete user - only Admin can delete
    [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("User deleted successfully");
        }

        // Get current user profile - any authenticated user
    [HttpGet("profile")]
            public async Task<ActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Role
            });
        }

        // Change password - any authenticated user
    [HttpPost("change-password")]
            public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("Old and new passwords are required");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            bool isValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            if (!isValid)
                return Unauthorized("Old password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Password changed successfully");
        }
    }

    // Supporting models
    public class LoginRequest
    {
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    }

    public class ChangePasswordRequest
    {
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    }
}
