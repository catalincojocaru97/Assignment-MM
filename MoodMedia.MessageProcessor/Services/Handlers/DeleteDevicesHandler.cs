using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services.Interfaces;

namespace MoodMedia.MessageProcessor.Services.Handlers
{
    /// <summary>
    /// Handles the processing of DeleteDevices messages
    /// </summary>
    public class DeleteDevicesHandler : IMessageHandler<DeleteDevicesMessage>
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<DeleteDevicesHandler> _logger;
        private const int MaxBatchSize = 1000;

        public DeleteDevicesHandler(
            IDeviceRepository deviceRepository,
            ILogger<DeleteDevicesHandler> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> HandleAsync(DeleteDevicesMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                await ValidateMessageAsync(message);

                _logger.LogInformation("Processing DeleteDevices message for {Count} devices", message.SerialNumbers.Count);

                var totalDeleted = 0;
                var batches = message.SerialNumbers
                    .Select((serialNumber, index) => new { serialNumber, index })
                    .GroupBy(x => x.index / MaxBatchSize)
                    .Select(g => g.Select(x => x.serialNumber).ToList());

                foreach (var batch in batches)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Delete operation cancelled after processing {Count} devices", totalDeleted);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var deletedCount = await _deviceRepository.DeleteDevicesBySerialNumbersAsync(batch);
                    totalDeleted += deletedCount;

                    _logger.LogInformation("Deleted {DeletedCount} devices out of {BatchSize} in current batch",
                        deletedCount, batch.Count);
                }

                _logger.LogInformation("Successfully deleted {TotalDeleted} devices out of {RequestedCount}",
                    totalDeleted, message.SerialNumbers.Count);

                return true;
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                _logger.LogError(ex, "Error processing DeleteDevices message: {Message}", ex.Message);
                return false;
            }
        }

        private Task ValidateMessageAsync(DeleteDevicesMessage message)
        {
            if (message.SerialNumbers == null || !message.SerialNumbers.Any())
            {
                throw new ArgumentException("At least one serial number is required", nameof(message));
            }

            if (message.SerialNumbers.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Serial numbers cannot be null or empty", nameof(message));
            }

            if (message.SerialNumbers.Count != message.SerialNumbers.Distinct().Count())
            {
                throw new ArgumentException("Duplicate serial numbers are not allowed", nameof(message));
            }

            return Task.CompletedTask;
        }
    }
} 