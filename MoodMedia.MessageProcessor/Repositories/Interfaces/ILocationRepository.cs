using System.Threading.Tasks;
using MoodMedia.MessageProcessor.Models.Entities;

namespace MoodMedia.MessageProcessor.Repositories.Interfaces
{
    /// <summary>
    /// Interface for location data access operations
    /// </summary>
    public interface ILocationRepository
    {
        /// <summary>
        /// Creates a new location record in the database
        /// </summary>
        /// <param name="location">The location entity to create</param>
        /// <returns>The ID of the newly created location</returns>
        Task<int> CreateLocationAsync(Location location);
    }
} 