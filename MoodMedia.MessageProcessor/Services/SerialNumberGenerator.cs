using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services.Interfaces;
using Polly;
using Polly.Retry;

namespace MoodMedia.MessageProcessor.Services
{
    /// <summary>
    /// Implementation of ISerialNumberGenerator that generates unique serial numbers for devices
    /// </summary>
    public class SerialNumberGenerator : ISerialNumberGenerator
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<SerialNumberGenerator> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly Random _random;
        private const int MaxRetries = 3;
        private const int MinRandomValue = 1000;
        private const int MaxRandomValue = 9999;

        public SerialNumberGenerator(
            IDeviceRepository deviceRepository,
            ILogger<SerialNumberGenerator> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _random = new Random();
            
            // Configure retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Attempt {RetryCount} of {MaxRetries} failed to generate unique serial number. Waiting {DelayMs}ms before next attempt",
                            retryCount,
                            MaxRetries,
                            timeSpan.TotalMilliseconds);
                    });
        }

        /// <inheritdoc/>
        public async Task<string> GenerateUniqueSerialNumberAsync()
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var serialNumber = await GenerateSerialNumberWithUniqueCheckAsync();
                
                if (string.IsNullOrEmpty(serialNumber))
                {
                    throw new InvalidOperationException("Failed to generate a unique serial number after maximum attempts");
                }
                
                _logger.LogInformation("Successfully generated unique serial number: {SerialNumber}", serialNumber);
                return serialNumber;
            });
        }

        private async Task<string> GenerateSerialNumberWithUniqueCheckAsync()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomPart = _random.Next(MinRandomValue, MaxRandomValue);
            var candidateSerialNumber = $"SN-{timestamp}-{randomPart}";

            try
            {
                var exists = await _deviceRepository.SerialNumberExistsAsync(candidateSerialNumber);
                
                if (!exists)
                {
                    return candidateSerialNumber;
                }
                
                _logger.LogDebug("Generated serial number already exists: {SerialNumber}", candidateSerialNumber);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking serial number existence: {SerialNumber}", candidateSerialNumber);
                throw;
            }
        }
    }
} 