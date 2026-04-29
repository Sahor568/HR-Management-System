using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/Employees")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
    private readonly ManagementContext _context;

    public EmployeesApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Employees
    [HttpGet]
            public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .ToListAsync();
        }

        // GET: api/Employees/5
    [HttpGet("{id}")]
            public async Task<ActionResult<Employee>> GetEmployee(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Employees can only view their own profile unless they are Admin/HR
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR" && employee.UserId != currentUserId)
            {
                return Forbid();
            }

            return employee;
        }

        // POST: api/Employees
    [HttpPost]
            public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.FullName))
                return BadRequest("Full name is required");

            if (employee.DepartmentId <= 0)
                return BadRequest("Valid DepartmentId is required");

            if (employee.UserId <= 0)
                return BadRequest("Valid UserId is required");

            // Verify department exists
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == employee.DepartmentId);
            if (!departmentExists)
                return BadRequest("Department does not exist");

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == employee.UserId);
            if (!userExists)
                return BadRequest("User does not exist");

            // Validate supervisor if provided
            if (employee.SupervisorId.HasValue && employee.SupervisorId > 0)
            {
                var supervisorExists = await _context.Employees.AnyAsync(e => e.Id == employee.SupervisorId.Value);
                if (!supervisorExists)
                    return BadRequest("Supervisor does not exist");

                // Prevent self-supervision
                if (employee.SupervisorId.Value == employee.Id)
                    return BadRequest("Employee cannot be their own supervisor");

                // Check for circular reference (supervisor cannot be a subordinate)
                // This is a simple check; for complex hierarchies, we'd need recursion
                var supervisor = await _context.Employees
                    .Include(e => e.Subordinates)
                    .FirstOrDefaultAsync(e => e.Id == employee.SupervisorId.Value);
                
                // Check if the employee is already in the supervisor's chain (would create a cycle)
                // For now, we'll just prevent direct cycles
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // PUT: api/Employees/5
    [HttpPut("{id}")]
            public async Task<IActionResult> PutEmployee(long id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(employee.FullName))
                return BadRequest("Full name is required");

            // Employees can update their own profile (limited fields)
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            var existingEmployee = await _context.Employees.FindAsync(id);
            if (existingEmployee == null)
                return NotFound();

            // Validate supervisor if provided (for Admin/HR updates)
            if (currentUserRole == "Admin" || currentUserRole == "HR")
            {
                if (employee.SupervisorId.HasValue && employee.SupervisorId > 0)
                {
                    var supervisorExists = await _context.Employees.AnyAsync(e => e.Id == employee.SupervisorId.Value);
                    if (!supervisorExists)
                        return BadRequest("Supervisor does not exist");

                    // Prevent self-supervision
                    if (employee.SupervisorId.Value == employee.Id)
                        return BadRequest("Employee cannot be their own supervisor");

                    // Check for circular reference (prevent supervisor cycles)
                    // Simple check: ensure the supervisor is not already a subordinate
                    var isCircular = await CheckCircularReference(id, employee.SupervisorId.Value);
                    if (isCircular)
                        return BadRequest("Circular reference detected: supervisor cannot be a subordinate");
                }
            }

            // If not Admin/HR, can only update own employee record and only certain fields
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                if (existingEmployee.UserId != currentUserId)
                    return Forbid();

                // Employees can only update their personal info, not department, salary, or supervisor
                existingEmployee.FullName = employee.FullName;
                existingEmployee.Age = employee.Age;
                existingEmployee.Phone = employee.Phone;
                existingEmployee.Address = employee.Address;
            }
            else
            {
                // Admin/HR can update all fields
                _context.Entry(existingEmployee).CurrentValues.SetValues(employee);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
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

        // DELETE: api/Employees/5
    [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteEmployee(long id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Employees/by-user/{userId}
    [HttpGet("by-user/{userId}")]
            public async Task<ActionResult<Employee>> GetEmployeeByUser(long userId)
        {
            var currentUserId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Users can only view their own employee record unless they are Admin/HR
            if (currentUserRole != "Admin" && currentUserRole != "HR" && currentUserId != userId)
            {
                return Forbid();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        // GET: api/Employees/{id}/subordinates
    [HttpGet("{id}/subordinates")]
            public async Task<ActionResult<IEnumerable<Employee>>> GetSubordinates(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Subordinates)
                    .ThenInclude(s => s.Department)
                .Include(e => e.Subordinates)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee.Subordinates);
        }

        // GET: api/Employees/{id}/supervisor-chain
    [HttpGet("{id}/supervisor-chain")]
            public async Task<ActionResult<IEnumerable<Employee>>> GetSupervisorChain(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            var chain = new List<Employee>();
            var current = employee.Supervisor;
            
            while (current != null)
            {
                chain.Add(current);
                current = await _context.Employees
                    .Include(e => e.Supervisor)
                    .FirstOrDefaultAsync(e => e.Id == current.Id);
                current = current?.Supervisor;
            }

            return Ok(chain);
        }

        // GET: api/Employees/with-supervisor
    [HttpGet("with-supervisor")]
            public async Task<ActionResult<IEnumerable<object>>> GetEmployeesWithSupervisor()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : "N/A",
                    SupervisorId = e.SupervisorId,
                    SupervisorName = e.Supervisor != null ? e.Supervisor.FullName : "No Supervisor",
                    e.Phone,
                    e.Salary
                })
                .ToListAsync();

            return Ok(employees);
        }

    private async Task<bool> CheckCircularReference(long employeeId, long potentialSupervisorId)
        {
            // If the potential supervisor is the same as employee, it's circular
            if (employeeId == potentialSupervisorId)
                return true;

            // Get all subordinates of the potential supervisor (including indirect)
            var visited = new HashSet<long>();
            var queue = new Queue<long>();
            queue.Enqueue(potentialSupervisorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (visited.Contains(currentId))
                    continue;
                    
                visited.Add(currentId);

                // If we find the employee in the supervisor's chain, it's circular
                if (currentId == employeeId)
                    return true;

                // Get direct subordinates of current employee
                var currentEmployee = await _context.Employees
                    .Include(e => e.Subordinates)
                    .FirstOrDefaultAsync(e => e.Id == currentId);

                if (currentEmployee != null)
                {
                    foreach (var subordinate in currentEmployee.Subordinates)
                    {
                        queue.Enqueue(subordinate.Id);
                    }
                }
            }

            return false;
        }

    private bool EmployeeExists(long id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
