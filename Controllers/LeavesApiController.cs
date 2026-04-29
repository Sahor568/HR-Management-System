using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/Leaves")]
    [ApiController]
    public class LeavesApiController : ControllerBase
    {
    private readonly ManagementContext _context;

    public LeavesApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Leaves
    [HttpGet]
            public async Task<ActionResult<IEnumerable<Leave>>> GetLeaves()
        {
            return await _context.Leaves
                .Include(l => l.Employee)
                .ToListAsync();
        }

        // GET: api/Leaves/5
    [HttpGet("{id}")]
            public async Task<ActionResult<Leave>> GetLeave(long id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }

            // Check access: Employees can only view their own leaves
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || leave.EmployeeId != employee.Id)
                    return Forbid();
            }

            return leave;
        }

        // GET: api/Leaves/employee/{employeeId}
    [HttpGet("employee/{employeeId}")]
            public async Task<ActionResult<IEnumerable<Leave>>> GetLeavesByEmployee(long employeeId)
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Employees can only view their own leaves
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || employeeId != employee.Id)
                    return Forbid();
            }

            var leaves = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId)
                .Include(l => l.Employee)
                .ToListAsync();

            return leaves;
        }

        // GET: api/Leaves/pending
    [HttpGet("pending")]
            public async Task<ActionResult<IEnumerable<Leave>>> GetPendingLeaves()
        {
            var leaves = await _context.Leaves
                .Where(l => l.Status == "Pending")
                .Include(l => l.Employee)
                .ToListAsync();

            return leaves;
        }

        // POST: api/Leaves
    [HttpPost]
            public async Task<ActionResult<Leave>> PostLeave(Leave leave)
        {
            if (leave.EmployeeId <= 0)
                return BadRequest("Valid EmployeeId is required");

            if (leave.FromDate == default || leave.ToDate == default)
                return BadRequest("FromDate and ToDate are required");

            if (leave.FromDate > leave.ToDate)
                return BadRequest("FromDate cannot be after ToDate");

            if (string.IsNullOrEmpty(leave.Reason))
                return BadRequest("Reason is required");

            // Employees can only create leaves for themselves
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || leave.EmployeeId != employee.Id)
                    return Forbid();
            }

            // Set default status
            leave.Status = "Pending";

            // Check for overlapping leaves
            var overlappingLeaves = await _context.Leaves
                .Where(l => l.EmployeeId == leave.EmployeeId &&
                           l.Status != "Rejected" &&
                           ((leave.FromDate >= l.FromDate && leave.FromDate <= l.ToDate) ||
                            (leave.ToDate >= l.FromDate && leave.ToDate <= l.ToDate) ||
                            (leave.FromDate <= l.FromDate && leave.ToDate >= l.ToDate)))
                .ToListAsync();

            if (overlappingLeaves.Any())
                return BadRequest("Leave request overlaps with an existing approved or pending leave");

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeave), new { id = leave.Id }, leave);
        }

        // PUT: api/Leaves/5
    [HttpPut("{id}")]
            public async Task<IActionResult> PutLeave(long id, Leave leave)
        {
            if (id != leave.Id)
            {
                return BadRequest();
            }

            var existingLeave = await _context.Leaves.FindAsync(id);
            if (existingLeave == null)
                return NotFound();

            // Check access
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || existingLeave.EmployeeId != employee.Id)
                    return Forbid();

                // Employees can only update their own leaves if status is Pending
                if (existingLeave.Status != "Pending")
                    return BadRequest("Cannot update a leave that is already approved or rejected");

                // Employees can only update certain fields
                existingLeave.FromDate = leave.FromDate;
                existingLeave.ToDate = leave.ToDate;
                existingLeave.Reason = leave.Reason;
            }
            else
            {
                // Admin/HR can update all fields including status
                _context.Entry(existingLeave).CurrentValues.SetValues(leave);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeaveExists(id))
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

        // PUT: api/Leaves/5/approve
    [HttpPut("{id}/approve")]
            public async Task<IActionResult> ApproveLeave(long id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound();

            leave.Status = "Approved";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Leaves/5/reject
    [HttpPut("{id}/reject")]
            public async Task<IActionResult> RejectLeave(long id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound();

            leave.Status = "Rejected";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Leaves/5
    [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteLeave(long id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
            {
                return NotFound();
            }

            // Check access
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || leave.EmployeeId != employee.Id)
                    return Forbid();

                // Employees can only delete their own leaves if status is Pending
                if (leave.Status != "Pending")
                    return BadRequest("Cannot delete a leave that is already approved or rejected");
            }

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    private bool LeaveExists(long id)
        {
            return _context.Leaves.Any(e => e.Id == id);
        }
    }
}
