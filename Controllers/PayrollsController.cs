using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Management.Controllers
{
    [Authorize]
    public class PayrollsController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public PayrollsController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [Route("Payrolls")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: api/Payrolls
        [HttpGet("api/Payrolls")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetPayrolls([FromQuery] int? month = null, [FromQuery] int? year = null)
        {
            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .AsQueryable();

            if (month.HasValue)
                query = query.Where(p => p.Month == month.Value);

            if (year.HasValue)
                query = query.Where(p => p.Year == year.Value);

            var payrolls = await query
                .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
                .Select(p => new
                {
                    p.Id,
                    p.EmployeeId,
                    Employee = new
                    {
                        p.Employee!.Id,
                        p.Employee.FullName,
                        p.Employee.Position,
                        Department = p.Employee!.Department != null ? new { p.Employee.Department.Id, p.Employee.Department.Name } : null
                    },
                    p.Month,
                    p.Year,
                    p.BasicSalary,
                    p.Bonus,
                    p.Deductions,
                    p.NetSalary,
                    p.ApprovalStatus,
                    p.ApprovalRemarks,
                    p.ApprovedBy,
                    p.ApprovedAt
                })
                .ToListAsync();

            return Ok(payrolls);
        }

        // GET: api/Payrolls/employee/{employeeId}
        [HttpGet("api/Payrolls/employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPayrollsByEmployee(long employeeId)
        {
            // Employees can only view their own payroll; Admin/HR can view any
            var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var ownEmployeeId = await _context.Employees
                    .Where(e => e.UserId == currentUserId)
                    .Select(e => e.Id)
                    .FirstOrDefaultAsync();
                if (employeeId != ownEmployeeId)
                    return StatusCode(403, new { message = "You can only view your own payroll records" });
            }

            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
                .Select(p => new
                {
                    p.Id,
                    p.EmployeeId,
                    Employee = new
                    {
                        p.Employee!.Id,
                        p.Employee.FullName,
                        p.Employee.Position,
                        Department = p.Employee!.Department != null ? new { p.Employee.Department.Id, p.Employee.Department.Name } : null
                    },
                    p.Month,
                    p.Year,
                    p.BasicSalary,
                    p.Bonus,
                    p.Deductions,
                    p.NetSalary,
                    p.ApprovalStatus,
                    p.ApprovalRemarks
                })
                .ToListAsync();

            return Ok(payrolls);
        }

        // POST: api/Payrolls
        [HttpPost("api/Payrolls")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> CreatePayroll([FromBody] CreatePayrollDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employeeExists = await _context.Employees.AnyAsync(e => e.Id == dto.EmployeeId);
                if (!employeeExists)
                    return BadRequest(new { message = "Employee not found" });

                // Check if payroll already exists for this employee/month/year
                var existing = await _context.Payrolls
                    .AnyAsync(p => p.EmployeeId == dto.EmployeeId && p.Month == dto.Month && p.Year == dto.Year);
                if (existing)
                    return BadRequest(new { message = "Payroll already exists for this employee for the specified month/year" });

                var payroll = new Payroll
                {
                    EmployeeId = dto.EmployeeId,
                    Month = dto.Month,
                    Year = dto.Year,
                    BasicSalary = dto.BasicSalary,
                    Bonus = dto.Bonus,
                    Deductions = dto.Deductions,
                    NetSalary = dto.BasicSalary + dto.Bonus - dto.Deductions,
                    ApprovalStatus = "Pending"
                };

                _context.Payrolls.Add(payroll);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    payroll.Id,
                    payroll.EmployeeId,
                    payroll.Month,
                    payroll.Year,
                    payroll.BasicSalary,
                    payroll.Bonus,
                    payroll.Deductions,
                    payroll.NetSalary,
                    payroll.ApprovalStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating payroll", error = ex.Message });
            }
        }

        // PUT: api/Payrolls/{id}
        [HttpPut("api/Payrolls/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> UpdatePayroll(long id, [FromBody] UpdatePayrollDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var payroll = await _context.Payrolls.FindAsync(id);
                if (payroll == null)
                    return NotFound(new { message = "Payroll not found" });

                if (dto.BasicSalary.HasValue) payroll.BasicSalary = dto.BasicSalary.Value;
                if (dto.Bonus.HasValue) payroll.Bonus = dto.Bonus.Value;
                if (dto.Deductions.HasValue) payroll.Deductions = dto.Deductions.Value;

                payroll.NetSalary = payroll.BasicSalary + payroll.Bonus - payroll.Deductions;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Payroll updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating payroll", error = ex.Message });
            }
        }

        // PUT: api/Payrolls/{id}/approve
        [HttpPut("api/Payrolls/{id}/approve")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> ApprovePayroll(long id, [FromBody] PayrollApprovalDto? dto = null)
        {
            try
            {
                var payroll = await _context.Payrolls.FindAsync(id);
                if (payroll == null)
                    return NotFound(new { message = "Payroll not found" });

                if (payroll.ApprovalStatus != "Pending")
                    return BadRequest(new { message = "Only pending payrolls can be approved" });

                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                payroll.ApprovalStatus = "Approved";
                payroll.ApprovedBy = userId;
                payroll.ApprovedAt = DateTime.UtcNow;
                payroll.ApprovalRemarks = dto?.Remarks;

                await _context.SaveChangesAsync();

                // Notify the employee about bonus approval if there's a bonus
                if (payroll.Bonus > 0)
                {
                    var employee = await _context.Employees.FindAsync(payroll.EmployeeId);
                    if (employee != null)
                    {
                        await _notificationService.NotifyBonusApprovalAsync(payroll.Id, payroll.EmployeeId, employee.FullName, true, dto?.Remarks);
                    }
                }

                return Ok(new { message = "Payroll approved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving payroll", error = ex.Message });
            }
        }

        // PUT: api/Payrolls/{id}/reject
        [HttpPut("api/Payrolls/{id}/reject")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> RejectPayroll(long id, [FromBody] PayrollApprovalDto? dto = null)
        {
            try
            {
                var payroll = await _context.Payrolls.FindAsync(id);
                if (payroll == null)
                    return NotFound(new { message = "Payroll not found" });

                if (payroll.ApprovalStatus != "Pending")
                    return BadRequest(new { message = "Only pending payrolls can be rejected" });

                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                payroll.ApprovalStatus = "Rejected";
                payroll.ApprovedBy = userId;
                payroll.ApprovedAt = DateTime.UtcNow;
                payroll.ApprovalRemarks = dto?.Remarks;

                await _context.SaveChangesAsync();

                // Notify the employee about bonus rejection if there's a bonus
                if (payroll.Bonus > 0)
                {
                    var employee = await _context.Employees.FindAsync(payroll.EmployeeId);
                    if (employee != null)
                    {
                        await _notificationService.NotifyBonusApprovalAsync(payroll.Id, payroll.EmployeeId, employee.FullName, false, dto?.Remarks);
                    }
                }

                return Ok(new { message = "Payroll rejected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while rejecting payroll", error = ex.Message });
            }
        }

        // POST: api/Payrolls/generate
        [HttpPost("api/Payrolls/generate")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> GeneratePayroll([FromBody] GeneratePayrollDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (dto.Month < 1 || dto.Month > 12)
                    return BadRequest(new { message = "Invalid month" });
                if (dto.Year < 2000 || dto.Year > 2100)
                    return BadRequest(new { message = "Invalid year" });

                var employees = await _context.Employees.ToListAsync();
                var generated = 0;
                var skipped = 0;

                var existingEmployeeIds = await _context.Payrolls
                    .Where(p => p.Month == dto.Month && p.Year == dto.Year)
                    .Select(p => p.EmployeeId)
                    .ToListAsync();

                foreach (var employee in employees)
                {
                    if (existingEmployeeIds.Contains(employee.Id))
                    {
                        skipped++;
                        continue;
                    }

                    var payroll = new Payroll
                    {
                        EmployeeId = employee.Id,
                        Month = dto.Month,
                        Year = dto.Year,
                        BasicSalary = employee.Salary,
                        Bonus = 0,
                        Deductions = 0,
                        NetSalary = employee.Salary,
                        ApprovalStatus = "Pending"
                    };

                    _context.Payrolls.Add(payroll);
                    generated++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Generated {generated} payroll records, skipped {skipped} (already exist)",
                    generated,
                    skipped,
                    month = dto.Month,
                    year = dto.Year
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating payroll", error = ex.Message });
            }
        }

        // DELETE: api/Payrolls/{id}
        [HttpDelete("api/Payrolls/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> DeletePayroll(long id)
        {
            try
            {
                var payroll = await _context.Payrolls.FindAsync(id);
                if (payroll == null)
                    return NotFound(new { message = "Payroll not found" });

                _context.Payrolls.Remove(payroll);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payroll deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting payroll", error = ex.Message });
            }
        }

        public class CreatePayrollDto
        {
            public long EmployeeId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public decimal BasicSalary { get; set; }
            public decimal Bonus { get; set; }
            public decimal Deductions { get; set; }
        }

        public class UpdatePayrollDto
        {
            public decimal? BasicSalary { get; set; }
            public decimal? Bonus { get; set; }
            public decimal? Deductions { get; set; }
        }

        public class PayrollApprovalDto
        {
            public string? Remarks { get; set; }
        }

        public class GeneratePayrollDto
        {
            public int Month { get; set; }
            public int Year { get; set; }
        }
    }
}
