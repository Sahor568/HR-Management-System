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
    [Route("api/[controller]")]
    [ApiController]
    public class AttendancesController : ControllerBase
    {
        private readonly ManagementContext _context;

        public AttendancesController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Attendances
        [HttpGet]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendances()
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .ToListAsync();
        }

        // GET: api/Attendances/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<Attendance>> GetAttendance(long id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            // Check access: Employees can only view their own attendance
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || attendance.EmployeeId != employee.Id)
                    return Forbid();
            }

            return attendance;
        }

        // GET: api/Attendances/employee/{employeeId}
        [HttpGet("employee/{employeeId}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendancesByEmployee(long employeeId)
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Employees can only view their own attendance
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || employeeId != employee.Id)
                    return Forbid();
            }

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .Include(a => a.Employee)
                .ToListAsync();

            return attendances;
        }

        // GET: api/Attendances/today
        [HttpGet("today")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetTodayAttendances()
        {
            var today = DateTime.Today;
            var attendances = await _context.Attendances
                .Where(a => a.Date.Date == today)
                .Include(a => a.Employee)
                .ToListAsync();

            return attendances;
        }

        // POST: api/Attendances
        [HttpPost]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<Attendance>> PostAttendance(Attendance attendance)
        {
            if (attendance.EmployeeId <= 0)
                return BadRequest("Valid EmployeeId is required");

            if (attendance.Date == default)
                attendance.Date = DateTime.Today;

            // Validate status
            var validStatuses = new[] { "Present", "Absent", "Late" };
            if (!validStatuses.Contains(attendance.Status))
                return BadRequest("Status must be Present, Absent, or Late");

            // Check if attendance already exists for this employee on this date
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == attendance.EmployeeId && a.Date.Date == attendance.Date.Date);
            if (existing != null)
                return BadRequest("Attendance already recorded for this employee on this date");

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // POST: api/Attendances/check-in
        [HttpPost("check-in")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<Attendance>> CheckIn()
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            if (employee == null)
                return BadRequest("Employee record not found for current user");

            var today = DateTime.Today;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date.Date == today);

            if (existingAttendance != null)
            {
                if (existingAttendance.CheckIn != default)
                    return BadRequest("Already checked in today");
                
                existingAttendance.CheckIn = DateTime.Now;
                existingAttendance.Status = "Present";
                await _context.SaveChangesAsync();
                return Ok(existingAttendance);
            }
            else
            {
                var attendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    Date = today,
                    CheckIn = DateTime.Now,
                    Status = "Present"
                };
                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
            }
        }

        // POST: api/Attendances/check-out
        [HttpPost("check-out")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<Attendance>> CheckOut()
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            if (employee == null)
                return BadRequest("Employee record not found for current user");

            var today = DateTime.Today;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date.Date == today);

            if (attendance == null)
                return BadRequest("No check-in record found for today");

            if (attendance.CheckOut != default)
                return BadRequest("Already checked out today");

            attendance.CheckOut = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        // PUT: api/Attendances/5
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> PutAttendance(long id, Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return BadRequest();
            }

            // Validate status
            var validStatuses = new[] { "Present", "Absent", "Late" };
            if (!validStatuses.Contains(attendance.Status))
                return BadRequest("Status must be Present, Absent, or Late");

            _context.Entry(attendance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
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

        // DELETE: api/Attendances/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteAttendance(long id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AttendanceExists(long id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}