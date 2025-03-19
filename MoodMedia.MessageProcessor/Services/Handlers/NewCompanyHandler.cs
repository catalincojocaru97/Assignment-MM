using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Entities;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services.Interfaces;

namespace MoodMedia.MessageProcessor.Services.Handlers
{
    /// <summary>
    /// Handles the processing of NewCompany messages
    /// </summary>
    public class NewCompanyHandler : IMessageHandler<NewCompanyMessage>
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ISerialNumberGenerator _serialNumberGenerator;
        private readonly ILogger<NewCompanyHandler> _logger;

        public NewCompanyHandler(
            ICompanyRepository companyRepository,
            ILocationRepository locationRepository,
            IDeviceRepository deviceRepository,
            ISerialNumberGenerator serialNumberGenerator,
            ILogger<NewCompanyHandler> logger)
        {
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> HandleAsync(NewCompanyMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                await ValidateMessageAsync(message);

                using var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                    TransactionScopeAsyncFlowOption.Enabled);

                _logger.LogInformation("Processing NewCompany message for company {CompanyName} with code {CompanyCode}",
                    message.CompanyName, message.CompanyCode);

                var company = await CreateCompanyAsync(message, cancellationToken);
                await ProcessDevicesAsync(message, company.Id, cancellationToken);

                scope.Complete();
                _logger.LogInformation("Successfully processed NewCompany message for company {CompanyName}", message.CompanyName);
                return true;
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                _logger.LogError(ex, "Error processing NewCompany message: {Message}", ex.Message);
                return false;
            }
        }

        private async Task ValidateMessageAsync(NewCompanyMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.CompanyName))
            {
                throw new ArgumentException("Company name is required", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.CompanyCode))
            {
                throw new ArgumentException("Company code is required", nameof(message));
            }

            if (message.Devices == null || message.Devices.Count == 0)
            {
                throw new ArgumentException("At least one device is required", nameof(message));
            }

            var existingCompany = await _companyRepository.GetCompanyByCodeAsync(message.CompanyCode);
            if (existingCompany != null)
            {
                throw new InvalidOperationException($"Company with code {message.CompanyCode} already exists");
            }
        }

        private async Task<Company> CreateCompanyAsync(NewCompanyMessage message, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<LicensingType>(message.Licensing, true, out var licensingType))
            {
                throw new ArgumentException($"Invalid licensing type: {message.Licensing}", nameof(message));
            }

            var company = new Company
            {
                Name = message.CompanyName,
                Code = message.CompanyCode,
                Licensing = (int)licensingType
            };

            company.Id = await _companyRepository.CreateCompanyAsync(company);
            _logger.LogInformation("Created company {CompanyName} with ID {CompanyId}", company.Name, company.Id);

            return company;
        }

        private async Task ProcessDevicesAsync(NewCompanyMessage message, int companyId, CancellationToken cancellationToken)
        {
            for (int i = 0; i < message.Devices.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Processing cancelled after {ProcessedCount} devices", i);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var deviceInfo = message.Devices[i];
                await ProcessSingleDeviceAsync(deviceInfo, companyId, i + 1);
            }
        }

        private async Task ProcessSingleDeviceAsync(DeviceInfo deviceInfo, int companyId, int locationIndex)
        {
            if (!Enum.TryParse<DeviceType>(deviceInfo.Type, true, out var deviceType))
            {
                throw new ArgumentException($"Invalid device type: {deviceInfo.Type}");
            }

            var location = new Location
            {
                Name = $"Location {locationIndex}",
                Address = deviceInfo.Address ?? "No address provided",
                ParentId = companyId
            };

            location.Id = await _locationRepository.CreateLocationAsync(location);
            _logger.LogInformation("Created location {LocationName} with ID {LocationId} for company {CompanyId}",
                location.Name, location.Id, companyId);

            var serialNumber = await _serialNumberGenerator.GenerateUniqueSerialNumberAsync();
            var device = new Device
            {
                SerialNumber = serialNumber,
                Type = (int)deviceType,
                LocationId = location.Id
            };

            device.Id = await _deviceRepository.CreateDeviceAsync(device);
            _logger.LogInformation(
                "Created device with ID {DeviceId} and serial number {SerialNumber} at location {LocationId}",
                device.Id, device.SerialNumber, location.Id);
        }
    }

    public enum LicensingType
    {
        Standard = 1,
        Premium = 2,
        Enterprise = 3
    }

    public enum DeviceType
    {
        Standard = 1,
        Custom = 2
    }
} 