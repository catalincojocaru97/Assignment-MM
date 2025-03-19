using System;
using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Messages
{
    /// <summary>
    /// Base class for all message types
    /// </summary>
    public class BaseMessage
    {
        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        public BaseMessage()
        {
        }
        
        /// <summary>
        /// The unique identifier for the message
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// The type of the message, used for message routing
        /// </summary>
        [Required]
        public string MessageType { get; set; } = string.Empty;
    }
} 