using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Management.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Controllers
{
    [Authorize(Policy = "MainAdminOnly")]
    public class LogsController : Controller
    {
    private readonly ManagementContext _context;

    public LogsController(ManagementContext context)
        {
            _context = context;
        }

        // GET: Logs
    public async Task<IActionResult> Index(
            string? level = null,
            string? source = null,
            string? userName = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? logType = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.SystemLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(level) && level != "All")
            {
                query = query.Where(l => l.Level == level);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(l => l.Source != null && l.Source.Contains(source));
            }

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName != null && l.UserName.Contains(userName));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => 
                    l.Message.Contains(search) || 
                    (l.Details != null && l.Details.Contains(search)) ||
                    (l.Path != null && l.Path.Contains(search))
                );
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= fromDate.Value.ToUniversalTime());
            }

            if (toDate.HasValue)
            {
                var toDateEnd = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt <= toDateEnd.ToUniversalTime());
            }

            if (!string.IsNullOrEmpty(logType) && logType != "All")
            {
                query = query.Where(l => l.LogType == logType);
            }

            // Order by most recent first
            query = query.OrderByDescending(l => l.CreatedAt);

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Prepare view model
            // Get distinct levels and log types from database
            var distinctLevels = await _context.SystemLogs
                .Select(l => l.Level)
                .Distinct()
                .ToListAsync();
            
            var distinctLogTypes = await _context.SystemLogs
                .Select(l => l.LogType)
                .Where(lt => lt != null)
                .Distinct()
                .ToListAsync();

            // Add "All" option at the beginning
            var allLevels = new List<string> { "All" };
            allLevels.AddRange(distinctLevels.Where(l => !string.IsNullOrEmpty(l)));
            
            var allLogTypes = new List<string> { "All" };
            allLogTypes.AddRange(distinctLogTypes.Where(lt => !string.IsNullOrEmpty(lt))!);

            var viewModel = new LogsViewModel
            {
                Logs = logs,
                LevelFilter = level,
                SourceFilter = source,
                UserNameFilter = userName,
                SearchFilter = search,
                FromDateFilter = fromDate,
                ToDateFilter = toDate,
                LogTypeFilter = logType,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Levels = allLevels.ToArray(),
                LogTypes = allLogTypes.ToArray()
            };

            return View(viewModel);
        }

        // GET: Logs/Details/5
    public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemLog = await _context.SystemLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemLog == null)
            {
                return NotFound();
            }

            return View(systemLog);
        }

        // GET: Logs/ClearOld
    public IActionResult ClearOld()
        {
            return View();
        }

        // POST: Logs/ClearOld
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearOld(int daysToKeep)
        {
            if (daysToKeep < 1 || daysToKeep > 365)
            {
                ModelState.AddModelError("daysToKeep", "Days to keep must be between 1 and 365.");
                return View();
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = _context.SystemLogs.Where(l => l.CreatedAt < cutoffDate);

            var count = await oldLogs.CountAsync();
            _context.SystemLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully cleared {count} logs older than {daysToKeep} days.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Logs/Export
    public async Task<IActionResult> Export(
            string? level = null,
            string? source = null,
            string? userName = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level) && level != "All")
            {
                query = query.Where(l => l.Level == level);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(l => l.Source != null && l.Source.Contains(source));
            }

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName != null && l.UserName.Contains(userName));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= fromDate.Value.ToUniversalTime());
            }

            if (toDate.HasValue)
            {
                var toDateEnd = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt <= toDateEnd.ToUniversalTime());
            }

            query = query.OrderByDescending(l => l.CreatedAt);

            var logs = await query.ToListAsync();

            // Generate CSV
            var csv = "Id,Level,Message,Source,Action,Path,UserName,IpAddress,StatusCode,DurationMs,LogType,CreatedAt\n";
            foreach (var log in logs)
            {
                csv += $"\"{log.Id}\",\"{log.Level}\",\"{EscapeCsv(log.Message)}\",\"{EscapeCsv(log.Source)}\",\"{EscapeCsv(log.Action)}\",\"{EscapeCsv(log.Path)}\",\"{EscapeCsv(log.UserName)}\",\"{EscapeCsv(log.IpAddress)}\",\"{log.StatusCode}\",\"{log.DurationMs}\",\"{EscapeCsv(log.LogType)}\",\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\"\n";
            }

            var fileName = $"logs_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

    private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        // POST: Logs/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
            public async Task<IActionResult> Delete(long id)
        {
            var systemLog = await _context.SystemLogs.FindAsync(id);
            if (systemLog == null)
            {
                return NotFound();
            }

            _context.SystemLogs.Remove(systemLog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Log #{id} has been deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Logs/GetStatistics
    [HttpGet]
    public async Task<IActionResult> GetStatistics()
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var logsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today)
                .CountAsync();

            var logsThisWeek = await _context.SystemLogs
                .Where(l => l.CreatedAt >= weekStart)
                .CountAsync();

            var errorsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today && l.Level == "Error")
                .CountAsync();

            var warningsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today && l.Level == "Warning")
                .CountAsync();

            return Json(new
            {
                logsToday,
                logsThisWeek,
                errorsToday,
                warningsToday
            });
        }

        // GET: Logs/GetLogStats
    [HttpGet]
    public async Task<IActionResult> GetLogStats(int days = 30)
        {
            if (days < 1 || days > 365)
            {
                return BadRequest("Days must be between 1 and 365");
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            var totalLogs = await _context.SystemLogs.CountAsync();
            var logsToKeep = await _context.SystemLogs
                .Where(l => l.CreatedAt >= cutoffDate)
                .CountAsync();
            var logsToDelete = totalLogs - logsToKeep;

            var oldestLog = await _context.SystemLogs
                .OrderBy(l => l.CreatedAt)
                .Select(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            return Json(new
            {
                totalLogs,
                logsToKeep,
                logsToDelete,
                oldestLogDate = oldestLog > DateTime.MinValue ? oldestLog : (DateTime?)null
            });
        }
        // API Endpoints

        // GET: api/Logs
        [HttpGet("api/Logs")]
        public async Task<ActionResult<object>> GetLogsApi(
            [FromQuery] string? level = null,
            [FromQuery] string? source = null,
            [FromQuery] string? userName = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? logType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level) && level != "All")
                query = query.Where(l => l.Level == level);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(l => l.Source != null && l.Source.Contains(source));

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(l => l.UserName != null && l.UserName.Contains(userName));

            if (!string.IsNullOrEmpty(search))
                query = query.Where(l =>
                    l.Message.Contains(search) ||
                    (l.Details != null && l.Details.Contains(search)) ||
                    (l.Path != null && l.Path.Contains(search)));

            if (fromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
            {
                var toDateEnd = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt <= toDateEnd.ToUniversalTime());
            }

            if (!string.IsNullOrEmpty(logType) && logType != "All")
                query = query.Where(l => l.LogType == logType);

            query = query.OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Level,
                    l.Message,
                    l.Details,
                    l.Source,
                    l.Action,
                    l.Path,
                    l.UserName,
                    l.IpAddress,
                    l.StatusCode,
                    l.DurationMs,
                    l.LogType,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                logs
            });
        }

        // GET: api/Logs/GetStatistics
        [HttpGet("api/Logs/GetStatistics")]
        public async Task<ActionResult<object>> GetStatisticsApi()
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var logsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today)
                .CountAsync();

            var logsThisWeek = await _context.SystemLogs
                .Where(l => l.CreatedAt >= weekStart)
                .CountAsync();

            var errorsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today && l.Level == "Error")
                .CountAsync();

            var warningsToday = await _context.SystemLogs
                .Where(l => l.CreatedAt >= today && l.Level == "Warning")
                .CountAsync();

            return Ok(new
            {
                logsToday,
                logsThisWeek,
                errorsToday,
                warningsToday
            });
        }
    }

    public class LogsViewModel
    {
    public List<SystemLog> Logs { get; set; } = new List<SystemLog>();
    public string? LevelFilter { get; set; }
    public string? SourceFilter { get; set; }
    public string? UserNameFilter { get; set; }
    public string? SearchFilter { get; set; }
    public DateTime? FromDateFilter { get; set; }
    public DateTime? ToDateFilter { get; set; }
    public string? LogTypeFilter { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string[] Levels { get; set; } = Array.Empty<string>();
    public string[] LogTypes { get; set; } = Array.Empty<string>();
    }
}
