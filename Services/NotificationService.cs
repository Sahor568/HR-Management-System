using Management.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Management.Services
{
    public interface INotificationService
    {
        Task NotifyAdminAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyHRAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyEmployeeAsync(long employeeId, string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyAllAdminsAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyAllHRsAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyAllEmployeesAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null);
        
        Task NotifyLeaveRequestAsync(long leaveId, long employeeId, string employeeName, DateTime startDate, DateTime endDate);
        
        Task NotifyLeaveApprovalAsync(long leaveId, long employeeId, string employeeName, bool isApproved, string? remarks = null);
        
        Task NotifyBonusRequestAsync(long payrollId, long employeeId, string employeeName, decimal amount, string reason);
        
        Task NotifyBonusApprovalAsync(long payrollId, long employeeId, string employeeName, bool isApproved, string? remarks = null);
        
        Task NotifySalaryChangeAsync(long employeeId, string employeeName, decimal oldSalary, decimal newSalary, DateTime effectiveDate);
        
        Task NotifyEmployeeCreatedAsync(long employeeId, string employeeName, string createdBy);
        
        Task NotifyAccountCreatedAsync(long userId, string userName, string role, string createdBy);
        
        Task NotifyAttendanceRecordedAsync(long attendanceId, long employeeId, string employeeName, string status, DateTime date);
        
        Task NotifyCheckInAsync(long employeeId, string employeeName, DateTime checkInTime);
        
        Task NotifyCheckOutAsync(long employeeId, string employeeName, DateTime checkOutTime);
        
        Task MarkAsReadAsync(long notificationId);
        
        Task<int> GetUnreadCountAsync(long? userId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ManagementContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ManagementContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task NotifyAdminAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Find all admin users
            var adminUsers = await _context.Users
                .Where(u => u.Role == "Admin")
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                await CreateNotificationAsync(admin.Id, title, message, notificationType, 
                    "Admin", relatedEntityId, relatedEntityType);
            }
            
            _logger.LogInformation($"Sent admin notification: {title}");
        }

        public async Task NotifyHRAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Find all HR users
            var hrUsers = await _context.Users
                .Where(u => u.Role == "HR")
                .ToListAsync();

            foreach (var hr in hrUsers)
            {
                await CreateNotificationAsync(hr.Id, title, message, notificationType,
                    "HR", relatedEntityId, relatedEntityType);
            }
            
            _logger.LogInformation($"Sent HR notification: {title}");
        }

        public async Task NotifyEmployeeAsync(long employeeId, string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Find employee first, then get their user
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
                
            if (employee?.User != null)
            {
                await CreateNotificationAsync(employee.User.Id, title, message, notificationType,
                    "Specific", relatedEntityId, relatedEntityType);
            }
            
            _logger.LogInformation($"Sent employee notification: {title}");
        }

        public async Task NotifyAllAdminsAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            await NotifyAdminAsync(title, message, notificationType, relatedEntityId, relatedEntityType);
        }

        public async Task NotifyAllHRsAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            await NotifyHRAsync(title, message, notificationType, relatedEntityId, relatedEntityType);
        }

        public async Task NotifyAllEmployeesAsync(string title, string message, string notificationType = "Info",
            long? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Find all employee users
            var employeeUsers = await _context.Users
                .Where(u => u.Role == "Employee")
                .ToListAsync();

            foreach (var employee in employeeUsers)
            {
                await CreateNotificationAsync(employee.Id, title, message, notificationType,
                    "Employees", relatedEntityId, relatedEntityType);
            }
            
            _logger.LogInformation($"Sent all employees notification: {title}");
        }

        public async Task NotifyLeaveRequestAsync(long leaveId, long employeeId, string employeeName, DateTime startDate, DateTime endDate)
        {
            var title = "New Leave Request";
            var message = $"Employee {employeeName} has requested leave from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
            
            await NotifyAdminAsync(title, message, "Leave", leaveId, "Leave");
            
            // Also notify the HR
            await NotifyHRAsync(title, message, "Leave", leaveId, "Leave");
            
            _logger.LogInformation($"Sent leave request notification for leave ID {leaveId}");
        }

        public async Task NotifyLeaveApprovalAsync(long leaveId, long employeeId, string employeeName, bool isApproved, string? remarks = null)
        {
            var status = isApproved ? "approved" : "rejected";
            var title = $"Leave Request {status}";
            var message = $"Your leave request has been {status}";
            if (!string.IsNullOrEmpty(remarks))
            {
                message += $". Remarks: {remarks}";
            }
            
            await NotifyEmployeeAsync(employeeId, title, message, isApproved ? "Success" : "Warning", leaveId, "Leave");
            
            _logger.LogInformation($"Sent leave approval notification for leave ID {leaveId}");
        }

        public async Task NotifyBonusRequestAsync(long payrollId, long employeeId, string employeeName, decimal amount, string reason)
        {
            var title = "New Bonus Request";
            var message = $"Employee {employeeName} has requested a bonus of {amount:C} for: {reason}";
            
            await NotifyAdminAsync(title, message, "Payroll", payrollId, "Payroll");
            
            _logger.LogInformation($"Sent bonus request notification for payroll ID {payrollId}");
        }

        public async Task NotifyBonusApprovalAsync(long payrollId, long employeeId, string employeeName, bool isApproved, string? remarks = null)
        {
            var status = isApproved ? "approved" : "rejected";
            var title = $"Bonus Request {status}";
            var message = $"Your bonus request has been {status}";
            if (!string.IsNullOrEmpty(remarks))
            {
                message += $". Remarks: {remarks}";
            }
            
            await NotifyEmployeeAsync(employeeId, title, message, isApproved ? "Success" : "Warning", payrollId, "Payroll");
            
            _logger.LogInformation($"Sent bonus approval notification for payroll ID {payrollId}");
        }

        public async Task NotifySalaryChangeAsync(long employeeId, string employeeName, decimal oldSalary, decimal newSalary, DateTime effectiveDate)
        {
            var title = "Salary Change";
            var message = $"Salary for {employeeName} has been changed from {oldSalary:C} to {newSalary:C} effective {effectiveDate:yyyy-MM-dd}";
            
            await NotifyAdminAsync(title, message, "Payroll", employeeId, "Employee");
            
            // Notify the employee
            await NotifyEmployeeAsync(employeeId, title, $"Your salary has been updated to {newSalary:C} effective {effectiveDate:yyyy-MM-dd}", 
                "Info", employeeId, "Employee");
            
            _logger.LogInformation($"Sent salary change notification for employee ID {employeeId}");
        }

        public async Task NotifyEmployeeCreatedAsync(long employeeId, string employeeName, string createdBy)
        {
            var title = "New Employee Created";
            var message = $"Employee {employeeName} has been created by {createdBy}";
            
            await NotifyAdminAsync(title, message, "Info", employeeId, "Employee");
            
            _logger.LogInformation($"Sent employee created notification for employee ID {employeeId}");
        }

        public async Task NotifyAccountCreatedAsync(long userId, string userName, string role, string createdBy)
        {
            var title = "New Account Created";
            var message = $"Account for {userName} with role {role} has been created by {createdBy}";
            
            await NotifyAdminAsync(title, message, "Info", userId, "User");
            
            _logger.LogInformation($"Sent account created notification for user ID {userId}");
        }

        public async Task NotifyAttendanceRecordedAsync(long attendanceId, long employeeId, string employeeName, string status, DateTime date)
        {
            var title = "Attendance Recorded";
            var message = $"Attendance for {employeeName} on {date:yyyy-MM-dd} has been recorded as {status}";
            
            await NotifyAdminAsync(title, message, "Attendance", attendanceId, "Attendance");
            
            _logger.LogInformation($"Sent attendance recorded notification for employee ID {employeeId}");
        }

        public async Task NotifyCheckInAsync(long employeeId, string employeeName, DateTime checkInTime)
        {
            var title = "Employee Check-In";
            var message = $"{employeeName} checked in at {checkInTime:HH:mm:ss}";
            
            await NotifyAdminAsync(title, message, "Attendance", employeeId, "Attendance");
            
            _logger.LogInformation($"Sent check-in notification for employee ID {employeeId}");
        }

        public async Task NotifyCheckOutAsync(long employeeId, string employeeName, DateTime checkOutTime)
        {
            var title = "Employee Check-Out";
            var message = $"{employeeName} checked out at {checkOutTime:HH:mm:ss}";
            
            await NotifyAdminAsync(title, message, "Attendance", employeeId, "Attendance");
            
            _logger.LogInformation($"Sent check-out notification for employee ID {employeeId}");
        }

        public async Task MarkAsReadAsync(long notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(long? userId = null)
        {
            var query = _context.Notifications.Where(n => !n.IsRead);
            
            if (userId.HasValue)
            {
                query = query.Where(n => n.RecipientId == userId);
            }
            
            return await query.CountAsync();
        }

        private async Task CreateNotificationAsync(long recipientId, string title, string message,
            string notificationType, string scope, long? relatedEntityId, string? relatedEntityType)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                NotificationType = notificationType,
                RecipientId = recipientId,
                NotificationScope = scope,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType ?? string.Empty
            };

            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification");
            }
        }
    }
}