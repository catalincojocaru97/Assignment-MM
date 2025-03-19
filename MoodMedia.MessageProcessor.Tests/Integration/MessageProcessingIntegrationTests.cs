using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services.Interfaces;
using Xunit;

namespace MoodMedia.MessageProcessor.Tests.Integration
{
    public class MessageProcessingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public MessageProcessingIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Mock repositories for integration tests
                    var mockCompanyRepository = new Mock<ICompanyRepository>();
                    var mockLocationRepository = new Mock<ILocationRepository>();
                    var mockDeviceRepository = new Mock<IDeviceRepository>();
                    
                    // Set up some basic functionality for the mocks
                    mockCompanyRepository.Setup(r => r.GetCompanyByCodeAsync(It.IsAny<string>()))
                        .ReturnsAsync((Models.Entities.Company)null);
                        
                    mockCompanyRepository.Setup(r => r.CreateCompanyAsync(It.IsAny<Models.Entities.Company>()))
                        .ReturnsAsync(1);
                        
                    mockLocationRepository.Setup(r => r.CreateLocationAsync(It.IsAny<Models.Entities.Location>()))
                        .ReturnsAsync(1);
                        
                    mockDeviceRepository.Setup(r => r.CreateDeviceAsync(It.IsAny<Models.Entities.Device>()))
                        .ReturnsAsync(1);
                        
                    mockDeviceRepository.Setup(r => r.SerialNumberExistsAsync(It.IsAny<string>()))
                        .ReturnsAsync(false);
                        
                    mockDeviceRepository.Setup(r => r.DeleteDevicesBySerialNumbersAsync(It.IsAny<System.Collections.Generic.List<string>>()))
                        .ReturnsAsync(1);
                    
                    // Replace the repositories with mocks
                    services.AddSingleton(_ => mockCompanyRepository.Object);
                    services.AddSingleton(_ => mockLocationRepository.Object);
                    services.AddSingleton(_ => mockDeviceRepository.Object);
                    
                    // Use real implementations for the rest of the services
                });
            });
        }
        
        [Fact]
        public async Task ProcessNewCompanyMessage_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", "your-development-api-key-should-be-changed-in-production");
            
            var newCompanyJson = @"{
                ""id"": ""0BA545F1-64C8-487C-988F-1B466A06B30F"",
                ""messageType"": ""NewCompany"",
                ""companyName"": ""Test Company"",
                ""companyCode"": ""TEST123"",
                ""licensing"": ""Standard"",
                ""devices"": [
                    {
                        ""orderNo"": ""ORDER-123"",
                        ""type"": ""Standard"",
                        ""address"": ""123 Test Street""
                    }
                ]
            }";
            
            var content = new StringContent(newCompanyJson, Encoding.UTF8, "application/json");
            
            // Act
            var response = await client.PostAsync("/api/message/process", content);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Should().ContainKey("X-Correlation-ID");
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("success");
        }
        
        [Fact]
        public async Task ProcessDeleteDevicesMessage_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", "your-development-api-key-should-be-changed-in-production");
            
            var deleteDevicesJson = @"{
                ""id"": ""0BA545F1-64C8-487C-988F-1B466A06B30F"",
                ""messageType"": ""DeleteDevices"",
                ""serialNumbers"": [""SN-123"", ""SN-456""]
            }";
            
            var content = new StringContent(deleteDevicesJson, Encoding.UTF8, "application/json");
            
            // Act
            var response = await client.PostAsync("/api/message/process", content);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Should().ContainKey("X-Correlation-ID");
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("success");
        }
        
        [Fact]
        public async Task ProcessMessage_WithInvalidMessageType_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", "your-development-api-key-should-be-changed-in-production");
            
            var invalidMessageJson = @"{
                ""id"": ""0BA545F1-64C8-487C-988F-1B466A06B30F"",
                ""messageType"": ""InvalidType""
            }";
            
            var content = new StringContent(invalidMessageJson, Encoding.UTF8, "application/json");
            
            // Act
            var response = await client.PostAsync("/api/message/process", content);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Headers.Should().ContainKey("X-Correlation-ID");
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("fail");
        }
        
        [Fact]
        public async Task ProcessMessage_WithoutApiKey_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            // No API key header
            
            var validMessageJson = @"{
                ""id"": ""0BA545F1-64C8-487C-988F-1B466A06B30F"",
                ""messageType"": ""NewCompany"",
                ""companyName"": ""Test Company"",
                ""companyCode"": ""TEST123"",
                ""licensing"": ""Standard"",
                ""devices"": []
            }";
            
            var content = new StringContent(validMessageJson, Encoding.UTF8, "application/json");
            
            // Act
            var response = await client.PostAsync("/api/message/process", content);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            // Health check doesn't need API key
            
            // Act
            var response = await client.GetAsync("/api/healthcheck");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("Healthy");
        }
    }
} 