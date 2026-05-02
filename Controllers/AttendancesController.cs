using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Authorize(Policy = "AllRoles")]
    public class AttendancesController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public AttendancesController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // MVC Action - Returns HTML view for /Attendances
        [Route("Attendances")]
        public IActionResult Index()
        {
            return View();
        }

        // API Endpoints - All under /api/Attendances route

        // GET: api/Attendances
        [HttpGet("api/Attendances")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetAttendances()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
                .Select(a => new
                {
                    a.Id,
                    a.EmployeeId,
                    Employee = a.Employee != null ? new { FullName = a.Employee.FullName, Department = a.Employee.Department != null ? new { Name = a.Employee.Department.Name } : null } : null,
                    a.Date,
                    a.CheckIn,
                    a.CheckOut,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendances);
        }

        // GET: api/Attendances/5
        [HttpGet("api/Attendances/{id}")]
        public async Task<ActionResult<object>> GetAttendance(long id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound(new { message = "Attendance not found" });
            }

            // Check access: Employees can only view their own attendance
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null || attendance.EmployeeId != currentEmployee.Id)
                    return StatusCode(403, new { message = "You can only view your own attendance records" });
            }

            return Ok(new
            {
                attendance.Id,
                attendance.EmployeeId,
                Employee = attendance.Employee != null ? new { FullName = attendance.Employee.FullName, Department = attendance.Employee.Department != null ? new { Name = attendance.Employee.Department.Name } : null } : null,
                attendance.Date,
                attendance.CheckIn,
                attendance.CheckOut,
                attendance.Status
            });
        }

        // POST: api/Attendances
        [HttpPost("api/Attendances")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> PostAttendance([FromBody] CreateAttendanceDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });
            if (dto.EmployeeId <= 0)
                return BadRequest(new { message = "Valid EmployeeId is required" });

            // Verify employee exists
            var employeeExists = await _context.Employees.AnyAsync(e => e.Id == dto.EmployeeId);
            if (!employeeExists)
                return BadRequest(new { message = "Employee does not exist" });

            var attendance = new Attendance
            {
                EmployeeId = dto.EmployeeId,
                Date = dto.Date != default ? dto.Date : DateTime.UtcNow.Date,
                CheckIn = dto.CheckIn != default ? dto.CheckIn : DateTime.UtcNow,
                CheckOut = dto.CheckOut,
                Status = dto.Status ?? "Present"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                attendance.Id,
                attendance.EmployeeId,
                attendance.Date,
                attendance.CheckIn,
                attendance.CheckOut,
                attendance.Status
            });
        }

        // PUT: api/Attendances/5
        [HttpPut("api/Attendances/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> PutAttendance(long id, [FromBody] UpdateAttendanceDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound(new { message = "Attendance not found" });

            if (dto.EmployeeId.HasValue)
            {
                if (dto.EmployeeId.Value <= 0)
                    return BadRequest(new { message = "Valid EmployeeId is required" });

                var employeeExists = await _context.Employees.AnyAsync(e => e.Id == dto.EmployeeId.Value);
                if (!employeeExists)
                    return BadRequest(new { message = "Employee does not exist" });

                attendance.EmployeeId = dto.EmployeeId.Value;
            }

            if (dto.Date.HasValue) attendance.Date = dto.Date.Value;
            if (dto.CheckIn.HasValue) attendance.CheckIn = dto.CheckIn.Value;
            if (dto.CheckOut.HasValue) attendance.CheckOut = dto.CheckOut.Value;
            if (!string.IsNullOrEmpty(dto.Status)) attendance.Status = dto.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
                {
                    return NotFound(new { message = "Attendance not found" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Attendance updated successfully" });
        }

        // DELETE: api/Attendances/5
        [HttpDelete("api/Attendances/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteAttendance(long id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound(new { message = "Attendance not found" });
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Attendance deleted successfully" });
        }

        // GET: api/Attendances/employee/{employeeId}
        [HttpGet("api/Attendances/employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAttendancesByEmployee(long employeeId)
        {
            // Check access: Employees can only view their own attendance
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null || employeeId != currentEmployee.Id)
                    return StatusCode(403, new { message = "You can only view your own attendance records" });
            }

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
                .Select(a => new
                {
                    a.Id,
                    a.EmployeeId,
                    Employee = a.Employee != null ? new { FullName = a.Employee.FullName, Department = a.Employee.Department != null ? new { Name = a.Employee.Department.Name } : null } : null,
                    a.Date,
                    a.CheckIn,
                    a.CheckOut,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendances);
        }

        // GET: api/Attendances/today
        [HttpGet("api/Attendances/today")]
        public async Task<ActionResult<IEnumerable<object>>> GetTodayAttendances()
        {
            var today = DateTime.UtcNow.Date;
            
            // Check access: Employees can only view their own attendance
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null)
                    return StatusCode(403, new { message = "You don't have an employee record" });

                var attendance = await _context.Attendances
                    .Where(a => a.EmployeeId == currentEmployee.Id && a.Date == today)
                    .Include(a => a.Employee)
                    .ThenInclude(e => e!.Department)
                    .Select(a => new
                    {
                        a.Id,
                        a.EmployeeId,
                        Employee = a.Employee != null ? new { FullName = a.Employee.FullName, Department = a.Employee.Department != null ? new { Name = a.Employee.Department.Name } : null } : null,
                        a.Date,
                        a.CheckIn,
                        a.CheckOut,
                        a.Status
                    })
                    .ToListAsync();

                return Ok(attendance);
            }

            // Admin/HR can view all today's attendances
            var attendances = await _context.Attendances
                .Where(a => a.Date == today)
                .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
                .Select(a => new
                {
                    a.Id,
                    a.EmployeeId,
                    Employee = a.Employee != null ? new { FullName = a.Employee.FullName, Department = a.Employee.Department != null ? new { Name = a.Employee.Department.Name } : null } : null,
                    a.Date,
                    a.CheckIn,
                    a.CheckOut,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendances);
        }

        // POST: api/Attendances/check-in
        [HttpPost("api/Attendances/check-in")]
        public async Task<ActionResult<Attendance>> CheckIn()
        {
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            
            if (currentEmployee == null)
                return BadRequest(new { message = "No employee record found for current user" });

            var today = DateTime.UtcNow.Date;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == currentEmployee.Id && a.Date == today);

            if (existingAttendance != null && existingAttendance.CheckIn != default)
                return BadRequest(new { message = "Already checked in today" });

            var attendance = new Attendance
            {
                EmployeeId = currentEmployee.Id,
                Date = today,
                CheckIn = DateTime.UtcNow,
                Status = "Present"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                attendance.Id,
                attendance.EmployeeId,
                attendance.Date,
                attendance.CheckIn,
                attendance.CheckOut,
                attendance.Status,
                message = "Checked in successfully"
            });
        }

        // POST: api/Attendances/check-out/{attendanceId}
        [HttpPost("api/Attendances/check-out/{attendanceId}")]
        public async Task<ActionResult<Attendance>> CheckOut(long attendanceId)
        {
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            
            if (currentEmployee == null)
                return BadRequest(new { message = "No employee record found for current user" });

            var attendance = await _context.Attendances.FindAsync(attendanceId);
            if (attendance == null)
                return NotFound(new { message = "Attendance not found" });

            if (attendance.EmployeeId != currentEmployee.Id)
                return StatusCode(403, new { message = "You can only check out your own attendance" });

            if (attendance.CheckOut != default)
                return BadRequest(new { message = "Already checked out" });

            attendance.CheckOut = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                attendance.Id,
                attendance.EmployeeId,
                attendance.Date,
                attendance.CheckIn,
                attendance.CheckOut,
                attendance.Status
            });
        }

        // GET: api/Attendances/summary/{employeeId}/{month}/{year}
        [HttpGet("api/Attendances/summary/{employeeId}/{month}/{year}")]
        public async Task<ActionResult<object>> GetAttendanceSummary(long employeeId, int month, int year)
        {
            // Check access: Employees can only view their own summary
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null || employeeId != currentEmployee.Id)
                    return StatusCode(403, new { message = "You can only view your own attendance summary" });
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            var presentDays = attendances.Count(a => a.Status == "Present");
            var absentDays = attendances.Count(a => a.Status == "Absent");
            var lateDays = attendances.Count(a => a.Status == "Late");
            var leaveDays = attendances.Count(a => a.Status == "Leave");

            return new
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                LeaveDays = leaveDays,
                TotalDays = attendances.Count
            };
        }

        private bool AttendanceExists(long id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }

        public class CreateAttendanceDto
        {
            public long EmployeeId { get; set; }
            public DateTime Date { get; set; }
            public DateTime CheckIn { get; set; }
            public DateTime CheckOut { get; set; }
            public string? Status { get; set; }
        }

        public class UpdateAttendanceDto
        {
            public long? EmployeeId { get; set; }
            public DateTime? Date { get; set; }
            public DateTime? CheckIn { get; set; }
            public DateTime? CheckOut { get; set; }
            public string? Status { get; set; }
        }
    }
}
