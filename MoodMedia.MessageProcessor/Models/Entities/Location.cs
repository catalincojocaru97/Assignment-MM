using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Entities
{
    /// <summary>
    /// Represents a location entity in the database
    /// </summary>
    public class Location
    {
        /// <summary>
        /// The unique identifier for the location
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the location
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The physical address of the location
        /// </summary>
        [Required]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// The parent company identifier (foreign key)
        /// </summary>
        [Required]
        public int ParentId { get; set; }
    }
} 