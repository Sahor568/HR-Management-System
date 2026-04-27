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
    public class HRController : ControllerBase
    {
        private readonly ManagementContext _context;

        public HRController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/HR
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<HR>>> GetHRs()
        {
            return await _context.HRs.ToListAsync();
        }

        // GET: api/HR/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<HR>> GetHR(long id)
        {
            var hR = await _context.HRs.FindAsync(id);

            if (hR == null)
            {
                return NotFound();
            }

            // HR users can only view their own record unless they are Admin
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole == "HR" && hR.Email != currentUserEmail)
                return Forbid();

            return hR;
        }

        // POST: api/HR
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<HR>> PostHR(HR hR)
        {
            if (string.IsNullOrEmpty(hR.Email))
                return BadRequest("Email is required");

            if (string.IsNullOrEmpty(hR.PasswordHash))
                return BadRequest("PasswordHash is required");

            // Check if email already exists
            var existing = await _context.HRs.AnyAsync(h => h.Email == hR.Email);
            if (existing)
                return BadRequest("HR with this email already exists");

            // Hash the password (in a real scenario, you'd use proper hashing)
            // For now, we'll assume PasswordHash is already hashed
            // In production, you should hash it here

            _context.HRs.Add(hR);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHR), new { id = hR.Id }, hR);
        }

        // PUT: api/HR/5
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> PutHR(long id, HR hR)
        {
            if (id != hR.Id)
            {
                return BadRequest();
            }

            var existingHR = await _context.HRs.FindAsync(id);
            if (existingHR == null)
                return NotFound();

            // Check permissions
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole == "HR" && existingHR.Email != currentUserEmail)
                return Forbid();

            // HR users can only update their own record, and only certain fields
            if (currentUserRole == "HR")
            {
                // HR can only update their own age and location, not email or password
                existingHR.Age = hR.Age;
                existingHR.Location = hR.Location;
                // Don't update email or password
            }
            else
            {
                // Admin can update all fields
                _context.Entry(existingHR).CurrentValues.SetValues(hR);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HRExists(id))
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

        // DELETE: api/HR/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteHR(long id)
        {
            var hR = await _context.HRs.FindAsync(id);
            if (hR == null)
            {
                return NotFound();
            }

            _context.HRs.Remove(hR);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/HR/by-email/{email}
        [HttpGet("by-email/{email}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<HR>> GetHRByEmail(string email)
        {
            var hR = await _context.HRs.FirstOrDefaultAsync(h => h.Email == email);

            if (hR == null)
            {
                return NotFound();
            }

            // Check permissions
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole == "HR" && hR.Email != currentUserEmail)
                return Forbid();

            return hR;
        }

        private bool HRExists(long id)
        {
            return _context.HRs.Any(e => e.Id == id);
        }
    }
}