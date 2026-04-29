using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Management.Services;


// The Serilog logging implementation covers the following logs:

// Application lifecycle logs - Startup, shutdown, hosting environment, configuration
// HTTP request/response logs - All HTTP methods, paths, status codes, timing, request/response bodies (limited)
// Database operations - SQL commands, EF Core queries, connection events, concurrency exceptions
// Authentication/Authorization logs - JWT validation, role-based policy evaluations, user authentication
// Controller operation logs - User CRUD operations, business logic, validation, errors, performance metrics
// System infrastructure logs - ASP.NET Core framework events, dependency injection, configuration loading
// Structured data - Timestamps, machine name, thread ID, user context, exception details, performance metrics


namespace Management.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            // Log request details
            var request = context.Request;
            var requestLog = new
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.Value,
                RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
                User = context.User.Identity?.Name ?? "Anonymous"
            };

            _logger.LogInformation("HTTP Request: {@RequestLog}", requestLog);

            // Capture request body for logging (for non-GET requests)
            string requestBody = string.Empty;
            if (request.Method != "GET" && request.ContentLength > 0)
            {
                request.EnableBuffering();
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
                
                if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 1000) // Limit log size
                {
                    _logger.LogDebug("Request Body: {RequestBody}", requestBody);
                }
            }

            // Capture response
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);
                    stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Unhandled exception during request {Method} {Path}", 
                        request.Method, request.Path);
                    throw;
                }

                // Log response details
                var response = context.Response;
                var responseLog = new
                {
                    StatusCode = response.StatusCode,
                    ContentType = response.ContentType,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    Path = request.Path
                };

                var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                              response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

                _logger.Log(logLevel, "HTTP Response: {@ResponseLog}", responseLog);

                // Log response body for errors
                string responseBodyText = string.Empty;
                if (response.StatusCode >= 400)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);
                    
                    if (!string.IsNullOrEmpty(responseBodyText) && responseBodyText.Length < 1000)
                    {
                        _logger.LogDebug("Error Response Body: {ResponseBody}", responseBodyText);
                    }
                }

                // Log to database using LogService
                try
                {
                    var logService = context.RequestServices.GetRequiredService<ILogService>();
                    await logService.LogRequestAsync(
                        request.Method,
                        request.Path,
                        request.QueryString.Value,
                        context.Connection.RemoteIpAddress?.ToString(),
                        context.User.Identity?.Name ?? "Anonymous",
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        requestBody,
                        responseBodyText
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log request to database");
                }

                // Always seek back to beginning before copying response
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}