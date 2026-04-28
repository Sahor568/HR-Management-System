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
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ManagementContext _context;

        public NotificationsController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Notifications
        [HttpGet]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (userId == 0)
            {
                return Unauthorized();
            }

            var query = _context.Notifications.AsQueryable();

            // Filter based on user role and notification scope
            if (userRole == "Employee")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "Employees");
            }
            else if (userRole == "HR")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "HR" ||
                                        n.NotificationScope == "Employees");
            }
            else if (userRole == "Admin")
            {
                // Admin can see all notifications
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        // GET: api/Notifications/unread
        [HttpGet("unread")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications()
        {
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (userId == 0)
            {
                return Unauthorized();
            }

            var query = _context.Notifications
                .Where(n => !n.IsRead);

            // Filter based on user role and notification scope
            if (userRole == "Employee")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "Employees");
            }
            else if (userRole == "HR")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "HR" ||
                                        n.NotificationScope == "Employees");
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        // GET: api/Notifications/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<Notification>> GetNotification(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this notification
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (!CanViewNotification(notification, userId, userRole))
            {
                return Forbid();
            }

            // Mark as read when retrieved
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return notification;
        }

        // POST: api/Notifications
        [HttpPost]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<Notification>> PostNotification(Notification notification)
        {
            // Set default values
            notification.CreatedAt = DateTime.Now;
            notification.IsRead = false;

            // Validate recipient if specified
            if (notification.RecipientId.HasValue)
            {
                var recipient = await _context.Users.FindAsync(notification.RecipientId.Value);
                if (recipient == null)
                {
                    return BadRequest(new { message = "Recipient not found" });
                }
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNotification", new { id = notification.Id }, notification);
        }

        // POST: api/Notifications/bulk
        [HttpPost("bulk")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult> PostBulkNotifications([FromBody] BulkNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { message = "Message is required" });
            }

            var notifications = new List<Notification>();
            var now = DateTime.Now;

            if (request.RecipientIds != null && request.RecipientIds.Any())
            {
                // Send to specific users
                foreach (var recipientId in request.RecipientIds)
                {
                    var recipient = await _context.Users.FindAsync(recipientId);
                    if (recipient != null)
                    {
                        notifications.Add(new Notification
                        {
                            Title = request.Title,
                            Message = request.Message,
                            NotificationType = request.NotificationType,
                            RecipientId = recipientId,
                            NotificationScope = "Specific",
                            CreatedAt = now,
                            IsRead = false
                        });
                    }
                }
            }
            else if (!string.IsNullOrEmpty(request.NotificationScope))
            {
                // Send to scope (All, Employees, HR, etc.)
                notifications.Add(new Notification
                {
                    Title = request.Title,
                    Message = request.Message,
                    NotificationType = request.NotificationType,
                    NotificationScope = request.NotificationScope,
                    CreatedAt = now,
                    IsRead = false
                });
            }
            else
            {
                return BadRequest(new { message = "Either RecipientIds or NotificationScope must be specified" });
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Created {notifications.Count} notifications",
                Notifications = notifications.Select(n => new { n.Id, n.Title })
            });
        }

        // PUT: api/Notifications/5/read
        [HttpPut("{id}/read")]
        [Authorize(Policy = "AllRoles")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            // Check if user has permission to mark this notification as read
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (!CanViewNotification(notification, userId, userRole))
            {
                return Forbid();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // PUT: api/Notifications/read-all
        [HttpPut("read-all")]
        [Authorize(Policy = "AllRoles")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (userId == 0)
            {
                return Unauthorized();
            }

            var query = _context.Notifications
                .Where(n => !n.IsRead);

            // Filter based on user role and notification scope
            if (userRole == "Employee")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "Employees");
            }
            else if (userRole == "HR")
            {
                query = query.Where(n => n.RecipientId == userId || 
                                        n.NotificationScope == "All" ||
                                        n.NotificationScope == "HR" ||
                                        n.NotificationScope == "Employees");
            }

            var unreadNotifications = await query.ToListAsync();
            var now = DateTime.Now;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Marked {unreadNotifications.Count} notifications as read"
            });
        }

        // DELETE: api/Notifications/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Notifications/stats
        [HttpGet("stats")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult<object>> GetNotificationStats()
        {
            var totalNotifications = await _context.Notifications.CountAsync();
            var unreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);
            
            var byType = await _context.Notifications
                .GroupBy(n => n.NotificationType)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Unread = g.Count(n => !n.IsRead)
                })
                .ToListAsync();

            var byScope = await _context.Notifications
                .GroupBy(n => n.NotificationScope)
                .Select(g => new
                {
                    Scope = g.Key,
                    Count = g.Count(),
                    Unread = g.Count(n => !n.IsRead)
                })
                .ToListAsync();

            var recentActivity = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.NotificationType,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();

            return Ok(new
            {
                Total = totalNotifications,
                Unread = unreadNotifications,
                ReadRate = totalNotifications > 0 ? 
                    ((totalNotifications - unreadNotifications) * 100.0 / totalNotifications).ToString("F1") + "%" : "0%",
                ByType = byType,
                ByScope = byScope,
                RecentActivity = recentActivity
            });
        }

        // POST: api/Notifications/system/leave-approval
        [HttpPost("system/leave-approval")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult> CreateLeaveApprovalNotification([FromBody] LeaveApprovalNotificationRequest request)
        {
            if (request.LeaveId <= 0 || string.IsNullOrEmpty(request.Action))
            {
                return BadRequest(new { message = "LeaveId and Action are required" });
            }

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == request.LeaveId);

            if (leave == null)
            {
                return NotFound(new { message = "Leave request not found" });
            }

            var notification = new Notification
            {
                Title = $"Leave Request {request.Action}",
                Message = $"Your leave request from {leave.FromDate:yyyy-MM-dd} to {leave.ToDate:yyyy-MM-dd} has been {request.Action.ToLower()}.",
                NotificationType = "Leave",
                RecipientId = leave.Employee.UserId,
                CreatedAt = DateTime.Now,
                IsRead = false,
                RelatedEntityId = leave.Id,
                RelatedEntityType = "Leave"
            };

            if (!string.IsNullOrEmpty(request.Comments))
            {
                notification.Message += $" Comments: {request.Comments}";
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Leave approval notification created",
                NotificationId = notification.Id
            });
        }

        // POST: api/Notifications/system/payroll-generated
        [HttpPost("system/payroll-generated")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<ActionResult> CreatePayrollGeneratedNotification([FromBody] PayrollNotificationRequest request)
        {
            if (request.Month <= 0 || request.Month > 12 || request.Year <= 0)
            {
                return BadRequest(new { message = "Valid Month and Year are required" });
            }

            // Get all employees who have payroll for this month
            var employeesWithPayroll = await _context.Payrolls
                .Include(p => p.Employee)
                .Where(p => p.Month == request.Month && p.Year == request.Year)
                .Select(p => p.Employee.UserId)
                .Distinct()
                .ToListAsync();

            var notifications = new List<Notification>();
            var now = DateTime.Now;

            foreach (var userId in employeesWithPayroll)
            {
                notifications.Add(new Notification
                {
                    Title = "Payroll Generated",
                    Message = $"Your payroll for {request.Month}/{request.Year} has been generated. Net salary: [Amount will be shown in payroll details]",
                    NotificationType = "Payroll",
                    RecipientId = userId,
                    CreatedAt = now,
                    IsRead = false,
                    RelatedEntityId = request.Month * 100 + request.Year, // Simple ID for reference
                    RelatedEntityType = "Payroll"
                });
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Created payroll notifications for {notifications.Count} employees",
                Month = request.Month,
                Year = request.Year
            });
        }

        private bool CanViewNotification(Notification notification, long userId, string userRole)
        {
            if (userRole == "Admin")
            {
                return true; // Admin can see all
            }

            if (notification.RecipientId == userId)
            {
                return true; // User is the recipient
            }

            if (notification.NotificationScope == "All")
            {
                return true;
            }

            if (userRole == "HR" && (notification.NotificationScope == "HR" || notification.NotificationScope == "Employees"))
            {
                return true;
            }

            if (userRole == "Employee" && notification.NotificationScope == "Employees")
            {
                return true;
            }

            return false;
        }

        private bool NotificationExists(long id)
        {
            return _context.Notifications.Any(e => e.Id == id);
        }
    }

    public class BulkNotificationRequest
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; } = "Info";
        public List<long> RecipientIds { get; set; }
        public string NotificationScope { get; set; }
    }

    public class LeaveApprovalNotificationRequest
    {
        public long LeaveId { get; set; }
        public string Action { get; set; } // Approved, Rejected
        public string Comments { get; set; }
    }

    public class PayrollNotificationRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

}