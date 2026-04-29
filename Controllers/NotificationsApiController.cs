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
    [Route("api/Notifications")]
    [ApiController]
    public class NotificationsApiController : ControllerBase
    {
        private readonly ManagementContext _context;

        public NotificationsApiController(ManagementContext context)
        {
            _context = context;
        }

        // GET: api/Notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            if (userId == 0)
            {
                return Unauthorized();
            }

            IQueryable<Notification> query = _context.Notifications;

            // HR and Admin can see all notifications
            if (userRole != "Admin" && userRole != "HR")
            {
                query = query.Where(n => n.RecipientId == userId);
            }

            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        // GET: api/Notifications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetNotification(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            var userRole = User.FindFirst("role")?.Value;

            // Check permission
            if (userRole != "Admin" && userRole != "HR" && notification.RecipientId != userId)
            {
                return Forbid();
            }

            return notification;
        }

        // POST: api/Notifications
        [HttpPost]
        public async Task<ActionResult<Notification>> PostNotification(Notification notification)
        {
            notification.CreatedAt = DateTime.Now;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNotification", new { id = notification.Id }, notification);
        }

        // PUT: api/Notifications/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNotification(long id, Notification notification)
        {
            if (id != notification.Id)
            {
                return BadRequest();
            }

            _context.Entry(notification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationExists(id))
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

        // DELETE: api/Notifications/5
        [HttpDelete("{id}")]
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

        // PATCH: api/Notifications/5/mark-read
        [HttpPatch("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var count = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .CountAsync();

            return count;
        }

        private bool NotificationExists(long id)
        {
            return _context.Notifications.Any(e => e.Id == id);
        }
    }
}