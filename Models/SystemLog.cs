using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models
{
    public class SystemLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = "Information"; // Information, Warning, Error, Debug

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(100)]
        public string? Source { get; set; } // Controller name, Service name, etc.

        [MaxLength(50)]
        public string? Action { get; set; } // HTTP Method, Action name

        [MaxLength(200)]
        public string? Path { get; set; } // URL path

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public int? StatusCode { get; set; }

        public long? DurationMs { get; set; } // Request duration in milliseconds

        [MaxLength(50)]
        public string? LogType { get; set; } = "System"; // System, Audit, Security, Performance

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties for UI
        [NotMapped]
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        [NotMapped]
        public string LevelClass => Level switch
        {
            "Error" => "text-danger",
            "Warning" => "text-warning",
            "Information" => "text-info",
            "Debug" => "text-muted",
            _ => "text-secondary"
        };
    }
}