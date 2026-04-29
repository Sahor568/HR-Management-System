using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/HR")]
    [ApiController]
    public class HRApiController : ControllerBase
    {
        private readonly ManagementContext _context;

        public HRApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/HR
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HR>>> GetHRs()
        {
            return await _context.HRs.ToListAsync();
        }

        // GET: api/HR/5
        [HttpGet("{id}")]
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
        public async Task<ActionResult<HR>> PostHR(HR hR)
        {
            _context.HRs.Add(hR);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetHR", new { id = hR.Id }, hR);
        }

        // PUT: api/HR/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHR(long id, HR hR)
        {
            if (id != hR.Id)
            {
                return BadRequest();
            }

            _context.Entry(hR).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HREExists(id))
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

        private bool HREExists(long id)
        {
            return _context.HRs.Any(e => e.Id == id);
        }
    }
}