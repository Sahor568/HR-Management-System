using Management.Models;
using Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Management.Controllers
{
    [Authorize(Policy = "AllRoles")]
    public class NotificationsController : Controller
    {
        private readonly ManagementContext _context;
        private readonly INotificationService _notificationService;

        public NotificationsController(ManagementContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // MVC Action - Display notifications page
        public IActionResult Index()
        {
            return View();
        }

        // API Endpoints

        [HttpGet("api/Notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? unreadOnly, [FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var query = _context.Notifications.AsQueryable();

                // Filter by recipient
                if (user.Role == "Admin" || user.Role == "HR")
                {
                    // Admins and HRs see all notifications
                    query = query.Where(n => n.RecipientId == null || n.RecipientId == userId);
                }
                else
                {
                    // Employees see only their notifications or general notifications
                    query = query.Where(n => n.RecipientId == null || n.RecipientId == userId);
                }

                // Apply filters
                if (unreadOnly.HasValue && unreadOnly.Value)
                {
                    query = query.Where(n => !n.IsRead);
                }

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(n => n.NotificationType == type);
                }

                // Order by creation date (newest first)
                query = query.OrderByDescending(n => n.CreatedAt);

                // Pagination
                var totalCount = await query.CountAsync();
                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.NotificationType,
                        n.NotificationScope,
                        n.CreatedAt,
                        n.IsRead,
                        n.ReadAt,
                        RecipientId = n.RecipientId,
                        RecipientName = n.RecipientId != null ? 
                            _context.Employees.Where(e => e.UserId == n.RecipientId).Select(e => e.FullName).FirstOrDefault() : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching notifications", error = ex.Message });
            }
        }

        [HttpGet("api/Notifications/{id}")]
        public async Task<IActionResult> GetNotification(long id)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var notification = await _context.Notifications.FindAsync(id);

                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                // Check permission
                if (notification.RecipientId != null && notification.RecipientId != userId)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.Role != "Admin" && user?.Role != "HR")
                        return StatusCode(403, new { message = "You can only modify your own notifications" });
                }

                // Mark as read if not already read
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.NotificationType,
                    notification.NotificationScope,
                    notification.CreatedAt,
                    notification.IsRead,
                    notification.ReadAt,
                    RecipientId = notification.RecipientId,
                    RecipientName = notification.RecipientId != null ?
                        _context.Employees.Where(e => e.UserId == notification.RecipientId).Select(e => e.FullName).FirstOrDefault() : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching notification", error = ex.Message });
            }
        }

        [HttpPost("api/Notifications")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var notification = new Notification
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    NotificationType = dto.NotificationType ?? "Info",
                    NotificationScope = dto.NotificationScope ?? "All",
                    RecipientId = dto.RecipientId,
                    RelatedEntityId = dto.RelatedEntityId,
                    RelatedEntityType = dto.RelatedEntityType ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.NotificationType,
                    notification.NotificationScope,
                    notification.CreatedAt,
                    notification.IsRead
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating notification", error = ex.Message });
            }
        }

        [HttpPut("api/Notifications/{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var notification = await _context.Notifications.FindAsync(id);

                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                // Check permission
                if (notification.RecipientId != null && notification.RecipientId != userId)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.Role != "Admin" && user?.Role != "HR")
                        return StatusCode(403, new { message = "You can only modify your own notifications" });
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking notification as read", error = ex.Message });
            }
        }

        [HttpPut("api/Notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _context.Users.FindAsync(userId);
                
                var query = _context.Notifications.Where(n => !n.IsRead);

                if (user?.Role != "Admin" && user?.Role != "HR")
                {
                    // Employees can only mark their own notifications as read
                    query = query.Where(n => n.RecipientId == null || n.RecipientId == userId);
                }

                var notifications = await query.ToListAsync();
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Marked {notifications.Count} notifications as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking notifications as read", error = ex.Message });
            }
        }

        [HttpGet("api/Notifications/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _context.Users.FindAsync(userId);
                
                var query = _context.Notifications.Where(n => !n.IsRead);

                if (user?.Role != "Admin" && user?.Role != "HR")
                {
                    // Employees can only see count of their own notifications
                    query = query.Where(n => n.RecipientId == null || n.RecipientId == userId);
                }

                var count = await query.CountAsync();

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching unread count", error = ex.Message });
            }
        }

        [HttpDelete("api/Notifications/{id}")]
        [Authorize(Policy = "AdminOrHR")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting notification", error = ex.Message });
            }
        }

        // DTO for creating notifications
        public class CreateNotificationDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? NotificationType { get; set; }
            public string? NotificationScope { get; set; }
            public long? RecipientId { get; set; }
            public long? RelatedEntityId { get; set; }
            public string? RelatedEntityType { get; set; }
        }
    }
}
