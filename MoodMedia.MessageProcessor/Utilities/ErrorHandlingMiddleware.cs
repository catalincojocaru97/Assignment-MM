using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Utilities
{
    /// <summary>
    /// Middleware to handle exceptions globally across the application
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Initializes a new instance of the ErrorHandlingMiddleware class
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="logger">The logger</param>
        /// <param name="environment">The host environment</param>
        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string? correlationId = null;
            
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationValues))
            {
                correlationId = correlationValues;
            }
            
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers.Add("X-Correlation-ID", correlationId);
            }
            
            context.Response.Headers.Add("X-Correlation-ID", correlationId);

            using var scope = _logger.BeginScope(new { CorrelationId = correlationId });
            
            try
            {
                await _next(context);
                
                // If we have an error status code but no exception was thrown, still log it
                if (context.Response.StatusCode >= 400)
                {
                    var endpoint = context.GetEndpoint()?.DisplayName ?? "Unknown";
                    _logger.LogWarning("Request {Method} {Path} resulted in status code {StatusCode} at {Endpoint}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            var statusCode = GetStatusCode(exception);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                Status = statusCode,
                Success = false,
                Message = GetMessage(exception, statusCode),
                CorrelationId = correlationId,
                Detail = _environment.IsDevelopment() ? exception.ToString() : null
            };

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            
            return context.Response.WriteAsync(json);
        }

        private static HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentException _ => HttpStatusCode.BadRequest,
                InvalidOperationException _ => HttpStatusCode.BadRequest,
                UnauthorizedAccessException _ => HttpStatusCode.Unauthorized,
                FileNotFoundException _ => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private static string GetMessage(Exception exception, HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.InternalServerError 
                ? "An unexpected error occurred" 
                : exception.Message;
        }
    }
} 