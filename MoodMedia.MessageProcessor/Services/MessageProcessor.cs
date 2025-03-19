using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Messages;
using MoodMedia.MessageProcessor.Services.Interfaces;

namespace MoodMedia.MessageProcessor.Services
{
    /// <summary>
    /// Implementation of IMessageProcessor that deserializes incoming messages and routes them to handlers
    /// </summary>
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageProcessor> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessMessageAsync(string jsonMessage, string correlationId, CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope(new { CorrelationId = correlationId });
            
            try
            {
                if (string.IsNullOrWhiteSpace(jsonMessage))
                {
                    _logger.LogError("Message is empty or null");
                    return false;
                }
                
                _logger.LogInformation("Processing message with correlation ID: {CorrelationId}", correlationId);
                
                // Parse the base message to determine the type
                BaseMessage baseMessage;
                try
                {
                    baseMessage = JsonSerializer.Deserialize<BaseMessage>(jsonMessage, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize message: {Error}", ex.Message);
                    return false;
                }

                if (baseMessage == null)
                {
                    _logger.LogError("Failed to parse message - deserialized to null");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(baseMessage.MessageType))
                {
                    _logger.LogError("Message type is missing");
                    return false;
                }
                
                _logger.LogInformation("Processing message of type {MessageType} with ID {MessageId}", 
                    baseMessage.MessageType, baseMessage.Id);

                // Process based on message type
                return await DispatchMessageAsync(baseMessage.MessageType, jsonMessage, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message processing was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing message: {Error}", ex.Message);
                return false;
            }
        }
        
        private async Task<bool> DispatchMessageAsync(string messageType, string jsonMessage, CancellationToken cancellationToken)
        {
            // Handle different message types using strategy pattern
            switch (messageType)
            {
                case "NewCompany":
                    return await HandleTypedMessageAsync<NewCompanyMessage>(jsonMessage, cancellationToken);
                    
                case "DeleteDevices":
                    return await HandleTypedMessageAsync<DeleteDevicesMessage>(jsonMessage, cancellationToken);
                    
                default:
                    _logger.LogError("Unknown message type: {MessageType}", messageType);
                    return false;
            }
        }
        
        private async Task<bool> HandleTypedMessageAsync<TMessage>(string jsonMessage, CancellationToken cancellationToken) 
            where TMessage : BaseMessage
        {
            try
            {
                // Create a scope for this handler to ensure proper DI lifetime
                using var scope = _serviceProvider.CreateScope();
                
                // Deserialize to specific message type
                var message = JsonSerializer.Deserialize<TMessage>(jsonMessage, _jsonOptions);
                
                if (message == null)
                {
                    _logger.LogError("Failed to deserialize message to {MessageType}", typeof(TMessage).Name);
                    return false;
                }
                
                // Get the appropriate handler
                var handler = scope.ServiceProvider.GetService<IMessageHandler<TMessage>>();
                
                if (handler == null)
                {
                    _logger.LogError("No handler registered for {MessageType}", typeof(TMessage).Name);
                    return false;
                }
                
                _logger.LogDebug("Dispatching message to handler {HandlerType}", handler.GetType().Name);
                
                // Process the message
                return await handler.HandleAsync(message, cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for {MessageType}: {Error}", 
                    typeof(TMessage).Name, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking handler for {MessageType}: {Error}",
                    typeof(TMessage).Name, ex.Message);
                return false;
            }
        }
    }
} 