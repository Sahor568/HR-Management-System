using Management.Models;
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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
    private readonly ManagementContext _context;

    public DashboardController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/stats
    [HttpGet("stats")]
            public async Task<ActionResult<object>> GetDashboardStats()
        {
            var totalEmployees = await _context.Employees.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            
            // Get today's attendance
            var today = DateTime.Today;
            var presentToday = await _context.Attendances
                .Where(a => a.Date.Date == today && a.Status == "Present")
                .CountAsync();
            
            // Get pending leave requests
            var pendingLeaves = await _context.Leaves
                .Where(l => l.Status == "Pending")
                .CountAsync();

            // Only Admin/HR can see payroll totals
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdminOrHR = userRole == "Admin" || userRole == "HR";

            if (isAdminOrHR)
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var totalPayroll = await _context.Payrolls
                    .Where(p => p.Month == currentMonth && p.Year == currentYear)
                    .SumAsync(p => p.NetSalary);

                return new
                {
                    TotalEmployees = totalEmployees,
                    TotalDepartments = totalDepartments,
                    TotalUsers = totalUsers,
                    PresentToday = presentToday,
                    PendingLeaves = pendingLeaves,
                    TotalPayroll = totalPayroll,
                    StatsDate = DateTime.Now
                };
            }

            return new
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                PresentToday = presentToday,
                PendingLeaves = pendingLeaves,
                StatsDate = DateTime.Now
            };
        }

        // GET: api/Dashboard/employee-distribution
    [HttpGet("employee-distribution")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<IEnumerable<object>>> GetEmployeeDistribution()
        {
            var distribution = await _context.Employees
                .Include(e => e.Department)
                .GroupBy(e => e.Department != null ? e.Department.Name : "Unassigned")
                .Select(g => new
                {
                    DepartmentName = g.Key,
                    EmployeeCount = g.Count(),
                    AverageSalary = g.Average(e => e.Salary)
                })
                .ToListAsync();

            return Ok(distribution);
        }

        // GET: api/Dashboard/attendance-trend
    [HttpGet("attendance-trend")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<IEnumerable<object>>> GetAttendanceTrend([FromQuery] int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);
            var totalEmployees = await _context.Employees.CountAsync();
            
            var trend = await _context.Attendances
                .Where(a => a.Date >= startDate)
                .GroupBy(a => a.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    PresentCount = g.Count(a => a.Status == "Present"),
                    AbsentCount = g.Count(a => a.Status == "Absent"),
                    LateCount = g.Count(a => a.Status == "Late"),
                    TotalEmployees = totalEmployees
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Ok(trend);
        }

        // GET: api/Dashboard/leave-summary
    [HttpGet("leave-summary")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<object>> GetLeaveSummary([FromQuery] int month = 0, [FromQuery] int year = 0)
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var leaves = await _context.Leaves
                .Where(l => l.FromDate.Month == month && l.FromDate.Year == year)
                .ToListAsync();

            var summary = new
            {
                TotalLeaves = leaves.Count,
                Approved = leaves.Count(l => l.Status == "Approved"),
                Pending = leaves.Count(l => l.Status == "Pending"),
                Rejected = leaves.Count(l => l.Status == "Rejected"),
                ByType = leaves.GroupBy(l => l.Reason) // Using Reason instead of Type since Leave model doesn't have Type property
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count()
                    })
            };

            return Ok(summary);
        }

        // GET: api/Dashboard/payroll-summary
    [HttpGet("payroll-summary")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<object>> GetPayrollSummary([FromQuery] int month = 0, [FromQuery] int year = 0)
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .Where(p => p.Month == month && p.Year == year)
                .ToListAsync();

            if (!payrolls.Any())
            {
                return Ok(new
                {
                    Message = "No payroll data for the specified period",
                    Month = month,
                    Year = year
                });
            }

            var summary = new
            {
                TotalEmployees = payrolls.Count,
                TotalBasicSalary = payrolls.Sum(p => p.BasicSalary),
                TotalBonus = payrolls.Sum(p => p.Bonus),
                TotalDeductions = payrolls.Sum(p => p.Deductions),
                TotalNetSalary = payrolls.Sum(p => p.NetSalary),
                AverageNetSalary = payrolls.Average(p => p.NetSalary),
                HighestSalary = payrolls.Max(p => p.NetSalary),
                LowestSalary = payrolls.Min(p => p.NetSalary),
                TopEarners = payrolls
                    .OrderByDescending(p => p.NetSalary)
                    .Take(5)
                    .Select(p => new
                    {
                        FullName = p.Employee != null ? p.Employee.FullName : "Unknown",
                        DepartmentName = p.Employee != null && p.Employee.Department != null ? p.Employee.Department.Name : "Unassigned",
                        p.NetSalary
                    })
            };

            return Ok(summary);
        }

        // GET: api/Dashboard/upcoming-holidays
    [HttpGet("upcoming-holidays")]
            public async Task<ActionResult<IEnumerable<object>>> GetUpcomingHolidays([FromQuery] int count = 10)
        {
            var today = DateTime.Today;
            
            var holidays = await _context.Holidays
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .Take(count)
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    h.Date,
                    DaysUntil = (h.Date - today).Days
                })
                .ToListAsync();

            return Ok(holidays);
        }

        // GET: api/Dashboard/employee-performance
    [HttpGet("employee-performance")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<IEnumerable<object>>> GetEmployeePerformance([FromQuery] int top = 10)
        {
            // Calculate performance based on attendance and leaves
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            
            var performance = await _context.Employees
                .Include(e => e.Department)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    DepartmentName = e.Department != null ? e.Department.Name : "Unassigned",
                    e.Salary,
                    AttendanceRate = _context.Attendances
                        .Where(a => a.EmployeeId == e.Id && a.Date >= thirtyDaysAgo)
                        .Count(a => a.Status == "Present") * 100.0 /
                        Math.Max(1, _context.Attendances
                            .Count(a => a.EmployeeId == e.Id && a.Date >= thirtyDaysAgo)),
                    LeavesTaken = _context.Leaves
                        .Count(l => l.EmployeeId == e.Id &&
                                   l.FromDate >= thirtyDaysAgo &&
                                   l.Status == "Approved")
                })
                .OrderByDescending(p => p.AttendanceRate)
                .Take(top)
                .ToListAsync();

            return Ok(performance);
        }

        // GET: api/Dashboard/department-performance
    [HttpGet("department-performance")]
    [Authorize(Policy = "AdminOrHR")]
            public async Task<ActionResult<IEnumerable<object>>> GetDepartmentPerformance()
        {
            var performance = await _context.Departments
                .Include(d => d.Employees)
                .Select(d => new
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count,
                    TotalSalary = d.Employees.Sum(e => e.Salary),
                    AverageSalary = d.Employees.Average(e => e.Salary),
                    AttendanceRate = d.Employees.Count > 0 ?
                        d.Employees.Average(e =>
                            _context.Attendances
                                .Count(a => a.EmployeeId == e.Id && a.Status == "Present") * 100.0 /
                                Math.Max(1, _context.Attendances
                                    .Count(a => a.EmployeeId == e.Id))) : 0
                })
                .OrderByDescending(p => p.AttendanceRate)
                .ToListAsync();

            return Ok(performance);
        }
    }
}
