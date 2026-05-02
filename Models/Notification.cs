using System;
using System.ComponentModel.DataAnnotations;

namespace Management.Models
{
    public class Notification
    {
        public long Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; } = "Info"; // Info, Warning, Success, Error, Leave, Payroll, Attendance
        
        public long? RecipientId { get; set; }
        
        [StringLength(50)]
        public string NotificationScope { get; set; } = "All"; // All, Employees, HR, Admin, Specific
        
        public DateTime CreatedAt { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        public long? RelatedEntityId { get; set; }
        
        [StringLength(50)]
        public string RelatedEntityType { get; set; } = string.Empty; // Leave, Payroll, Attendance, Employee, User
        
        // Navigation property
        public User? Recipient { get; set; }
    }
}