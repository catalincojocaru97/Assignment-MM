using System.Threading;
using System.Threading.Tasks;
using MoodMedia.MessageProcessor.Models.Messages;

namespace MoodMedia.MessageProcessor.Services.Interfaces
{
    /// <summary>
    /// Defines a contract for message handlers that process specific message types
    /// </summary>
    /// <typeparam name="TMessage">The type of message this handler can process</typeparam>
    public interface IMessageHandler<in TMessage> where TMessage : BaseMessage
    {
        /// <summary>
        /// Handles the processing of a message asynchronously
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the message was processed successfully; otherwise, false.
        /// </returns>
        Task<bool> HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    }
} 