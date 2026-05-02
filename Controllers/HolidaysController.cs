using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Management.Controllers
{
    [Authorize(Policy = "AdminOrHR")]
    public class HolidaysController : Controller
    {
        private readonly ManagementContext _context;

        public HolidaysController(ManagementContext context)
        {
            _context = context;
        }

        [Route("Holidays")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: api/Holidays
        [HttpGet("api/Holidays")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetHolidays([FromQuery] int? year = null)
        {
            var query = _context.Holidays.AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(h => h.Date.Year == year.Value);
            }

            var holidays = await query
                .OrderBy(h => h.Date)
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    h.Date
                })
                .ToListAsync();

            return Ok(holidays);
        }

        // GET: api/Holidays/{id}
        [HttpGet("api/Holidays/{id}")]
        public async Task<ActionResult<object>> GetHoliday(long id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return NotFound(new { message = "Holiday not found" });

            return Ok(new
            {
                holiday.Id,
                holiday.Name,
                holiday.Date
            });
        }

        // POST: api/Holidays
        [HttpPost("api/Holidays")]
        public async Task<ActionResult<object>> CreateHoliday([FromBody] CreateHolidayDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var holiday = new Holiday
                {
                    Name = dto.Name,
                    Date = dto.Date
                };

                _context.Holidays.Add(holiday);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    holiday.Id,
                    holiday.Name,
                    holiday.Date
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating holiday", error = ex.Message });
            }
        }

        // PUT: api/Holidays/{id}
        [HttpPut("api/Holidays/{id}")]
        public async Task<IActionResult> UpdateHoliday(long id, [FromBody] CreateHolidayDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var holiday = await _context.Holidays.FindAsync(id);
                if (holiday == null)
                    return NotFound(new { message = "Holiday not found" });

                holiday.Name = dto.Name;
                holiday.Date = dto.Date;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Holiday updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating holiday", error = ex.Message });
            }
        }

        // DELETE: api/Holidays/{id}
        [HttpDelete("api/Holidays/{id}")]
        public async Task<IActionResult> DeleteHoliday(long id)
        {
            try
            {
                var holiday = await _context.Holidays.FindAsync(id);
                if (holiday == null)
                    return NotFound(new { message = "Holiday not found" });

                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Holiday deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting holiday", error = ex.Message });
            }
        }

        // POST: api/Holidays/bulk
        [HttpPost("api/Holidays/bulk")]
        public async Task<ActionResult<object>> BulkCreateHolidays([FromBody] List<CreateHolidayDto> holidays)
        {
            try
            {
                if (holidays == null || !holidays.Any())
                    return BadRequest(new { message = "No holidays provided" });

                var created = new List<object>();
                foreach (var dto in holidays)
                {
                    var holiday = new Holiday
                    {
                        Name = dto.Name,
                        Date = dto.Date
                    };
                    _context.Holidays.Add(holiday);
                    created.Add(new { dto.Name, dto.Date });
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Created {created.Count} holidays successfully",
                    count = created.Count,
                    holidays = created
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating holidays", error = ex.Message });
            }
        }

        public class CreateHolidayDto
        {
            public string Name { get; set; } = string.Empty;
            public DateTime Date { get; set; }
        }
    }
}
