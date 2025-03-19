using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Entities;
using MoodMedia.MessageProcessor.Repositories.Interfaces;

namespace MoodMedia.MessageProcessor.Repositories
{
    /// <summary>
    /// Implementation of ILocationRepository using Dapper and ADO.NET
    /// </summary>
    public class LocationRepository : ILocationRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<LocationRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the LocationRepository class
        /// </summary>
        /// <param name="dbContext">The database context</param>
        /// <param name="logger">The logger</param>
        public LocationRepository(DatabaseContext dbContext, ILogger<LocationRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> CreateLocationAsync(Location location)
        {
            try
            {
                _logger.LogInformation("Creating location with name {LocationName} for company ID {ParentId}", 
                    location.Name, location.ParentId);
                
                const string sql = @"
                    INSERT INTO Location (Name, Address, ParentId)
                    VALUES (@Name, @Address, @ParentId);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                using (var connection = _dbContext.CreateConnection())
                {
                    var locationId = await connection.QuerySingleAsync<int>(sql, new
                    {
                        location.Name,
                        location.Address,
                        location.ParentId
                    });
                    
                    _logger.LogInformation("Location created successfully with ID {LocationId}", locationId);
                    return locationId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location: {Message}", ex.Message);
                throw;
            }
        }
    }
} 