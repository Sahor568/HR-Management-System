using Management.Models;
using Management.Services;
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
    public class EmployeesController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public EmployeesController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // MVC Action - Returns HTML view for /Employees
        [Authorize(Policy = "AdminOrHR")]
        [Route("Employees")]
        public IActionResult Index()
        {
            return View();
        }

        // API Endpoints - All under /api/Employees route

        // GET: api/Employees
        [HttpGet("api/Employees")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<IEnumerable<object>>> GetEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Age,
                    e.Phone,
                    e.Address,
                    e.Position,
                    e.HireDate,
                    e.Salary,
                    e.DepartmentId,
                    Department = e.Department != null ? new { e.Department.Id, e.Department.Name } : null,
                    e.UserId,
                    User = e.User != null ? new { e.User.Id, e.User.Email, e.User.Role } : null,
                    e.SupervisorId,
                    Supervisor = e.Supervisor != null ? new { e.Supervisor.Id, e.Supervisor.FullName } : null
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/Employees/5
        [HttpGet("api/Employees/{id}")]
        public async Task<ActionResult<object>> GetEmployee(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            // Employees can only view their own profile unless they are Admin/HR
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR" && employee.UserId != currentUserId)
            {
                return StatusCode(403, new { message = "You can only view your own employee profile" });
            }

            return Ok(new
            {
                employee.Id,
                employee.FullName,
                employee.Age,
                employee.Phone,
                employee.Address,
                employee.Position,
                employee.HireDate,
                employee.Salary,
                employee.DepartmentId,
                Department = employee.Department != null ? new { employee.Department.Id, employee.Department.Name } : null,
                employee.UserId,
                User = employee.User != null ? new { employee.User.Id, employee.User.Email, employee.User.Role } : null,
                employee.SupervisorId,
                Supervisor = employee.Supervisor != null ? new { employee.Supervisor.Id, employee.Supervisor.FullName } : null
            });
        }

        // GET: api/Employees/my-profile
        [HttpGet("api/Employees/my-profile")]
        public async Task<ActionResult<object>> GetMyProfile()
        {
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Invalid user session" });

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (employee == null)
                return NotFound(new { message = "No employee record found for current user" });

            return Ok(new
            {
                employee.Id,
                employee.FullName,
                employee.Age,
                employee.Phone,
                employee.Address,
                employee.Position,
                employee.HireDate,
                employee.Salary,
                employee.DepartmentId,
                Department = employee.Department != null ? new { employee.Department.Id, employee.Department.Name } : null,
                employee.UserId,
                User = employee.User != null ? new { employee.User.Id, employee.User.Email, employee.User.Role } : null,
                employee.SupervisorId,
                Supervisor = employee.Supervisor != null ? new { employee.Supervisor.Id, employee.Supervisor.FullName } : null
            });
        }

        // POST: api/Employees
        [HttpPost("api/Employees")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> PostEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });
            if (string.IsNullOrEmpty(dto.FullName))
                return BadRequest(new { message = "Full name is required" });

            if (dto.DepartmentId <= 0)
                return BadRequest(new { message = "Valid DepartmentId is required" });

            // Verify department exists
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                return BadRequest(new { message = "Department does not exist" });

            if (dto.UserId.HasValue && dto.UserId > 0)
            {
                // Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId.Value);
                if (!userExists)
                    return BadRequest(new { message = "User does not exist" });

                // Verify user is not already assigned to another employee
                var userAlreadyAssigned = await _context.Employees.AnyAsync(e => e.UserId == dto.UserId.Value);
                if (userAlreadyAssigned)
                    return BadRequest(new { message = "User is already assigned to another employee" });
            }

            var employee = new Employee
            {
                FullName = dto.FullName,
                Age = dto.Age,
                Phone = dto.Phone,
                Address = dto.Address,
                Position = dto.Position,
                HireDate = dto.HireDate,
                Salary = dto.Salary,
                DepartmentId = dto.DepartmentId,
                UserId = dto.UserId,
                SupervisorId = dto.SupervisorId
            };

            // Validate supervisor if provided
            if (employee.SupervisorId.HasValue && employee.SupervisorId > 0)
            {
                var supervisorExists = await _context.Employees.AnyAsync(e => e.Id == employee.SupervisorId.Value);
                if (!supervisorExists)
                    return BadRequest(new { message = "Supervisor does not exist" });

                // Prevent self-supervision (only possible after save when Id is assigned, skip for new employees)
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Notify admin about new employee creation
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
            await _notificationService.NotifyEmployeeCreatedAsync(employee.Id, employee.FullName, currentUserEmail);

            return Ok(new
            {
                employee.Id,
                employee.FullName,
                employee.Age,
                employee.Phone,
                employee.Address,
                employee.Position,
                employee.HireDate,
                employee.Salary,
                employee.DepartmentId,
                employee.UserId,
                employee.SupervisorId
            });
        }

        // PUT: api/Employees/5
        [HttpPut("api/Employees/{id}")]
        public async Task<IActionResult> PutEmployee(long id, [FromBody] UpdateEmployeeDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required" });

            var existingEmployee = await _context.Employees.FindAsync(id);
            if (existingEmployee == null)
                return NotFound(new { message = "Employee not found" });

            // Check authorization: Admin/HR can update any employee; employees can only update their own profile
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isOwnProfile = existingEmployee.UserId == currentUserId;

            if (currentUserRole != "Admin" && currentUserRole != "HR" && !isOwnProfile)
                return StatusCode(403, new { message = "You can only update your own profile" });

            if (currentUserRole == "Admin" || currentUserRole == "HR")
            {
                // Admin/HR can update all fields
                if (string.IsNullOrEmpty(dto.FullName))
                    return BadRequest(new { message = "Full name is required" });

                // Validate supervisor if provided
                if (dto.SupervisorId.HasValue && dto.SupervisorId > 0)
                {
                    var supervisorExists = await _context.Employees.AnyAsync(e => e.Id == dto.SupervisorId.Value);
                    if (!supervisorExists)
                        return BadRequest(new { message = "Supervisor does not exist" });

                    // Prevent self-supervision
                    if (dto.SupervisorId.Value == id)
                        return BadRequest(new { message = "Employee cannot be their own supervisor" });

                    // Check for circular reference (prevent supervisor cycles)
                    var isCircular = await CheckCircularReference(id, dto.SupervisorId.Value);
                    if (isCircular)
                        return BadRequest(new { message = "Circular reference detected: supervisor cannot be a subordinate" });
                }

                // Track salary changes for notification
                var oldSalary = existingEmployee.Salary;

                // Update all fields from DTO
                existingEmployee.FullName = dto.FullName;
                existingEmployee.Age = dto.Age;
                existingEmployee.Phone = dto.Phone;
                existingEmployee.Address = dto.Address;
                existingEmployee.Position = dto.Position;
                existingEmployee.HireDate = dto.HireDate;
                existingEmployee.Salary = dto.Salary;
                existingEmployee.DepartmentId = dto.DepartmentId;
                existingEmployee.SupervisorId = dto.SupervisorId;

                // Update email on the associated User record if provided
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    var user = await _context.Users.FindAsync(existingEmployee.UserId);
                    if (user != null && user.Email != dto.Email)
                    {
                        var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != user.Id);
                        if (emailExists)
                            return BadRequest(new { message = "A user with this email already exists" });
                        user.Email = dto.Email;
                    }
                }

                // Update the associated User record ID
                if (dto.UserId.HasValue && dto.UserId > 0)
                {
                    var userAlreadyAssigned = await _context.Employees.AnyAsync(e => e.UserId == dto.UserId.Value && e.Id != id);
                    if (userAlreadyAssigned)
                        return BadRequest(new { message = "User is already assigned to another employee" });
                }
                existingEmployee.UserId = dto.UserId;

                // Notify if salary changed
                if (oldSalary != dto.Salary)
                {
                    await _notificationService.NotifySalaryChangeAsync(
                        existingEmployee.Id, existingEmployee.FullName, oldSalary, dto.Salary, System.DateTime.UtcNow);
                }
            }
            else
            {
                // Employees can only update phone, age, address on their own profile
                existingEmployee.Phone = dto.Phone;
                existingEmployee.Age = dto.Age;
                existingEmployee.Address = dto.Address;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound(new { message = "Employee not found" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Employee updated successfully" });
        }

        // DELETE: api/Employees/5
        [HttpDelete("api/Employees/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteEmployee(long id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee deleted successfully" });
        }

        // GET: api/Employees/by-user/{userId}
        [HttpGet("api/Employees/by-user/{userId}")]
        public async Task<ActionResult<object>> GetEmployeeByUser(long userId)
        {
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Users can only view their own employee record unless they are Admin/HR
            if (currentUserRole != "Admin" && currentUserRole != "HR" && currentUserId != userId)
            {
                return StatusCode(403, new { message = "You can only view your own employee record" });
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            return Ok(new
            {
                employee.Id,
                employee.FullName,
                employee.Age,
                employee.Phone,
                employee.Address,
                employee.Position,
                employee.HireDate,
                employee.Salary,
                employee.DepartmentId,
                Department = employee.Department != null ? new { employee.Department.Id, employee.Department.Name } : null,
                employee.UserId,
                User = employee.User != null ? new { employee.User.Id, employee.User.Email, employee.User.Role } : null,
                employee.SupervisorId,
                Supervisor = employee.Supervisor != null ? new { employee.Supervisor.Id, employee.Supervisor.FullName } : null
            });
        }

        // GET: api/Employees/{id}/subordinates
        [HttpGet("api/Employees/{id}/subordinates")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubordinates(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Subordinates)
                    .ThenInclude(s => s.Department)
                .Include(e => e.Subordinates)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            // Check access: Admin/HR can view any, employees can only view their own subordinates
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null || currentEmployee.Id != id)
                    return StatusCode(403, new { message = "You can only view your own subordinates" });
            }

            var subordinates = employee.Subordinates.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Position,
                s.HireDate,
                s.Salary,
                s.DepartmentId,
                Department = s.Department != null ? new { s.Department.Id, s.Department.Name } : null,
                s.UserId,
                User = s.User != null ? new { s.User.Id, s.User.Email, s.User.Role } : null,
                s.SupervisorId
            }).ToList();

            return Ok(subordinates);
        }

        // GET: api/Employees/{id}/supervisor-chain
        [HttpGet("api/Employees/{id}/supervisor-chain")]
        public async Task<ActionResult<IEnumerable<object>>> GetSupervisorChain(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.Supervisor)
                .ThenInclude(s => s!.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            // Check access: Admin/HR can view any, employees can only view their own chain
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (currentUserRole != "Admin" && currentUserRole != "HR")
            {
                var currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
                if (currentEmployee == null || currentEmployee.Id != id)
                    return StatusCode(403, new { message = "You can only view your own supervisor chain" });
            }

            var chain = new List<object>();
            var current = employee.Supervisor;
            
            while (current != null)
            {
                chain.Add(new
                {
                    current.Id,
                    current.FullName,
                    current.Position,
                    current.SupervisorId,
                    Department = current.Department != null ? new { current.Department.Id, current.Department.Name } : null
                });
                var supervisorId = current.SupervisorId;
                current = null;
                if (supervisorId.HasValue)
                {
                    current = await _context.Employees
                        .Include(e => e.Supervisor)
                        .ThenInclude(s => s!.Department)
                        .FirstOrDefaultAsync(e => e.Id == supervisorId.Value);
                }
            }

            return Ok(chain);
        }

        // GET: api/Employees/with-supervisor
        [HttpGet("api/Employees/with-supervisor")]
        [Authorize(Policy = "AdminOrHR")]
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
                    e.Salary,
                    e.Position,
                    e.HireDate
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

        public class CreateEmployeeDto
        {
            public string FullName { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Position { get; set; } = string.Empty;
            public DateTime HireDate { get; set; } = DateTime.UtcNow;
            public decimal Salary { get; set; }
            public long DepartmentId { get; set; }
            public long? UserId { get; set; }
            public long? SupervisorId { get; set; }
        }

        public class UpdateEmployeeDto
        {
            public string FullName { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Position { get; set; } = string.Empty;
            public DateTime HireDate { get; set; } = DateTime.UtcNow;
            public decimal Salary { get; set; }
            public long DepartmentId { get; set; }
            public long? SupervisorId { get; set; }
            public long? UserId { get; set; }
            public string? Email { get; set; }
        }
    }
}
