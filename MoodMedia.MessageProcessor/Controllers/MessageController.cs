using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Services.Interfaces;
using System.Text.Json;

namespace MoodMedia.MessageProcessor.Controllers
{
    /// <summary>
    /// Controller for message processing endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class MessageController : ControllerBase
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly ILogger<MessageController> _logger;

        /// <summary>
        /// Initializes a new instance of the MessageController class
        /// </summary>
        /// <param name="messageProcessor">The message processor service</param>
        /// <param name="logger">The logger</param>
        public MessageController(
            IMessageProcessor messageProcessor,
            ILogger<MessageController> logger)
        {
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes an incoming message
        /// </summary>
        /// <returns>A result indicating whether the message was processed successfully</returns>
        /// <remarks>
        /// Sample requests:
        /// 
        /// POST /api/message/process
        /// Content-Type: application/json
        /// 
        /// For creating a new company:
        /// ```json
        /// {
        ///   "id": "0BA545F1-64C8-487C-988F-1B466A06B30F",
        ///   "messageType": "NewCompany",
        ///   "companyName": "Acme Corporation",
        ///   "companyCode": "ACME001",
        ///   "licensing": "Standard",
        ///   "devices": [
        ///     {
        ///       "orderNo": "ORDER-123",
        ///       "type": "Standard",
        ///       "address": "123 Main St, City, Country"
        ///     },
        ///     {
        ///       "orderNo": "ORDER-124",
        ///       "type": "Custom",
        ///       "address": "456 Elm St, City, Country"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// For deleting devices:
        /// ```json
        /// {
        ///   "id": "0BA545F1-64C8-487C-988F-1B466A06B31F",
        ///   "messageType": "DeleteDevices",
        ///   "serialNumbers": ["SN-123456-7890", "SN-123456-7891"]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Message processed successfully</response>
        /// <response code="400">Invalid message format or processing failure</response>
        /// <response code="401">Unauthorized - API key is missing or invalid</response>
        /// <response code="429">Too many requests - Rate limit exceeded</response>
        /// <response code="500">Unexpected server error</response>
        [HttpPost("process")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessMessage([FromBody] object messageBody, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Generate a correlation ID for request tracking
            var correlationId = Guid.NewGuid().ToString();
                
            // Store correlation ID in response headers for client tracking
            // Add it early before any potential response has started
            Response.Headers["X-Correlation-ID"] = correlationId;
            
            try
            {                
                // Convert the message body to JSON
                var messageJson = messageBody != null ? JsonSerializer.Serialize(messageBody) : null;

                if (string.IsNullOrWhiteSpace(messageJson))
                {
                    _logger.LogWarning("Received empty message body");
                    return BadRequest(new { Success = false, Message = "Message body is required" });
                }

                var result = await _messageProcessor.ProcessMessageAsync(messageJson, correlationId, cancellationToken);

                // Log processing time for performance monitoring
                stopwatch.Stop();
                _logger.LogInformation("Message processing completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                if (result)
                {
                    return Ok(new { Success = true, Message = "Message processed successfully" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to process message" });
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogInformation("Request processing was cancelled by the client after {ElapsedMs}ms", 
                    stopwatch.ElapsedMilliseconds);
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unhandled exception in ProcessMessage endpoint after {ElapsedMs}ms", 
                    stopwatch.ElapsedMilliseconds);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Success = false, Message = "An unexpected error occurred" });
            }
        }
    }
} 