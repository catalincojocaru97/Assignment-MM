using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Controllers
{
    /// <summary>
    /// Controller for health checks and application status
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthCheckController> _logger;

        /// <summary>
        /// Initializes a new instance of the HealthCheckController
        /// </summary>
        /// <param name="healthCheckService">The health check service</param>
        /// <param name="logger">The logger</param>
        public HealthCheckController(
            HealthCheckService healthCheckService,
            ILogger<HealthCheckController> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the health status of the application
        /// </summary>
        /// <returns>Health status of the application and its dependencies</returns>
        /// <response code="200">The application is healthy</response>
        /// <response code="503">The application is degraded or unhealthy</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get()
        {
            var report = await _healthCheckService.CheckHealthAsync();
            
            _logger.LogInformation("Health check executed with status: {Status}", report.Status);

            return report.Status == HealthStatus.Healthy
                ? Ok(new { Status = "Healthy", Details = report.Entries })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    new { Status = report.Status.ToString(), Details = report.Entries });
        }
    }
} 