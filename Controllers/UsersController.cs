using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Management.Controllers
{
    [Authorize(Policy = "AdminOrHR")]
    public class UsersController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public UsersController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [Authorize(Policy = "AdminOrHR")]
        [Route("Users")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: api/Users
        [HttpGet("api/Users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Role,
                    u.IsMainAdmin,
                    EmployeeId = _context.Employees
                        .Where(e => e.UserId == u.Id)
                        .Select(e => e.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/{id}
        [HttpGet("api/Users/{id}")]
        public async Task<ActionResult<object>> GetUser(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var employeeId = await _context.Employees
                .Where(e => e.UserId == id)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Role,
                user.IsMainAdmin,
                EmployeeId = employeeId
            });
        }

        // GET: api/Users/current
        [HttpGet("api/Users/current")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Unauthorized(new { message = "Not authenticated" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Role,
                user.IsMainAdmin,
                Employee = employee != null ? new
                {
                    employee.Id,
                    employee.FullName,
                    employee.Age,
                    employee.Phone,
                    employee.Address,
                    employee.Position,
                    employee.HireDate,
                    employee.Salary,
                    employee.DepartmentId,
                    DepartmentName = employee.Department != null ? employee.Department.Name : null,
                    employee.SupervisorId,
                    SupervisorName = employee.Supervisor != null ? employee.Supervisor.FullName : null
                } : null
            });
        }

        // POST: api/Users
        [HttpPost("api/Users")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate role
                if (!Models.User.ValidRoles.Contains(dto.Role))
                    return BadRequest(new { message = $"Invalid role. Valid roles are: {string.Join(", ", Models.User.ValidRoles)}" });

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "A user with this email already exists" });

                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = dto.Role,
                    IsMainAdmin = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Notify about account creation
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users.FindAsync(long.Parse(currentUserId ?? "0"));
                await _notificationService.NotifyAccountCreatedAsync(user.Id, user.Email, user.Role, currentUser?.Email ?? "System");

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.Role,
                    user.IsMainAdmin
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating user", error = ex.Message });
            }
        }

        // PUT: api/Users/{id}
        [HttpPut("api/Users/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Prevent changing the main admin's role
                if (user.IsMainAdmin && dto.Role != null && dto.Role != "Admin")
                    return BadRequest(new { message = "Cannot change the main admin's role" });

                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                    if (emailExists)
                        return BadRequest(new { message = "A user with this email already exists" });
                    user.Email = dto.Email;
                }

                if (!string.IsNullOrEmpty(dto.Role))
                {
                    if (!Models.User.ValidRoles.Contains(dto.Role))
                        return BadRequest(new { message = $"Invalid role. Valid roles are: {string.Join(", ", Models.User.ValidRoles)}" });
                    user.Role = dto.Role;
                }

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating user", error = ex.Message });
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("api/Users/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (user.IsMainAdmin)
                    return BadRequest(new { message = "Cannot delete the main admin account" });

                // Check if user has an employee record
                var hasEmployee = await _context.Employees.AnyAsync(e => e.UserId == id);
                if (hasEmployee)
                    return BadRequest(new { message = "Cannot delete user with an associated employee record. Remove the employee first." });

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting user", error = ex.Message });
            }
        }

        public class CreateUserDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = "Employee";
        }

        public class UpdateUserDto
        {
            public string? Email { get; set; }
            public string? Role { get; set; }
            public string? Password { get; set; }
        }
    }
}
