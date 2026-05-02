using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class RolesController : Controller
    {
        private readonly ManagementContext _context;

        public RolesController(ManagementContext context)
        {
            _context = context;
        }

        // MVC Action - Returns HTML view for /Roles
        public IActionResult Index()
        {
            return View();
        }

        // API Endpoints - All under /api/Roles route

        // GET: api/Roles/system-roles
        [HttpGet("api/Roles/system-roles")]
        public ActionResult<object> GetSystemRoles()
        {
            var systemRoles = new[]
            {
                new { name = "Admin", description = GetDefaultDescription("Admin"), icon = GetRoleIcon("Admin"), gradient = GetRoleGradient("Admin"), permissions = GetDefaultPermissions("Admin") },
                new { name = "HR", description = GetDefaultDescription("HR"), icon = GetRoleIcon("HR"), gradient = GetRoleGradient("HR"), permissions = GetDefaultPermissions("HR") },
                new { name = "Employee", description = GetDefaultDescription("Employee"), icon = GetRoleIcon("Employee"), gradient = GetRoleGradient("Employee"), permissions = GetDefaultPermissions("Employee") }
            };

            return Ok(systemRoles);
        }

        // GET: api/Roles
        [HttpGet("api/Roles")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Roles/with-user-count
        [HttpGet("api/Roles/with-user-count")]
        public async Task<ActionResult<IEnumerable<object>>> GetRolesWithUserCount()
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    UserCount = _context.Users.Count(u => u.Role == r.Name)
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Roles/metadata
        [HttpGet("api/Roles/metadata")]
        public async Task<ActionResult<object>> GetRolesMetadata()
        {
            // Get all roles from database
            var roles = await _context.Roles.ToListAsync();
            
            // Build metadata for each role
            var metadata = new Dictionary<string, object>();
            
            foreach (var role in roles)
            {
                var roleMeta = new
                {
                    icon = GetRoleIcon(role.Name),
                    gradient = GetRoleGradient(role.Name),
                    description = GetDefaultDescription(role.Name),
                    permissions = GetDefaultPermissions(role.Name)
                };
                
                metadata[role.Name] = roleMeta;
            }

            return Ok(metadata);
        }

        // POST: api/Roles
        [HttpPost("api/Roles")]
        public async Task<ActionResult<object>> PostRole([FromBody] CreateRoleDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Role name is required" });

            // Check if role already exists
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.Name);
            if (existingRole != null)
                return BadRequest(new { message = "Role with this name already exists" });

            var role = new Role { Name = dto.Name };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new { role.Id, role.Name });
        }

        // PUT: api/Roles/5
        [HttpPut("api/Roles/{id}")]
        public async Task<IActionResult> PutRole(long id, [FromBody] UpdateRoleDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });

            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null)
                return NotFound(new { message = "Role not found" });

            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Role name is required" });

            // Check if name is being changed and if new name already exists
            if (existingRole.Name != dto.Name)
            {
                var duplicate = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.Name && r.Id != id);
                if (duplicate != null)
                    return BadRequest(new { message = "Role with this name already exists" });
            }

            existingRole.Name = dto.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(id))
                    return NotFound(new { message = "Role not found" });
                throw;
            }

            return Ok(new { existingRole.Id, existingRole.Name });
        }

        // DELETE: api/Roles/5
        [HttpDelete("api/Roles/{id}")]
        public async Task<IActionResult> DeleteRole(long id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            // Check if any users have this role
            var usersWithRole = await _context.Users.CountAsync(u => u.Role == role.Name);
            if (usersWithRole > 0)
                return BadRequest(new { message = $"Cannot delete role. {usersWithRole} user(s) are assigned this role." });

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Role deleted successfully" });
        }

        private bool RoleExists(long id)
        {
            return _context.Roles.Any(e => e.Id == id);
        }

        public class CreateRoleDto
        {
            public string Name { get; set; } = string.Empty;
        }

        public class UpdateRoleDto
        {
            public string Name { get; set; } = string.Empty;
        }

        // Helper methods for role metadata
        private string GetRoleIcon(string roleName)
        {
            return roleName switch
            {
                "Admin" => "fa-crown",
                "HR" => "fa-briefcase",
                "Employee" => "fa-user",
                _ => "fa-user-tag"
            };
        }

        private string GetRoleGradient(string roleName)
        {
            return roleName switch
            {
                "Admin" => "linear-gradient(135deg, #ef4444 0%, #dc2626 100%)",
                "HR" => "linear-gradient(135deg, #10b981 0%, #059e0a 100%)",
                "Employee" => "linear-gradient(135deg, #2563eb 0%, #0ea5e9 100%)",
                _ => "linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)"
            };
        }

        private string GetDefaultDescription(string roleName)
        {
            return roleName switch
            {
                "Admin" => "Full system access and control.",
                "HR" => "HR operations and employee management.",
                "Employee" => "Basic employee access and self-service.",
                _ => "Custom role with configurable permissions."
            };
        }

        private string[] GetDefaultPermissions(string roleName)
        {
            return roleName switch
            {
                "Admin" => new[] { "Manage Users", "Manage Departments", "Manage Employees", "View Reports", "Process Payroll", "System Settings", "View Logs" },
                "HR" => new[] { "Manage Employees", "Manage Departments", "Process Leaves", "Track Attendance", "View Payroll", "Generate Reports" },
                "Employee" => new[] { "View Own Profile", "Request Leave", "View Attendance", "View Payslip", "Update Profile", "View Dashboard" },
                _ => new[] { "View Dashboard" }
            };
        }
    }
}
