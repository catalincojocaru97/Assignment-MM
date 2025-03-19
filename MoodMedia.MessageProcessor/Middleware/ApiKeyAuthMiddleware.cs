using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Middleware
{
    /// <summary>
    /// Middleware to authenticate requests using API key
    /// </summary>
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthMiddleware> _logger;
        private readonly string _apiKey;
        private const string ApiKeyHeaderName = "X-API-Key";
        private const string ApiKeyQueryParamName = "api_key";
        
        /// <summary>
        /// Initializes a new instance of the ApiKeyAuthMiddleware class
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="configuration">The application configuration</param>
        /// <param name="logger">The logger</param>
        public ApiKeyAuthMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ApiKeyAuthMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _apiKey = configuration["ApiKey"] ?? 
                throw new InvalidOperationException("API Key is not configured in application settings");
            
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("API Key cannot be empty");
            }
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health check endpoint
            if (IsHealthCheckEndpoint(context) || IsSwaggerEndpoint(context))
            {
                await _next(context);
                return;
            }
            
            // Skip authentication for OPTIONS requests (CORS preflight)
            if (context.Request.Method == HttpMethods.Options)
            {
                await _next(context);
                return;
            }
            
            // Check for API key in header or query string
            if (!TryGetApiKey(context, out var apiKey))
            {
                _logger.LogWarning("API key is missing in request");
                await WriteUnauthorizedResponse(context, "API key is missing");
                return;
            }
            
            // Validate API key
            if (!IsValidApiKey(apiKey))
            {
                _logger.LogWarning("Invalid API key provided");
                await WriteUnauthorizedResponse(context, "Invalid API key");
                return;
            }
            
            _logger.LogDebug("API key authentication successful");
            await _next(context);
        }
        
        private static bool IsHealthCheckEndpoint(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            return path.StartsWith("/api/healthcheck", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/healthcheck", StringComparison.OrdinalIgnoreCase);
        }
        
        private static bool IsSwaggerEndpoint(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            return path.StartsWith("/swagger") 
                   || path == "/" 
                   || path == "/index.html" 
                   || path.StartsWith("/api-docs")
                   || path.EndsWith(".js")
                   || path.EndsWith(".css")
                   || path.EndsWith(".png")
                   || path.EndsWith(".ico");
        }
        
        private static bool TryGetApiKey(HttpContext context, out string apiKey)
        {
            // Check header first
            if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValues))
            {
                apiKey = headerValues;
                return !string.IsNullOrEmpty(apiKey);
            }
            
            // Then check query string
            if (context.Request.Query.TryGetValue(ApiKeyQueryParamName, out var queryValues))
            {
                apiKey = queryValues;
                return !string.IsNullOrEmpty(apiKey);
            }
            
            apiKey = string.Empty;
            return false;
        }
        
        private bool IsValidApiKey(string apiKey)
        {
            return string.Equals(_apiKey, apiKey, StringComparison.Ordinal);
        }
        
        private static Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            
            var response = new
            {
                Status = HttpStatusCode.Unauthorized,
                Message = message
            };
            
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
} 