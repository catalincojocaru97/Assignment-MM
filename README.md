# Mood Media Message Processor

A .NET web application for processing company and device messages.

## Overview

This application processes two types of messages:
- `NewCompany`: Creates a company record with associated locations and devices
- `DeleteDevices`: Removes device records based on their serial numbers

## Technical Stack

- .NET 8.0
- ASP.NET Core Web API
- Dapper for data access
- Polly for resilience
- OpenTelemetry for observability
- xUnit, Moq, and FluentAssertions for testing

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or higher)
- Visual Studio 2022 or VS Code

## Getting Started

### Database Setup

1. Open SQL Server Management Studio or another SQL tool
2. Run the `MoodMedia.MessageProcessor/DatabaseSetup.sql` script to create the database

### Configuration

The connection string and other settings can be configured in the `appsettings.json` file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MoodMedia;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "ApiKey": "your-api-key-here",
  "RateLimit": {
    "RequestLimit": 100,
    "TimeWindowSeconds": 60
  }
}
```

### Running the Application

#### Using Visual Studio
1. Open the solution in Visual Studio 2022
2. Set `MoodMedia.MessageProcessor` as the startup project
3. Press F5 to run the application

#### Using Command Line
```bash
# Navigate to the project directory
cd MoodMedia.MessageProcessor

# Run the application
dotnet run
```

The API will be available at:
- https://localhost:7148 (HTTPS)
- http://localhost:5236 (HTTP)

### API Endpoints

#### Process Message
```
POST /api/message/process
```

Example NewCompany payload:
```json
{
  "id": "0BA545F1-64C8-487C-988F-1B466A06B30F",
  "messageType": "NewCompany",
  "companyName": "Acme Corporation",
  "companyCode": "ACME001",
  "licensing": "Standard",
  "devices": [
    {
      "orderNo": "ORDER-123",
      "type": "Standard",
      "address": "123 Main St, City, Country"
    }
  ]
}
```

Example DeleteDevices payload:
```json
{
  "id": "0BA545F1-64C8-487C-988F-1B466A06B31F",
  "messageType": "DeleteDevices",
  "serialNumbers": ["SN-123456-7890", "SN-123456-7891"]
}
```

#### Health Check
```
GET /api/healthcheck
```

## Running Tests

### Using Visual Studio
1. Open the solution in Visual Studio 2022
2. Open Test Explorer
3. Run all tests

### Using Command Line
```bash
# Navigate to the test project directory
cd MoodMedia.MessageProcessor.Tests

# Run the tests
dotnet test
```

## Project Structure

- **Controllers**: API endpoints and request handling
- **Models**: Entity models and message DTOs
- **Repositories**: Data access layer with Dapper implementation
- **Services**: Business logic and message handling
- **Utilities**: Helper classes and utilities
- **HealthChecks**: Health monitoring components
- **Middleware**: Custom middleware for authentication and rate limiting

## Testing Strategy

1. **Unit Tests**: Validate individual components in isolation
   - Repository tests with mocked database connections
   - Service and handler tests with mocked repositories
   - Controller tests with mocked services

2. **Integration Tests**: Test the interaction between components
   - End-to-end message processing tests
   - API endpoint tests with mock repositories

3. **Health and Monitoring**:
   - Database connectivity tests
   - API health check endpoint 