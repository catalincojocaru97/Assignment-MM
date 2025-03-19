using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services;
using Xunit;

namespace MoodMedia.MessageProcessor.Tests.Services
{
    public class SerialNumberGeneratorTests
    {
        private readonly Mock<IDeviceRepository> _mockDeviceRepository;
        private readonly Mock<ILogger<SerialNumberGenerator>> _mockLogger;
        private readonly SerialNumberGenerator _generator;

        public SerialNumberGeneratorTests()
        {
            _mockDeviceRepository = new Mock<IDeviceRepository>();
            _mockLogger = new Mock<ILogger<SerialNumberGenerator>>();
            _generator = new SerialNumberGenerator(_mockDeviceRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateUniqueSerialNumberAsync_WhenFirstAttemptIsUnique_ReturnsSerialNumber()
        {
            // Arrange
            _mockDeviceRepository.Setup(r => r.SerialNumberExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _generator.GenerateUniqueSerialNumberAsync();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().StartWith("SN-");
            result.Should().MatchRegex(@"^SN-\d+-\d+$");
            
            _mockDeviceRepository.Verify(r => r.SerialNumberExistsAsync(It.IsAny<string>()), Times.Once);
            
            // Verify that success was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully generated unique serial number")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateUniqueSerialNumberAsync_WhenFirstAttemptIsDuplicate_RetryUntilUnique()
        {
            // Arrange - First attempt returns duplicate, second attempt is unique
            _mockDeviceRepository.SetupSequence(r => r.SerialNumberExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true)   // First attempt - exists
                .ReturnsAsync(false); // Second attempt - unique

            // Act
            var result = await _generator.GenerateUniqueSerialNumberAsync();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().StartWith("SN-");
            result.Should().MatchRegex(@"^SN-\d+-\d+$");
            
            // Should have tried twice
            _mockDeviceRepository.Verify(r => r.SerialNumberExistsAsync(It.IsAny<string>()), Times.Exactly(2));
            
            // Verify that a duplicate was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("already exists")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            
            // Verify that success was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully generated unique serial number")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateUniqueSerialNumberAsync_WhenRepositoryThrowsException_HandlesRetries()
        {
            // Arrange - First attempt throws exception, second attempt is successful
            var expectedException = new Exception("Test exception");
            
            _mockDeviceRepository.SetupSequence(r => r.SerialNumberExistsAsync(It.IsAny<string>()))
                .ThrowsAsync(expectedException) // First attempt - exception
                .ReturnsAsync(false);           // Second attempt - success

            // Act
            var result = await _generator.GenerateUniqueSerialNumberAsync();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().StartWith("SN-");
            result.Should().MatchRegex(@"^SN-\d+-\d+$");
            
            // Should have tried twice
            _mockDeviceRepository.Verify(r => r.SerialNumberExistsAsync(It.IsAny<string>()), Times.Exactly(2));
            
            // Verify that the exception was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error checking serial number existence")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            
            // Verify that retry warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("failed to generate unique serial number")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            
            // Verify that success was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Successfully generated unique serial number")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateUniqueSerialNumberAsync_WhenSerialNumberFormatIsValid()
        {
            // Arrange
            _mockDeviceRepository.Setup(r => r.SerialNumberExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _generator.GenerateUniqueSerialNumberAsync();
            
            // Assert
            // Serial number format should be SN-{timestamp}-{random}
            var parts = result.Split('-');
            parts.Length.Should().Be(3);
            parts[0].Should().Be("SN");
            
            // Timestamp part should be a valid number
            long.TryParse(parts[1], out var timestamp).Should().BeTrue();
            timestamp.Should().BeGreaterThan(0);
            
            // Random part should be a valid number between 1000-9999
            int.TryParse(parts[2], out var randomPart).Should().BeTrue();
            randomPart.Should().BeGreaterOrEqual(1000);
            randomPart.Should().BeLessOrEqual(9999);
        }
    }
} 