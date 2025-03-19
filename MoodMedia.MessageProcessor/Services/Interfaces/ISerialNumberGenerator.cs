using System.Threading.Tasks;

namespace MoodMedia.MessageProcessor.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for generating unique device serial numbers
    /// </summary>
    public interface ISerialNumberGenerator
    {
        /// <summary>
        /// Generates a unique serial number for a device asynchronously
        /// </summary>
        /// <returns>A unique serial number string</returns>
        /// <remarks>
        /// The generated serial number follows the format: SN-{timestamp}-{random}
        /// The method ensures uniqueness by checking against existing serial numbers in the database
        /// </remarks>
        Task<string> GenerateUniqueSerialNumberAsync();
    }
} 