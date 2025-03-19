using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Entities
{
    /// <summary>
    /// Represents a company entity in the database
    /// </summary>
    public class Company
    {
        /// <summary>
        /// The unique identifier for the company
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the company
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The unique code identifying the company
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The licensing type of the company
        /// </summary>
        [Required]
        public int Licensing { get; set; }
    }
} 