using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MoodMedia.MessageProcessor.Models.Entities;
using MoodMedia.MessageProcessor.Repositories;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using Xunit;
using FluentAssertions;
using Moq.Dapper;

namespace MoodMedia.MessageProcessor.Tests.Repositories
{
    public class CompanyRepositoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<CompanyRepository>> _mockLogger;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<DatabaseContext> _mockDbContext;
        private readonly ICompanyRepository _repository;

        public CompanyRepositoryTests()
        {
            // Set up mock configuration
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Set up mock logger
            _mockLogger = new Mock<ILogger<CompanyRepository>>();
            
            // Set up mock connection
            _mockConnection = new Mock<IDbConnection>();
            
            // Set up mock database context
            _mockDbContext = new Mock<DatabaseContext>(_mockConfiguration.Object) { CallBase = true };
            _mockDbContext.Setup(x => x.CreateConnection()).Returns(_mockConnection.Object);
            
            // Create repository with mocks
            _repository = new CompanyRepository(_mockConfiguration.Object, _mockLogger.Object);
            
            // Use reflection to replace the database context with our mock
            var field = typeof(CompanyRepository).GetField("_dbContext", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_repository, _mockDbContext.Object);
        }

        [Fact]
        public async Task CreateCompanyAsync_WithValidCompany_ReturnId()
        {
            // Arrange
            var company = new Company
            {
                Name = "Test Company",
                Code = "TEST123",
                Licensing = 1
            };

            const int expectedId = 42;
            
            _mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                It.IsAny<string>(),
                It.Is<object>(p => 
                    p.GetType().GetProperty("Name")?.GetValue(p)?.ToString() == company.Name &&
                    p.GetType().GetProperty("Code")?.GetValue(p)?.ToString() == company.Code &&
                    (int)p.GetType().GetProperty("Licensing")?.GetValue(p) == company.Licensing),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _repository.CreateCompanyAsync(company);

            // Assert
            result.Should().Be(expectedId);
            _mockConnection.VerifyAll();
        }

        [Fact]
        public async Task GetCompanyByCodeAsync_WithExistingCode_ReturnsCompany()
        {
            // Arrange
            const string companyCode = "TEST123";
            var expectedCompany = new Company
            {
                Id = 42,
                Name = "Test Company",
                Code = companyCode,
                Licensing = 1
            };

            _mockConnection.SetupDapperAsync(c => c.QuerySingleOrDefaultAsync<Company>(
                It.IsAny<string>(),
                It.Is<object>(p => p.GetType().GetProperty("Code")?.GetValue(p)?.ToString() == companyCode),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
                .ReturnsAsync(expectedCompany);

            // Act
            var result = await _repository.GetCompanyByCodeAsync(companyCode);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedCompany.Id);
            result.Name.Should().Be(expectedCompany.Name);
            result.Code.Should().Be(expectedCompany.Code);
            result.Licensing.Should().Be(expectedCompany.Licensing);
            _mockConnection.VerifyAll();
        }

        [Fact]
        public async Task GetCompanyByCodeAsync_WithNonExistingCode_ReturnsNull()
        {
            // Arrange
            const string companyCode = "NONEXISTENT";

            _mockConnection.SetupDapperAsync(c => c.QuerySingleOrDefaultAsync<Company>(
                It.IsAny<string>(),
                It.Is<object>(p => p.GetType().GetProperty("Code")?.GetValue(p)?.ToString() == companyCode),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
                .ReturnsAsync((Company)null);

            // Act
            var result = await _repository.GetCompanyByCodeAsync(companyCode);

            // Assert
            result.Should().BeNull();
            _mockConnection.VerifyAll();
        }

        [Fact]
        public async Task CreateCompanyAsync_WithException_LogsErrorAndRethrows()
        {
            // Arrange
            var company = new Company
            {
                Name = "Test Company",
                Code = "TEST123",
                Licensing = 1
            };

            var expectedException = new InvalidOperationException("Test exception");
            
            _mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _repository.CreateCompanyAsync(company));
            
            // Verify that the logger was called with the correct error message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(expectedException.Message)),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
} 