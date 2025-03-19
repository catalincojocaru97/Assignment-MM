using System;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MoodMedia.MessageProcessor.Utilities;
using Xunit;

namespace MoodMedia.MessageProcessor.Tests.Utilities
{
    public class TransactionManagerTests
    {
        private readonly Mock<ILogger<TransactionManager>> _mockLogger;
        private readonly TransactionManager _transactionManager;

        public TransactionManagerTests()
        {
            _mockLogger = new Mock<ILogger<TransactionManager>>();
            _transactionManager = new TransactionManager(_mockLogger.Object);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WithSuccessfulOperation_ReturnsResult()
        {
            // Arrange
            const int expectedResult = 42;
            Func<Task<int>> operation = () => Task.FromResult(expectedResult);

            // Act
            var result = await _transactionManager.ExecuteInTransactionAsync(operation);

            // Assert
            result.Should().Be(expectedResult);
            
            // Verify that transaction start and completion were logged
            VerifyLoggerCalled(LogLevel.Debug, "Starting transaction with isolation level", Times.Once());
            VerifyLoggerCalled(LogLevel.Information, "Transaction completed successfully", Times.Once());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WithNoResultOperation_Completes()
        {
            // Arrange
            bool operationExecuted = false;
            Func<Task> operation = () => 
            {
                operationExecuted = true;
                return Task.CompletedTask;
            };

            // Act
            await _transactionManager.ExecuteInTransactionAsync(operation);

            // Assert
            operationExecuted.Should().BeTrue();
            
            // Verify that transaction start and completion were logged
            VerifyLoggerCalled(LogLevel.Debug, "Starting transaction with isolation level", Times.Once());
            VerifyLoggerCalled(LogLevel.Information, "Transaction completed successfully", Times.Once());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WithException_LogsErrorAndRethrows()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            Func<Task<int>> operation = () => throw expectedException;

            // Act & Assert
            var act = async () => await _transactionManager.ExecuteInTransactionAsync(operation);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(expectedException.Message);
            
            // Verify that transaction start and error were logged
            VerifyLoggerCalled(LogLevel.Debug, "Starting transaction with isolation level", Times.Once());
            VerifyLoggerCalled(LogLevel.Error, "Transaction failed", Times.Once());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WithCustomIsolationLevel_UsesSpecifiedLevel()
        {
            // Arrange
            const int expectedResult = 42;
            Func<Task<int>> operation = () => Task.FromResult(expectedResult);
            var isolationLevel = IsolationLevel.Serializable;

            // Act
            var result = await _transactionManager.ExecuteInTransactionAsync(operation, isolationLevel);

            // Assert
            result.Should().Be(expectedResult);
            
            // Verify that transaction with correct isolation level was logged
            VerifyLoggerContains(LogLevel.Debug, isolationLevel.ToString(), Times.Once());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WithNullOperation_ThrowsArgumentNullException()
        {
            // Arrange
            Func<Task<int>> nullOperation = null;

            // Act & Assert
            var act = async () => await _transactionManager.ExecuteInTransactionAsync(nullOperation);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private void VerifyLoggerCalled(LogLevel level, string contains, Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
        
        private void VerifyLoggerContains(LogLevel level, string contains, Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
    }
} 