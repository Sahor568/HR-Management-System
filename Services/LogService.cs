using Management.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Management.Services
{
    public interface ILogService
    {
        Task LogInformationAsync(string message, string? source = null, string? action = null, 
            string? path = null, string? userName = null, string? ipAddress = null, 
            string? details = null);
        
        Task LogWarningAsync(string message, string? source = null, string? action = null, 
            string? path = null, string? userName = null, string? ipAddress = null, 
            string? details = null);
        
        Task LogErrorAsync(string message, Exception? exception = null, string? source = null, 
            string? action = null, string? path = null, string? userName = null, 
            string? ipAddress = null, string? details = null);
        
        Task LogRequestAsync(string method, string path, string? queryString, string? remoteIp, 
            string? userName, int statusCode, long elapsedMs, string? requestBody = null, 
            string? responseBody = null);
    }

    public class LogService : ILogService
    {
        private readonly ManagementContext _context;
        private readonly ILogger<LogService> _logger;

        public LogService(ManagementContext context, ILogger<LogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogInformationAsync(string message, string? source = null, string? action = null, 
            string? path = null, string? userName = null, string? ipAddress = null, string? details = null)
        {
            await LogToDatabaseAsync("Information", message, source, action, path, userName, ipAddress, details);
            _logger.LogInformation(message);
        }

        public async Task LogWarningAsync(string message, string? source = null, string? action = null, 
            string? path = null, string? userName = null, string? ipAddress = null, string? details = null)
        {
            await LogToDatabaseAsync("Warning", message, source, action, path, userName, ipAddress, details);
            _logger.LogWarning(message);
        }

        public async Task LogErrorAsync(string message, Exception? exception = null, string? source = null, 
            string? action = null, string? path = null, string? userName = null, 
            string? ipAddress = null, string? details = null)
        {
            var fullDetails = details;
            if (exception != null)
            {
                fullDetails = $"Exception: {exception.Message}\nStackTrace: {exception.StackTrace}\nDetails: {details}";
            }
            
            await LogToDatabaseAsync("Error", message, source, action, path, userName, ipAddress, fullDetails);
            _logger.LogError(exception, message);
        }

        public async Task LogRequestAsync(string method, string path, string? queryString, string? remoteIp, 
            string? userName, int statusCode, long elapsedMs, string? requestBody = null, 
            string? responseBody = null)
        {
            var message = $"HTTP {method} {path} - {statusCode} ({elapsedMs}ms)";
            var details = $"Query: {queryString}\n";
            
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 500)
            {
                details += $"Request: {requestBody}\n";
            }
            
            if (!string.IsNullOrEmpty(responseBody) && responseBody.Length < 500 && statusCode >= 400)
            {
                details += $"Response: {responseBody}";
            }

            var logType = statusCode >= 500 ? "Error" : statusCode >= 400 ? "Warning" : "Information";
            
            var log = new SystemLog
            {
                Level = logType,
                Message = message,
                Details = details,
                Source = "RequestLoggingMiddleware",
                Action = method,
                Path = path,
                UserName = userName,
                IpAddress = remoteIp,
                StatusCode = statusCode,
                DurationMs = elapsedMs,
                LogType = "System",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Fallback to file logging if database logging fails
                _logger.LogError(ex, "Failed to save log to database");
            }
        }

        private async Task LogToDatabaseAsync(string level, string message, string? source, 
            string? action, string? path, string? userName, string? ipAddress, string? details)
        {
            var log = new SystemLog
            {
                Level = level,
                Message = message,
                Details = details,
                Source = source,
                Action = action,
                Path = path,
                UserName = userName,
                IpAddress = ipAddress,
                LogType = "System",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Fallback to file logging if database logging fails
                _logger.LogError(ex, "Failed to save log to database");
            }
        }
    }
}