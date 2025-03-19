using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services.Handlers;
using Xunit;

namespace MoodMedia.MessageProcessor.Tests.Services.Handlers
{
    public class DeleteDevicesHandlerTests
    {
        private readonly Mock<IDeviceRepository> _mockDeviceRepository;
        private readonly Mock<ILogger<DeleteDevicesHandler>> _mockLogger;
        private readonly DeleteDevicesHandler _handler;

        public DeleteDevicesHandlerTests()
        {
            _mockDeviceRepository = new Mock<IDeviceRepository>();
            _mockLogger = new Mock<ILogger<DeleteDevicesHandler>>();
            _handler = new DeleteDevicesHandler(_mockDeviceRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidSerialNumbers_ReturnsTrue()
        {
            // Arrange
            var serialNumbers = new List<string> { "SN-123", "SN-456" };
            var message = new DeleteDevicesMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "DeleteDevices",
                SerialNumbers = serialNumbers
            };

            _mockDeviceRepository.Setup(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(serialNumbers.Count);

            // Act
            var result = await _handler.HandleAsync(message);

            // Assert
            result.Should().BeTrue();
            _mockDeviceRepository.Verify(r => r.DeleteDevicesBySerialNumbersAsync(serialNumbers), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithEmptySerialNumbers_ReturnsTrue()
        {
            // Arrange
            var message = new DeleteDevicesMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "DeleteDevices",
                SerialNumbers = new List<string>()
            };

            // Act
            var result = await _handler.HandleAsync(message);

            // Assert
            result.Should().BeTrue();
            _mockDeviceRepository.Verify(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithNullSerialNumbers_ReturnsTrue()
        {
            // Arrange
            var message = new DeleteDevicesMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "DeleteDevices",
                SerialNumbers = null
            };

            // Act
            var result = await _handler.HandleAsync(message);

            // Assert
            result.Should().BeTrue();
            _mockDeviceRepository.Verify(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenRepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var serialNumbers = new List<string> { "SN-123", "SN-456" };
            var message = new DeleteDevicesMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "DeleteDevices",
                SerialNumbers = serialNumbers
            };

            var expectedException = new Exception("Test exception");
            _mockDeviceRepository.Setup(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<List<string>>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _handler.HandleAsync(message);

            // Assert
            result.Should().BeFalse();
            _mockDeviceRepository.Verify(r => r.DeleteDevicesBySerialNumbersAsync(serialNumbers), Times.Once);
            
            // Verify that the exception was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(expectedException.Message)),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var serialNumbers = new List<string> { "SN-123", "SN-456" };
            var message = new DeleteDevicesMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "DeleteDevices",
                SerialNumbers = serialNumbers
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            _mockDeviceRepository.Setup(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<List<string>>()))
                .Callback(() => 
                {
                    // This ensures the cancellation is respected
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                })
                .ReturnsAsync(serialNumbers.Count);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.HandleAsync(message, cancellationTokenSource.Token));
            
            _mockDeviceRepository.Verify(r => r.DeleteDevicesBySerialNumbersAsync(serialNumbers), Times.Once);
        }
    }
} 