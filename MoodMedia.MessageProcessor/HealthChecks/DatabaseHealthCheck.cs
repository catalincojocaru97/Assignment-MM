using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using MoodMedia.MessageProcessor.Repositories;

namespace MoodMedia.MessageProcessor.HealthChecks
{
    /// <summary>
    /// Health check that verifies database connectivity
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<DatabaseHealthCheck> _logger;
        private const string HealthCheckQuery = "SELECT 1";

        /// <summary>
        /// Initializes a new instance of the DatabaseHealthCheck class
        /// </summary>
        /// <param name="dbContext">Database context</param>
        /// <param name="logger">Logger</param>
        public DatabaseHealthCheck(
            DatabaseContext dbContext,
            ILogger<DatabaseHealthCheck> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks the health of the database connection
        /// </summary>
        /// <param name="context">Health check context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Executing database health check");
                
                using var connection = _dbContext.CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = HealthCheckQuery;
                
                // Execute synchronously - IDbCommand doesn't have async methods
                var result = command.ExecuteScalar();
                
                _logger.LogInformation("Database connectivity health check succeeded");

                return Task.FromResult(HealthCheckResult.Healthy("Database connection is healthy"));
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database connectivity health check failed");
                
                var data = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ErrorCode"] = ex.Number,
                    ["ServerName"] = ex.Server
                };
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    ex,
                    data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during database health check");
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    ex));
            }
        }
    }
} 