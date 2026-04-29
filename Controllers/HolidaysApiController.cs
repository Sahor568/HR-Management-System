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
    [Route("api/Holidays")]
    [ApiController]
    public class HolidaysApiController : ControllerBase
    {
    private readonly ManagementContext _context;

    public HolidaysApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Holidays
    [HttpGet]
            public async Task<ActionResult<IEnumerable<Holiday>>> GetHolidays()
        {
            return await _context.Holidays.ToListAsync();
        }

        // GET: api/Holidays/upcoming
    [HttpGet("upcoming")]
            public async Task<ActionResult<IEnumerable<Holiday>>> GetUpcomingHolidays()
        {
            var today = DateTime.Today;
            return await _context.Holidays
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        // GET: api/Holidays/year/{year}
    [HttpGet("year/{year}")]
            public async Task<ActionResult<IEnumerable<Holiday>>> GetHolidaysByYear(int year)
        {
            return await _context.Holidays
                .Where(h => h.Date.Year == year)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        // GET: api/Holidays/5
    [HttpGet("{id}")]
            public async Task<ActionResult<Holiday>> GetHoliday(long id)
        {
            var holiday = await _context.Holidays.FindAsync(id);

            if (holiday == null)
            {
                return NotFound();
            }

            return holiday;
        }

        // POST: api/Holidays
    [HttpPost]
            public async Task<ActionResult<Holiday>> PostHoliday(Holiday holiday)
        {
            if (string.IsNullOrEmpty(holiday.Name))
                return BadRequest("Holiday name is required");

            if (holiday.Date == default)
                return BadRequest("Date is required");

            // Check if holiday already exists on this date
            var existing = await _context.Holidays
                .FirstOrDefaultAsync(h => h.Date.Date == holiday.Date.Date);
            if (existing != null)
                return BadRequest("Holiday already exists on this date");

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHoliday), new { id = holiday.Id }, holiday);
        }

        // POST: api/Holidays/bulk
    [HttpPost("bulk")]
            public async Task<ActionResult> PostHolidaysBulk(List<Holiday> holidays)
        {
            if (holidays == null || !holidays.Any())
                return BadRequest("No holidays provided");

            var errors = new List<string>();
            var addedCount = 0;

            foreach (var holiday in holidays)
            {
                if (string.IsNullOrEmpty(holiday.Name))
                {
                    errors.Add($"Holiday at index {holidays.IndexOf(holiday)} has no name");
                    continue;
                }

                if (holiday.Date == default)
                {
                    errors.Add($"Holiday '{holiday.Name}' has no date");
                    continue;
                }

                // Check if holiday already exists on this date
                var existing = await _context.Holidays
                    .FirstOrDefaultAsync(h => h.Date.Date == holiday.Date.Date);
                if (existing != null)
                {
                    errors.Add($"Holiday already exists on {holiday.Date:yyyy-MM-dd}");
                    continue;
                }

                _context.Holidays.Add(holiday);
                addedCount++;
            }

            await _context.SaveChangesAsync();

            var result = new
            {
                Added = addedCount,
                Errors = errors,
                TotalProvided = holidays.Count
            };

            if (errors.Any())
                return Ok(result); // Return 200 with errors listed

            return CreatedAtAction(nameof(GetHolidays), result);
        }

        // PUT: api/Holidays/5
    [HttpPut("{id}")]
            public async Task<IActionResult> PutHoliday(long id, Holiday holiday)
        {
            if (id != holiday.Id)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(holiday.Name))
                return BadRequest("Holiday name is required");

            if (holiday.Date == default)
                return BadRequest("Date is required");

            // Check if another holiday already exists on this date (excluding current)
            var existingOnDate = await _context.Holidays
                .FirstOrDefaultAsync(h => h.Date.Date == holiday.Date.Date && h.Id != id);
            if (existingOnDate != null)
                return BadRequest("Another holiday already exists on this date");

            _context.Entry(holiday).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HolidayExists(id))
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

        // DELETE: api/Holidays/5
    [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteHoliday(long id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    private bool HolidayExists(long id)
        {
            return _context.Holidays.Any(e => e.Id == id);
        }
    }
}