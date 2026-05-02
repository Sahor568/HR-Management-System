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
    [Authorize]
    public class SupervisorController : Controller
    {
        private readonly ManagementContext _context;

        public SupervisorController(ManagementContext context)
        {
            _context = context;
        }

        // GET: Supervisor/Index
        [Route("Supervisor")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: Supervisor/Hierarchy
        [Route("Supervisor/Hierarchy")]
        public IActionResult Hierarchy()
        {
            return View();
        }

        // API: Get organizational hierarchy (simplified - returns flat list)
        [HttpGet("api/Supervisor/hierarchy")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> GetOrganizationalHierarchy()
        {
            // Get all employees with their supervisors and departments
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Supervisor)
                .Include(e => e.User)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Position,
                    e.Salary,
                    Department = e.Department != null ? new { e.Department.Id, e.Department.Name } : null,
                    SupervisorId = e.SupervisorId,
                    SupervisorName = e.Supervisor != null ? e.Supervisor.FullName : null,
                    Email = e.User != null ? e.User.Email : null,
                    SubordinateCount = e.Subordinates.Count
                })
                .ToListAsync();

            return Ok(new
            {
                Employees = employees,
                TotalEmployees = employees.Count,
                RootEmployees = employees.Count(e => !e.SupervisorId.HasValue || e.SupervisorId == 0)
            });
        }

        // API: Get supervisor chain for current user
        [HttpGet("api/Supervisor/my-chain")]
        public async Task<ActionResult<object>> GetMySupervisorChain()
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Invalid user session" });

            // Find employee for current user, include supervisor chain
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Supervisor)
                    .ThenInclude(s => s!.Department)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (employee == null)
                return NotFound(new { message = "No employee record found for current user" });

            // Build the supervisor chain by walking up the tree
            var chain = new List<object>();
            var currentSupervisorId = employee.SupervisorId;
            int level = 1;
            var visited = new HashSet<long>(); // prevent infinite loops

            while (currentSupervisorId.HasValue && currentSupervisorId > 0 && !visited.Contains(currentSupervisorId.Value))
            {
                visited.Add(currentSupervisorId.Value);
                var sup = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == currentSupervisorId.Value);

                if (sup == null) break;

                chain.Add(new
                {
                    Level = level++,
                    Id = sup.Id,
                    Name = sup.FullName,
                    Position = sup.Position ?? "N/A",
                    Department = sup.Department != null ? sup.Department.Name : "N/A",
                    Email = sup.User != null ? sup.User.Email : null
                });

                currentSupervisorId = sup.SupervisorId;
            }

            return Ok(new
            {
                Employee = new
                {
                    employee.Id,
                    employee.FullName,
                    employee.Position,
                    Department = employee.Department != null ? employee.Department.Name : "N/A"
                },
                SupervisorChain = chain,
                ChainLength = chain.Count
            });
        }

        // API: Get employees by supervisor
        [HttpGet("api/Supervisor/{supervisorId}/subordinates")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubordinates(long supervisorId)
        {
            var subordinates = await _context.Employees
                .Where(e => e.SupervisorId == supervisorId)
                .Include(e => e.Department)
                .Include(e => e.User)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Position,
                    e.Salary,
                    e.HireDate,
                    Department = e.Department != null ? e.Department.Name : "N/A",
                    Email = e.User != null ? e.User.Email : null,
                    SubordinateCount = e.Subordinates.Count
                })
                .ToListAsync();

            return Ok(subordinates);
        }

        // API: Get all supervisors (employees who have subordinates)
        [HttpGet("api/Supervisor/supervisors")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetSupervisors()
        {
            var supervisors = await _context.Employees
                .Where(e => e.Subordinates.Any())
                .Include(e => e.Department)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Position,
                    Department = e.Department != null ? e.Department.Name : "N/A",
                    SubordinateCount = e.Subordinates.Count
                })
                .ToListAsync();

            return Ok(supervisors);
        }
    }
}