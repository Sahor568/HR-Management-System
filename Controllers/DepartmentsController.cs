using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly ManagementContext _context;

        public DepartmentsController(ManagementContext context)
        {
            _context = context;
        }

        // MVC Action - Returns HTML view for /Departments
        [Authorize(Policy = "AdminOrHR")]
        public IActionResult Index()
        {
            return View();
        }

        // API Endpoints - All under /api/Departments route

        // GET: api/Departments
        [HttpGet("api/Departments")]
        public async Task<ActionResult<IEnumerable<object>>> GetDepartments()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdminOrHR = userRole == "Admin" || userRole == "HR";

            if (isAdminOrHR)
            {
                // Admin/HR can see departments with employee details
                return await _context.Departments
                    .Include(d => d.Employees)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        EmployeeCount = d.Employees.Count,
                        Employees = d.Employees.Select(e => new
                        {
                            e.Id,
                            e.FullName,
                            e.Salary,
                            e.Phone
                        })
                    })
                    .ToListAsync();
            }

            // Regular employees only see department names and employee count
            return await _context.Departments
                .Include(d => d.Employees)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    EmployeeCount = d.Employees.Count
                })
                .ToListAsync();
        }

        // GET: api/Departments/5
        [HttpGet("api/Departments/{id}")]
        public async Task<ActionResult<object>> GetDepartment(long id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdminOrHR = userRole == "Admin" || userRole == "HR";

            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            if (isAdminOrHR)
            {
                return new
                {
                    department.Id,
                    department.Name,
                    EmployeeCount = department.Employees.Count,
                    Employees = department.Employees.Select(e => new
                    {
                        e.Id,
                        e.FullName,
                        e.Salary,
                        e.Phone
                    })
                };
            }

            return new
            {
                department.Id,
                department.Name,
                EmployeeCount = department.Employees.Count
            };
        }

        // POST: api/Departments
        [HttpPost("api/Departments")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> PostDepartment([FromBody] CreateDepartmentDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Department name is required" });

            var department = new Department { Name = dto.Name };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return Ok(new { department.Id, department.Name });
        }

        // PUT: api/Departments/5
        [HttpPut("api/Departments/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> PutDepartment(long id, [FromBody] UpdateDepartmentDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound(new { message = "Department not found" });

            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Department name is required" });

            department.Name = dto.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
                {
                    return NotFound(new { message = "Department not found" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { department.Id, department.Name });
        }

        // DELETE: api/Departments/5
        [HttpDelete("api/Departments/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            // Check if department has employees
            var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
            if (hasEmployees)
            {
                return BadRequest(new { message = "Cannot delete department that has employees. Reassign or remove employees first." });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department deleted successfully" });
        }

        private bool DepartmentExists(long id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }

        public class CreateDepartmentDto
        {
            public string Name { get; set; } = string.Empty;
        }

        public class UpdateDepartmentDto
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
