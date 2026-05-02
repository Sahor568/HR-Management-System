using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ManagementContext _context;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public HomeController(ManagementContext context, IConfiguration configuration, INotificationService notificationService)
        {
            _context = context;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Hello()
        {
            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Login()
        {
            // If already authenticated, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Email and password are required";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (user.IsMainAdmin)
            {
                claims.Add(new Claim("IsMainAdmin", "true"));
            }

            // Check if user has an associated employee record
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee != null)
            {
                claims.Add(new Claim("EmployeeId", employee.Id.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Dashboard");
        }

        public IActionResult Register()
        {
            return View();
        }

        [Authorize]
        public IActionResult Settings()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        // ========== API ENDPOINTS ==========

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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            
            // Add IsMainAdmin claim if user is main admin
            if (user.IsMainAdmin)
            {
                claims.Add(new Claim("IsMainAdmin", "true"));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Register - Admin and HR can create accounts (with restrictions)
        [HttpPost("api/Login/register")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<User>> ApiRegister([FromBody] ApiRegisterRequest registerRequest)
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

            // Check if current user is main admin when creating Admin/HR role
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUser = await _context.Users.FindAsync(currentUserId);
            var isCurrentUserMainAdmin = User.HasClaim("IsMainAdmin", "true");
            var isCurrentUserHR = User.IsInRole("HR");
            
            // HR can only create Employee accounts
            if (isCurrentUserHR && registerRequest.Role != "Employee")
            {
                return StatusCode(403, new { message = "HR can only create Employee accounts" });
            }
            
            // Only main admin can create other Admin accounts
            if (registerRequest.Role == "Admin" && !isCurrentUserMainAdmin)
            {
                return StatusCode(403, new { message = "Only main admin can create other Admin accounts" });
            }

            // Only main admin can create HR accounts
            if (registerRequest.Role == "HR" && !isCurrentUserMainAdmin)
            {
                return StatusCode(403, new { message = "Only main admin can create HR accounts" });
            }

            // check duplicate email
            var exists = await _context.Users.AnyAsync(u => u.Email == registerRequest.Email);
            if (exists)
                return BadRequest("Email already exists");

            // Create new user
            var user = new User
            {
                Email = registerRequest.Email,
                Role = registerRequest.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password),
                IsMainAdmin = false // Only seed data can create main admin
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Notify admin about new account creation
            var creatorEmail = currentUser?.Email ?? "Unknown";
            await _notificationService.NotifyAccountCreatedAsync(user.Id, user.Email, user.Role, creatorEmail);

            // Return user without password hash - create a new object to avoid modifying the entity
            var responseUser = new User
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsMainAdmin = user.IsMainAdmin,
                PasswordHash = "" // Empty for response
            };
            return Ok(responseUser);
        }

        // Login - accessible to all
        [HttpPost("api/Login/login")]
        public async Task<ActionResult> ApiLogin([FromBody] ApiLoginRequest loginRequest)
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

            var employeeId = await _context.Employees
                .Where(e => e.UserId == user.Id)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Login successful",
                token,
                user.Id,
                user.Email,
                user.Role,
                employeeId = employeeId > 0 ? employeeId : (long?)null
            });
        }

        // Get current user profile - any authenticated user
        [HttpGet("api/Login/profile")]
        [Authorize]
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
        [HttpPost("api/Login/change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ApiChangePasswordRequest request)
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Supporting models for API endpoints
    public class ApiLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiRegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }

    public class ApiChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ApiUpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
