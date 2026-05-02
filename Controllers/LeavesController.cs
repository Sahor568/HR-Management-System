using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Management.Controllers
{
    [Authorize(Policy = "AllRoles")]
    public class LeavesController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public LeavesController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: api/Leaves
        [HttpGet("api/Leaves")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetLeaves([FromQuery] string? status = null)
        {
            var query = _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var leaves = await query
                .OrderByDescending(l => l.FromDate)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    Employee = l.Employee != null ? new { FullName = l.Employee.FullName, Department = l.Employee.Department != null ? new { Name = l.Employee.Department.Name } : null } : null,
                    l.FromDate,
                    l.ToDate,
                    l.Reason,
                    l.Status,
                    l.ApprovedBy,
                    l.ApprovedAt,
                    l.ApprovalRemarks
                })
                .ToListAsync();

            return Ok(leaves);
        }

        // GET: api/Leaves/employee/{employeeId}
        [HttpGet("api/Leaves/employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetLeavesByEmployee(long employeeId)
        {
            var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || employee.Id != employeeId)
                    return StatusCode(403, new { message = "You can only view your own leave requests" });
            }
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.FromDate)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    Employee = l.Employee != null ? new { FullName = l.Employee.FullName, Department = l.Employee.Department != null ? new { Name = l.Employee.Department.Name } : null } : null,
                    l.FromDate,
                    l.ToDate,
                    l.Reason,
                    l.Status,
                    l.ApprovedBy,
                    l.ApprovedAt,
                    l.ApprovalRemarks
                })
                .ToListAsync();

            return Ok(leaves);
        }

        // GET: api/Leaves/my-leaves
        [HttpGet("api/Leaves/my-leaves")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyLeaves()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                return Ok(new List<object>());
            }

            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.FromDate)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    Employee = l.Employee != null ? new { FullName = l.Employee.FullName, Department = l.Employee.Department != null ? new { Name = l.Employee.Department.Name } : null } : null,
                    l.FromDate,
                    l.ToDate,
                    l.Reason,
                    l.Status,
                    l.ApprovedBy,
                    l.ApprovedAt,
                    l.ApprovalRemarks
                })
                .ToListAsync();

            return Ok(leaves);
        }

        // POST: api/Leaves
        [HttpPost("api/Leaves")]
        public async Task<ActionResult<object>> CreateLeave([FromBody] CreateLeaveDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

                long employeeId = dto.EmployeeId;
                if (employeeId == 0 && employee != null)
                {
                    employeeId = employee.Id;
                }

                if (employeeId == 0)
                    return BadRequest(new { message = "Employee ID is required" });

                var employeeExists = await _context.Employees.AnyAsync(e => e.Id == employeeId);
                if (!employeeExists)
                    return BadRequest(new { message = "Employee not found" });

                var leave = new Leave
                {
                    EmployeeId = employeeId,
                    FromDate = dto.FromDate,
                    ToDate = dto.ToDate,
                    Reason = dto.Reason,
                    Status = "Pending"
                };

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();

                // Notify admins/HR about the leave request
                var emp = await _context.Employees.FindAsync(employeeId);
                if (emp != null)
                {
                    await _notificationService.NotifyLeaveRequestAsync(leave.Id, employeeId, emp.FullName, dto.FromDate, dto.ToDate);
                }

                return Ok(new
                {
                    leave.Id,
                    leave.EmployeeId,
                    leave.FromDate,
                    leave.ToDate,
                    leave.Reason,
                    leave.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating leave request", error = ex.Message });
            }
        }

        // PUT: api/Leaves/{id}/approve
        [HttpPut("api/Leaves/{id}/approve")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> ApproveLeave(long id, [FromBody] ApprovalDto? dto = null)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                    return NotFound(new { message = "Leave request not found" });

                if (leave.Status != "Pending")
                    return BadRequest(new { message = "Only pending leaves can be approved" });

                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                leave.Status = "Approved";
                leave.ApprovedBy = userId;
                leave.ApprovedAt = DateTime.UtcNow;
                leave.ApprovalRemarks = dto?.Remarks;

                await _context.SaveChangesAsync();

                // Notify the employee
                var employee = await _context.Employees.FindAsync(leave.EmployeeId);
                if (employee != null)
                {
                    await _notificationService.NotifyLeaveApprovalAsync(leave.Id, leave.EmployeeId, employee.FullName, true, dto?.Remarks);
                }

                return Ok(new { message = "Leave request approved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving leave", error = ex.Message });
            }
        }

        // PUT: api/Leaves/{id}/reject
        [HttpPut("api/Leaves/{id}/reject")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> RejectLeave(long id, [FromBody] ApprovalDto? dto = null)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                    return NotFound(new { message = "Leave request not found" });

                if (leave.Status != "Pending")
                    return BadRequest(new { message = "Only pending leaves can be rejected" });

                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                leave.Status = "Rejected";
                leave.ApprovedBy = userId;
                leave.ApprovedAt = DateTime.UtcNow;
                leave.ApprovalRemarks = dto?.Remarks;

                await _context.SaveChangesAsync();

                // Notify the employee
                var employee = await _context.Employees.FindAsync(leave.EmployeeId);
                if (employee != null)
                {
                    await _notificationService.NotifyLeaveApprovalAsync(leave.Id, leave.EmployeeId, employee.FullName, false, dto?.Remarks);
                }

                return Ok(new { message = "Leave request rejected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while rejecting leave", error = ex.Message });
            }
        }

        public class CreateLeaveDto
        {
            public long EmployeeId { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        public class ApprovalDto
        {
            public string? Remarks { get; set; }
        }
    }
}
