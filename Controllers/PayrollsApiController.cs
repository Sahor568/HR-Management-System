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
    [Route("api/Payrolls")]
    [ApiController]
    public class PayrollsApiController : ControllerBase
    {
    private readonly ManagementContext _context;

    public PayrollsApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Payrolls
    [HttpGet]
            public async Task<ActionResult<IEnumerable<Payroll>>> GetPayrolls()
        {
            return await _context.Payrolls
                .Include(p => p.Employee)
                .ToListAsync();
        }

        // GET: api/Payrolls/5
    [HttpGet("{id}")]
            public async Task<ActionResult<Payroll>> GetPayroll(long id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payroll == null)
            {
                return NotFound();
            }

            // Check access: Employees can only view their own payroll
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || payroll.EmployeeId != employee.Id)
                    return Forbid();
            }

            return payroll;
        }

        // GET: api/Payrolls/employee/{employeeId}
    [HttpGet("employee/{employeeId}")]
            public async Task<ActionResult<IEnumerable<Payroll>>> GetPayrollsByEmployee(long employeeId)
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Employees can only view their own payroll
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (employee == null || employeeId != employee.Id)
                    return Forbid();
            }

            var payrolls = await _context.Payrolls
                .Where(p => p.EmployeeId == employeeId)
                .Include(p => p.Employee)
                .ToListAsync();

            return payrolls;
        }

        // GET: api/Payrolls/month/{year}/{month}
    [HttpGet("month/{year}/{month}")]
            public async Task<ActionResult<IEnumerable<Payroll>>> GetPayrollsByMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                return BadRequest("Month must be between 1 and 12");

            var payrolls = await _context.Payrolls
                .Where(p => p.Year == year && p.Month == month)
                .Include(p => p.Employee)
                .ToListAsync();

            return payrolls;
        }

        // POST: api/Payrolls
    [HttpPost]
            public async Task<ActionResult<Payroll>> PostPayroll(Payroll payroll)
        {
            if (payroll.EmployeeId <= 0)
                return BadRequest("Valid EmployeeId is required");

            if (payroll.Month < 1 || payroll.Month > 12)
                return BadRequest("Month must be between 1 and 12");

            if (payroll.Year < 2000 || payroll.Year > 2100)
                return BadRequest("Year must be between 2000 and 2100");

            if (payroll.BasicSalary < 0)
                return BadRequest("BasicSalary cannot be negative");

            if (payroll.Bonus < 0)
                return BadRequest("Bonus cannot be negative");

            if (payroll.Deductions < 0)
                return BadRequest("Deductions cannot be negative");

            // Calculate net salary
            payroll.NetSalary = payroll.BasicSalary + payroll.Bonus - payroll.Deductions;

            // Check if payroll already exists for this employee for this month/year
            var existing = await _context.Payrolls
                .FirstOrDefaultAsync(p => p.EmployeeId == payroll.EmployeeId && 
                                         p.Year == payroll.Year && 
                                         p.Month == payroll.Month);
            if (existing != null)
                return BadRequest("Payroll already exists for this employee for the specified month and year");

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayroll), new { id = payroll.Id }, payroll);
        }

        // POST: api/Payrolls/generate
    [HttpPost("generate")]
            public async Task<ActionResult> GeneratePayrolls([FromBody] PayrollGenerationRequest request)
        {
            if (request.Month < 1 || request.Month > 12)
                return BadRequest("Month must be between 1 and 12");

            if (request.Year < 2000 || request.Year > 2100)
                return BadRequest("Year must be between 2000 and 2100");

            // Get all employees
            var employees = await _context.Employees.ToListAsync();
            var generatedCount = 0;

            foreach (var employee in employees)
            {
                // Check if payroll already exists
                var existing = await _context.Payrolls
                    .FirstOrDefaultAsync(p => p.EmployeeId == employee.Id && 
                                             p.Year == request.Year && 
                                             p.Month == request.Month);
                if (existing != null)
                    continue;

                // Calculate attendance-based deductions
                var attendanceDays = await _context.Attendances
                    .Where(a => a.EmployeeId == employee.Id &&
                               a.Date.Year == request.Year &&
                               a.Date.Month == request.Month &&
                               a.Status == "Present")
                    .CountAsync();

                var totalDaysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
                var absentDays = totalDaysInMonth - attendanceDays;

                // Basic salary (from employee record)
                var basicSalary = employee.Salary;
                var deductions = absentDays * (basicSalary / totalDaysInMonth);
                var bonus = 0m; // Could be calculated based on performance

                var payroll = new Payroll
                {
                    EmployeeId = employee.Id,
                    Year = request.Year,
                    Month = request.Month,
                    BasicSalary = basicSalary,
                    Bonus = bonus,
                    Deductions = deductions,
                    NetSalary = basicSalary + bonus - deductions
                };

                _context.Payrolls.Add(payroll);
                generatedCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Generated {generatedCount} payroll records" });
        }

        // PUT: api/Payrolls/5
    [HttpPut("{id}")]
            public async Task<IActionResult> PutPayroll(long id, Payroll payroll)
        {
            if (id != payroll.Id)
            {
                return BadRequest();
            }

            if (payroll.Month < 1 || payroll.Month > 12)
                return BadRequest("Month must be between 1 and 12");

            if (payroll.Year < 2000 || payroll.Year > 2100)
                return BadRequest("Year must be between 2000 and 2100");

            if (payroll.BasicSalary < 0)
                return BadRequest("BasicSalary cannot be negative");

            if (payroll.Bonus < 0)
                return BadRequest("Bonus cannot be negative");

            if (payroll.Deductions < 0)
                return BadRequest("Deductions cannot be negative");

            // Recalculate net salary
            payroll.NetSalary = payroll.BasicSalary + payroll.Bonus - payroll.Deductions;

            _context.Entry(payroll).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PayrollExists(id))
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

        // DELETE: api/Payrolls/5
    [HttpDelete("{id}")]
            public async Task<IActionResult> DeletePayroll(long id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll == null)
            {
                return NotFound();
            }

            _context.Payrolls.Remove(payroll);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    private bool PayrollExists(long id)
        {
            return _context.Payrolls.Any(e => e.Id == id);
        }
    }

    public class PayrollGenerationRequest
    {
    public int Year { get; set; }
    public int Month { get; set; }
    }
}
