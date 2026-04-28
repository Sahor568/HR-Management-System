using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly ManagementContext _context;

        public RolesController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Roles
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Role>> GetRole(long id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return role;
        }

        // POST: api/Roles
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Role>> PostRole(Role role)
        {
            // Check if role with same name already exists
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == role.Name);
            if (existingRole != null)
            {
                return BadRequest(new { message = "Role with this name already exists" });
            }

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRole", new { id = role.Id }, role);
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> PutRole(long id, Role role)
        {
            if (id != role.Id)
            {
                return BadRequest();
            }

            // Check if role with same name already exists for another role
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == role.Name && r.Id != id);
            if (existingRole != null)
            {
                return BadRequest(new { message = "Role with this name already exists" });
            }

            _context.Entry(role).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteRole(long id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Check if any users are using this role
            var usersWithRole = await _context.Users
                .Where(u => u.Role == role.Name)
                .ToListAsync();
            if (usersWithRole.Any())
            {
                return BadRequest(new { 
                    message = $"Cannot delete role because it is used by {usersWithRole.Count} user(s)",
                    users = usersWithRole.Select(u => new { u.Id, u.Email })
                });
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Roles/system-roles
        [HttpGet("system-roles")]
        [Authorize(Policy = "AllRoles")]
        public ActionResult<IEnumerable<string>> GetSystemRoles()
        {
            // Return the predefined system roles from User model
            return Ok(Models.User.ValidRoles);
        }

        // GET: api/Roles/with-user-count
        [HttpGet("with-user-count")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetRolesWithUserCount()
        {
            var roles = await _context.Roles.ToListAsync();
            var result = new List<object>();

            foreach (var role in roles)
            {
                var userCount = await _context.Users
                    .Where(u => u.Role == role.Name)
                    .CountAsync();
                
                result.Add(new
                {
                    role.Id,
                    role.Name,
                    Description = "", // Role model doesn't have Description property
                    UserCount = userCount
                });
            }

            return Ok(result);
        }

        private bool RoleExists(long id)
        {
            return _context.Roles.Any(e => e.Id == id);
        }
    }
}