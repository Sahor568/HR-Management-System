using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly ManagementContext _context;

        public DepartmentsController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Departments
        [HttpGet]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            return await _context.Departments.Include(d => d.Employees).ToListAsync();
        }

        // GET: api/Departments/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<Department>> GetDepartment(long id)
        {
            var department = await _context.Departments.Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound();
            }

            return department;
        }

        // POST: api/Departments
        [HttpPost]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<Department>> PostDepartment(Department department)
        {
            if (string.IsNullOrEmpty(department.Name))
                return BadRequest("Department name is required");

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
        }

        // PUT: api/Departments/5
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> PutDepartment(long id, Department department)
        {
            if (id != department.Id)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(department.Name))
                return BadRequest("Department name is required");

            _context.Entry(department).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
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

        // DELETE: api/Departments/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            // Check if department has employees
            var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
            if (hasEmployees)
            {
                return BadRequest("Cannot delete department that has employees. Reassign or remove employees first.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DepartmentExists(long id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}