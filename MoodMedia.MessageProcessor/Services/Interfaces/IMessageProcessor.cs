using System.Threading;
using System.Threading.Tasks;

namespace MoodMedia.MessageProcessor.Services.Interfaces
{
    /// <summary>
    /// Defines a contract for processing JSON messages and routing them to appropriate handlers
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Processes a JSON message by deserializing it and routing to the appropriate handler
        /// </summary>
        /// <param name="jsonMessage">The JSON message to process</param>
        /// <param name="correlationId">Unique identifier for tracking this request through the system</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the message was processed successfully; otherwise, false.
        /// </returns>
        Task<bool> ProcessMessageAsync(string jsonMessage, string correlationId, CancellationToken cancellationToken = default);
    }
} 