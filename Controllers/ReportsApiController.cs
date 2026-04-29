using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/Reports")]
    [ApiController]
    public class ReportsApiController : ControllerBase
    {
    private readonly ManagementContext _context;

    public ReportsApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Reports/employee-list
    [HttpGet("employee-list")]
            public async Task<ActionResult> GenerateEmployeeListReport([FromQuery] string format = "json")
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Supervisor)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Age,
                    e.Phone,
                    e.Address,
                    e.Salary,
                    Department = e.Department.Name,
                    Supervisor = e.Supervisor != null ? e.Supervisor.FullName : "None",
                    HireDate = DateTime.Now // Employee model doesn't have CreatedAt property, using current date as placeholder
                })
                .ToListAsync();

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(employees, "employee_list.csv");
            }
            else if (format.ToLower() == "pdf")
            {
                // For PDF generation, you would typically use a library like iTextSharp
                // For now, return JSON with a message
                return Ok(new
                {
                    Message = "PDF generation would require additional libraries. Currently returning JSON data.",
                    Data = employees,
                    TotalEmployees = employees.Count,
                    GeneratedAt = DateTime.Now
                });
            }

            return Ok(new
            {
                Data = employees,
                TotalEmployees = employees.Count,
                GeneratedAt = DateTime.Now
            });
        }

        // GET: api/Reports/attendance
    [HttpGet("attendance")]
            public async Task<ActionResult> GenerateAttendanceReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] long? departmentId = null,
            [FromQuery] string format = "json")
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .Where(a => a.Date >= startDate && a.Date <= endDate);

            if (departmentId.HasValue)
            {
                query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);
            }

            var attendanceData = await query
                .Select(a => new
                {
                    a.Id,
                    EmployeeName = a.Employee.FullName,
                    Department = a.Employee.Department.Name,
                    a.Date,
                    a.CheckIn,
                    a.CheckOut,
                    a.Status,
                    WorkingHours = (a.CheckOut - a.CheckIn).TotalHours
                })
                .ToListAsync();

            var summary = attendanceData
                .GroupBy(a => a.EmployeeName)
                .Select(g => new
                {
                    EmployeeName = g.Key,
                    TotalDays = g.Count(),
                    PresentDays = g.Count(a => a.Status == "Present"),
                    AbsentDays = g.Count(a => a.Status == "Absent"),
                    LateDays = g.Count(a => a.Status == "Late"),
                    AverageWorkingHours = g.Average(a => a.WorkingHours)
                })
                .ToList();

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(summary, "attendance_report.csv");
            }

            return Ok(new
            {
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                Summary = summary,
                DetailedData = attendanceData,
                GeneratedAt = DateTime.Now
            });
        }

        // GET: api/Reports/payroll
    [HttpGet("payroll")]
            public async Task<ActionResult> GeneratePayrollReport(
            [FromQuery] int month = 0,
            [FromQuery] int year = 0,
            [FromQuery] string format = "json")
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
                .Where(p => p.Month == month && p.Year == year)
                .Select(p => new
                {
                    p.Id,
                    EmployeeName = p.Employee.FullName,
                    Department = p.Employee.Department.Name,
                    p.BasicSalary,
                    p.Bonus,
                    p.Deductions,
                    p.NetSalary,
                    p.Month,
                    p.Year,
                    PaymentDate = new DateTime(p.Year, p.Month, 1).AddMonths(1).AddDays(-1) // Calculate end of month as payment date
                })
                .ToListAsync();

            var summary = new
            {
                Month = month,
                Year = year,
                TotalEmployees = payrolls.Count,
                TotalBasicSalary = payrolls.Sum(p => p.BasicSalary),
                TotalBonus = payrolls.Sum(p => p.Bonus),
                TotalDeductions = payrolls.Sum(p => p.Deductions),
                TotalNetSalary = payrolls.Sum(p => p.NetSalary),
                AverageNetSalary = payrolls.Average(p => p.NetSalary)
            };

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(payrolls, $"payroll_{year}_{month}.csv");
            }

            return Ok(new
            {
                Summary = summary,
                DetailedData = payrolls,
                GeneratedAt = DateTime.Now
            });
        }

        // GET: api/Reports/leave
    [HttpGet("leave")]
            public async Task<ActionResult> GenerateLeaveReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string status = "all",
            [FromQuery] string format = "json")
        {
            startDate ??= DateTime.Today.AddMonths(-6);
            endDate ??= DateTime.Today;

            var query = _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
                .Where(l => l.FromDate >= startDate && l.ToDate <= endDate);

            if (status != "all")
            {
                query = query.Where(l => l.Status == status);
            }

            var leaves = await query
                .Select(l => new
                {
                    l.Id,
                    EmployeeName = l.Employee.FullName,
                    Department = l.Employee.Department.Name,
                    Type = "Leave", // Leave model doesn't have Type property, using default
                    l.Reason,
                    StartDate = l.FromDate,
                    EndDate = l.ToDate,
                    TotalDays = (l.ToDate - l.FromDate).Days + 1,
                    l.Status,
                    ApprovedBy = "System", // Leave model doesn't have ApprovedBy property
                    Comments = "" // Leave model doesn't have Comments property
                })
                .ToListAsync();

            var summary = leaves
                .GroupBy(l => l.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    TotalLeaves = g.Count(),
                    Approved = g.Count(l => l.Status == "Approved"),
                    Pending = g.Count(l => l.Status == "Pending"),
                    Rejected = g.Count(l => l.Status == "Rejected"),
                    TotalDays = g.Sum(l => l.TotalDays)
                })
                .ToList();

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(leaves, "leave_report.csv");
            }

            return Ok(new
            {
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                Summary = summary,
                DetailedData = leaves,
                GeneratedAt = DateTime.Now
            });
        }

        // GET: api/Reports/department-summary
    [HttpGet("department-summary")]
            public async Task<ActionResult> GenerateDepartmentSummaryReport([FromQuery] string format = "json")
        {
            var departments = await _context.Departments
                .Include(d => d.Employees)
                .Select(d => new
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    EmployeeCount = d.Employees.Count,
                    TotalSalary = d.Employees.Sum(e => e.Salary),
                    AverageSalary = d.Employees.Average(e => e.Salary),
                    MaxSalary = d.Employees.Max(e => e.Salary),
                    MinSalary = d.Employees.Min(e => e.Salary)
                })
                .ToListAsync();

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(departments, "department_summary.csv");
            }

            return Ok(new
            {
                TotalDepartments = departments.Count,
                TotalEmployees = departments.Sum(d => d.EmployeeCount),
                TotalSalaryBudget = departments.Sum(d => d.TotalSalary),
                Departments = departments,
                GeneratedAt = DateTime.Now
            });
        }

        // GET: api/Reports/turnover
    [HttpGet("turnover")]
            public async Task<ActionResult> GenerateTurnoverReport(
            [FromQuery] int months = 12,
            [FromQuery] string format = "json")
        {
            var startDate = DateTime.Today.AddMonths(-months);
            
            // This is a simplified example - in a real system you would have employee hire/termination dates
            // For now, we'll use CreatedAt if available, or simulate with random data
            
            var report = new
            {
                Period = $"{startDate:yyyy-MM-dd} to {DateTime.Today:yyyy-MM-dd}",
                TotalEmployees = await _context.Employees.CountAsync(),
                NewHires = 0, // Employee model doesn't have CreatedAt property, cannot track new hires
                TurnoverRate = "N/A", // Would need termination data
                DepartmentBreakdown = await _context.Departments
                    .Include(d => d.Employees)
                    .Select(d => new
                    {
                        Department = d.Name,
                        EmployeeCount = d.Employees.Count,
                        NewHires = 0 // Employee model doesn't have CreatedAt property
                    })
                    .ToListAsync(),
                GeneratedAt = DateTime.Now
            };

            if (format.ToLower() == "csv")
            {
                return GenerateCsvReport(new List<object> { report }, "turnover_report.csv");
            }

            return Ok(report);
        }

    private FileContentResult GenerateCsvReport<T>(IEnumerable<T> data, string filename)
        {
            var sb = new StringBuilder();
            
            // Get headers from first item
            var properties = typeof(T).GetProperties();
            var header = string.Join(",", properties.Select(p => p.Name));
            sb.AppendLine(header);

            // Add data rows
            foreach (var item in data)
            {
                var row = string.Join(",", properties.Select(p => 
                {
                    var value = p.GetValue(item, null);
                    return value == null ? "" : value.ToString().Replace(",", ";");
                }));
                sb.AppendLine(row);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", filename);
        }

        // POST: api/Reports/custom
    [HttpPost("custom")]
            public async Task<ActionResult> GenerateCustomReport([FromBody] CustomReportRequest request)
        {
            // This is a simplified example of a custom report generator
            // In a real system, this would be more sophisticated
            
            if (string.IsNullOrEmpty(request.ReportType))
            {
                return BadRequest(new { message = "Report type is required" });
            }

            var report = new
            {
                ReportType = request.ReportType,
                Parameters = request.Parameters,
                GeneratedAt = DateTime.Now,
                Data = new
                {
                    Message = "Custom report generation would be implemented based on report type and parameters",
                    ExampleData = new
                    {
                        TotalEmployees = await _context.Employees.CountAsync(),
                        TotalDepartments = await _context.Departments.CountAsync(),
                        ActiveUsers = await _context.Users.CountAsync()
                    }
                }
            };

            return Ok(report);
        }
    }

    public class CustomReportRequest
    {
    public string ReportType { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string Format { get; set; } = "json";
    }
}