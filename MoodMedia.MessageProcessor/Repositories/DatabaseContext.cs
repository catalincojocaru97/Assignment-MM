using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Repositories
{
    /// <summary>
    /// Provides database connections for repositories
    /// </summary>
    public class DatabaseContext
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseContext> _logger;

        /// <summary>
        /// Initializes a new instance of the DatabaseContext class
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        /// <param name="logger">The logger</param>
        public DatabaseContext(IConfiguration configuration, ILogger<DatabaseContext> logger = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger?.LogError("Database connection string is empty or not configured");
                throw new InvalidOperationException("Database connection string is not configured");
            }
        }

        /// <summary>
        /// Creates a new open database connection
        /// </summary>
        /// <returns>An open IDbConnection</returns>
        public IDbConnection CreateConnection()
        {
            try
            {
                _logger?.LogDebug("Creating new database connection");
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                _logger?.LogDebug("Database connection opened successfully");
                return connection;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating database connection: {Message}", ex.Message);
                throw;
            }
        }
    }
} 