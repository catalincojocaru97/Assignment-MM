using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Middleware
{
    /// <summary>
    /// Middleware to enforce rate limits on API requests
    /// </summary>
    public class ApiRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiRateLimitMiddleware> _logger;
        private readonly int _requestLimit;
        private readonly TimeSpan _timeWindow;
        private readonly ConcurrentDictionary<string, ClientStatistics> _clientStatistics;
        
        private const string ClientIpHeader = "X-Forwarded-For";
        private const string RateLimitRemainingHeader = "X-Rate-Limit-Remaining";
        private const string RateLimitResetHeader = "X-Rate-Limit-Reset";
        private const string RateLimitLimitHeader = "X-Rate-Limit-Limit";

        /// <summary>
        /// Initializes a new instance of the ApiRateLimitMiddleware class
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="configuration">The application configuration</param>
        /// <param name="logger">The logger</param>
        public ApiRateLimitMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ApiRateLimitMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Read configuration values, defaulting if not present
            _requestLimit = configuration.GetValue<int>("RateLimit:RequestLimit", 100);
            var timeWindowSeconds = configuration.GetValue<int>("RateLimit:TimeWindowSeconds", 60);
            _timeWindow = TimeSpan.FromSeconds(timeWindowSeconds);
            
            _clientStatistics = new ConcurrentDictionary<string, ClientStatistics>();
            
            _logger.LogInformation("Rate limit configured: {RequestLimit} requests per {TimeWindow} seconds", 
                _requestLimit, timeWindowSeconds);
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting for health check endpoint
            if (IsHealthCheckEndpoint(context))
            {
                await _next(context);
                return;
            }
            
            // Get client identifier (IP address or API key)
            var clientId = GetClientIdentifier(context);
            
            // Update client statistics and check limit
            var clientStats = _clientStatistics.AddOrUpdate(
                clientId,
                _ => new ClientStatistics(_requestLimit, _timeWindow),
                (_, stats) => 
                {
                    stats.CheckReset();
                    return stats;
                });
            
            // Add rate limit headers to response
            AddRateLimitHeaders(context, clientStats);
            
            // Check if rate limit is exceeded
            if (!clientStats.IncrementRequest())
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                await WriteTooManyRequestsResponse(context, clientStats);
                return;
            }
            
            await _next(context);
        }
        
        private static bool IsHealthCheckEndpoint(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api/healthcheck", StringComparison.OrdinalIgnoreCase);
        }
        
        private static string GetClientIdentifier(HttpContext context)
        {
            // First try to get from forwarded header (for clients behind proxies)
            if (context.Request.Headers.TryGetValue(ClientIpHeader, out var forwardedFor))
            {
                var clientIp = forwardedFor.ToString().Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(clientIp))
                {
                    return clientIp;
                }
            }
            
            // Then try to get from API key header
            if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            {
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return $"api:{apiKey}";
                }
            }
            
            // Fall back to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        
        private static void AddRateLimitHeaders(HttpContext context, ClientStatistics clientStats)
        {
            context.Response.Headers[RateLimitRemainingHeader] = clientStats.RemainingRequests.ToString();
            context.Response.Headers[RateLimitResetHeader] = clientStats.ResetTimeSeconds.ToString();
            context.Response.Headers[RateLimitLimitHeader] = clientStats.RequestLimit.ToString();
        }
        
        private static Task WriteTooManyRequestsResponse(HttpContext context, ClientStatistics clientStats)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            
            var response = new
            {
                Status = HttpStatusCode.TooManyRequests,
                Message = "Rate limit exceeded",
                RetryAfter = clientStats.ResetTimeSeconds
            };
            
            context.Response.Headers["Retry-After"] = clientStats.ResetTimeSeconds.ToString();
            
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        
        /// <summary>
        /// Class to track request statistics for a client
        /// </summary>
        private class ClientStatistics
        {
            private readonly int _requestLimit;
            private readonly TimeSpan _timeWindow;
            private int _requestCount;
            private DateTime _resetTime;
            private readonly object _lock = new object();
            
            public int RemainingRequests => Math.Max(0, _requestLimit - _requestCount);
            public int RequestLimit => _requestLimit;
            public int ResetTimeSeconds => Math.Max(0, (int)(_resetTime - DateTime.UtcNow).TotalSeconds);
            
            public ClientStatistics(int requestLimit, TimeSpan timeWindow)
            {
                _requestLimit = requestLimit;
                _timeWindow = timeWindow;
                _requestCount = 0;
                _resetTime = DateTime.UtcNow.Add(timeWindow);
            }
            
            public void CheckReset()
            {
                lock (_lock)
                {
                    if (DateTime.UtcNow >= _resetTime)
                    {
                        _requestCount = 0;
                        _resetTime = DateTime.UtcNow.Add(_timeWindow);
                    }
                }
            }
            
            public bool IncrementRequest()
            {
                lock (_lock)
                {
                    if (_requestCount >= _requestLimit)
                    {
                        return false;
                    }
                    
                    _requestCount++;
                    return true;
                }
            }
        }
    }
} 