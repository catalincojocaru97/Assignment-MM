using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Entities
{
    /// <summary>
    /// Represents a device entity in the database
    /// </summary>
    public class Device
    {
        /// <summary>
        /// The unique identifier for the device
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique serial number of the device
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// The type of the device (1 = Standard, 2 = Custom)
        /// </summary>
        [Required]
        public int Type { get; set; }

        /// <summary>
        /// The location identifier (foreign key)
        /// </summary>
        [Required]
        public int LocationId { get; set; }
    }
} 