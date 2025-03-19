using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MoodMedia.MessageProcessor.HealthChecks;
using MoodMedia.MessageProcessor.Middleware;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Repositories;
using MoodMedia.MessageProcessor.Repositories.Interfaces;
using MoodMedia.MessageProcessor.Services;
using MoodMedia.MessageProcessor.Services.Handlers;
using MoodMedia.MessageProcessor.Services.Interfaces;
using MoodMedia.MessageProcessor.Utilities;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
ConfigureServices(builder.Services, builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigureApp(app);

app.Run();

void ConfigureServices(IServiceCollection services, bool isDevelopment)
{
    // Configure controllers with options
    services.AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
        options.RespectBrowserAcceptHeader = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = isDevelopment;
    });

    // API Explorer and Swagger
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Mood Media Message Processor API",
            Version = "v1",
            Description = "API for processing Mood Media messages",
            Contact = new OpenApiContact
            {
                Name = "Mood Media",
                Email = "support@moodmedia.com"
            }
        });
        
        // Add API key authentication to Swagger
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API key needed to access the endpoints. X-API-Key: YOUR_KEY",
            In = ParameterLocation.Header,
            Name = "X-API-Key",
            Type = SecuritySchemeType.ApiKey
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });
        
        // Set the comments path for the Swagger JSON and UI
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (System.IO.File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Register database services
    services.AddSingleton<DatabaseContext>();

    // Register repositories
    services.AddScoped<ICompanyRepository, CompanyRepository>();
    services.AddScoped<ILocationRepository, LocationRepository>();
    services.AddScoped<IDeviceRepository, DeviceRepository>();

    // Register utilities and services
    services.AddScoped<ISerialNumberGenerator, SerialNumberGenerator>();
    services.AddScoped<IMessageProcessor, MessageProcessor>();

    // Register message handlers
    services.AddScoped<IMessageHandler<NewCompanyMessage>, NewCompanyHandler>();
    services.AddScoped<IMessageHandler<DeleteDevicesMessage>, DeleteDevicesHandler>();

    // Configure API behavior
    services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // Configure health checks
    services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database_check", tags: new[] { "database", "sql" });
    
    // Configure logging
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();

        if (isDevelopment)
        {
            logging.SetMinimumLevel(LogLevel.Debug);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
    });

    // Add HTTP client factory for external API calls
    services.AddHttpClient();
    
    // Add CORS
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Add OpenTelemetry instrumentation
    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("MoodMedia.MessageProcessor", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("SqlClientDiagnosticSource"))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());
    
    // Add performance telemetry
    if (!isDevelopment)
    {
        services.AddResponseCompression();
    }
}

void ConfigureApp(WebApplication app)
{
    // Global error handling middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();
    
    // Use response compression in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseResponseCompression();
    }
    
    // Configure CORS - Move it to the beginning of the pipeline
    app.UseCors(builder => 
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
    
    // Development-specific middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mood Media API v1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at root
        });
    }
    else
    {
        // Production-specific middleware
        app.UseHsts();
    }

    // Common middleware
    app.UseHttpsRedirection();
    app.UseRouting();
    
    // Apply rate limiting and API key authentication AFTER routing
    app.UseMiddleware<ApiRateLimitMiddleware>();
    app.UseMiddleware<ApiKeyAuthMiddleware>();
    
    app.UseAuthorization();
    
    // Map health checks - adding OPTIONS method handling
    app.MapHealthChecks("/api/healthcheck", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            // Set headers to ensure CORS works
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, X-API-Key";
            
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                Status = report.Status.ToString(),
                Results = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description,
                        Duration = e.Value.Duration.ToString()
                    })
            };
            
            await System.Text.Json.JsonSerializer.SerializeAsync(
                context.Response.Body, 
                response, 
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
        }
    });
    
    // Add explicit endpoint for health check without auth requirements
    app.MapGet("/api/health", (HttpContext context) => {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, X-API-Key";
        
        return Results.Json(new {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    })
    .WithOpenApi()
    .AllowAnonymous();
    
    // API endpoints
    app.MapControllers();
}
