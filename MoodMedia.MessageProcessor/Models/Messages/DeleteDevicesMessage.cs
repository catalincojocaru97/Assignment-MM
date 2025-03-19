using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Messages
{
    /// <summary>
    /// Message for deleting devices based on their serial numbers
    /// </summary>
    public class DeleteDevicesMessage : BaseMessage
    {
        /// <summary>
        /// The list of serial numbers of devices to delete
        /// </summary>
        [Required]
        public List<string> SerialNumbers { get; set; } = new List<string>();
    }
} 