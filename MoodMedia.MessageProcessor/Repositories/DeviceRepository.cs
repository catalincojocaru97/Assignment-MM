using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Entities;
using MoodMedia.MessageProcessor.Repositories.Interfaces;

namespace MoodMedia.MessageProcessor.Repositories
{
    /// <summary>
    /// Implementation of IDeviceRepository using Dapper and ADO.NET
    /// </summary>
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<DeviceRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the DeviceRepository class
        /// </summary>
        /// <param name="dbContext">The database context</param>
        /// <param name="logger">The logger</param>
        public DeviceRepository(DatabaseContext dbContext, ILogger<DeviceRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> CreateDeviceAsync(Device device)
        {
            try
            {
                _logger.LogInformation("Creating device with serial number {SerialNumber} for location ID {LocationId}", 
                    device.SerialNumber, device.LocationId);
                
                const string sql = @"
                    INSERT INTO Device (SerialNumber, Type, LocationId)
                    VALUES (@SerialNumber, @Type, @LocationId);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                using (var connection = _dbContext.CreateConnection())
                {
                    var deviceId = await connection.QuerySingleAsync<int>(sql, new
                    {
                        device.SerialNumber,
                        device.Type,
                        device.LocationId
                    });
                    
                    _logger.LogInformation("Device created successfully with ID {DeviceId}", deviceId);
                    return deviceId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteDevicesBySerialNumbersAsync(List<string> serialNumbers)
        {
            try
            {
                if (serialNumbers == null || !serialNumbers.Any())
                {
                    _logger.LogWarning("No serial numbers provided for device deletion");
                    return 0;
                }

                _logger.LogInformation("Deleting {Count} devices by serial numbers", serialNumbers.Count);
                
                // Dynamically create parameters for the IN clause
                var parameters = new DynamicParameters();
                
                for (int i = 0; i < serialNumbers.Count; i++)
                {
                    parameters.Add($"@SerialNumber{i}", serialNumbers[i]);
                    _logger.LogDebug("Added parameter for serial number: {SerialNumber}", serialNumbers[i]);
                }
                
                // Build the SQL query with the IN clause
                var inClause = string.Join(",", serialNumbers.Select((s, i) => $"@SerialNumber{i}"));
                var sql = $"DELETE FROM Device WHERE SerialNumber IN ({inClause})";

                using (var connection = _dbContext.CreateConnection())
                {
                    var deletedCount = await connection.ExecuteAsync(sql, parameters);
                    
                    _logger.LogInformation("Deleted {DeletedCount} devices out of {RequestedCount} requested", 
                        deletedCount, serialNumbers.Count);
                    
                    return deletedCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting devices by serial numbers: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SerialNumberExistsAsync(string serialNumber)
        {
            try
            {
                _logger.LogInformation("Checking if serial number {SerialNumber} exists", serialNumber);
                
                const string sql = "SELECT COUNT(1) FROM Device WHERE SerialNumber = @SerialNumber";

                using (var connection = _dbContext.CreateConnection())
                {
                    var count = await connection.ExecuteScalarAsync<int>(sql, new { SerialNumber = serialNumber });
                    
                    var exists = count > 0;
                    _logger.LogInformation("Serial number {SerialNumber} exists: {Exists}", serialNumber, exists);
                    
                    return exists;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if serial number exists: {Message}", ex.Message);
                throw;
            }
        }
    }
} 