using System.Collections.Generic;
using System.Threading.Tasks;
using MoodMedia.MessageProcessor.Models.Entities;

namespace MoodMedia.MessageProcessor.Repositories.Interfaces
{
    /// <summary>
    /// Interface for device data access operations
    /// </summary>
    public interface IDeviceRepository
    {
        /// <summary>
        /// Creates a new device record in the database
        /// </summary>
        /// <param name="device">The device entity to create</param>
        /// <returns>The ID of the newly created device</returns>
        Task<int> CreateDeviceAsync(Device device);

        /// <summary>
        /// Deletes devices with the specified serial numbers
        /// </summary>
        /// <param name="serialNumbers">List of serial numbers to delete</param>
        /// <returns>The number of devices deleted</returns>
        Task<int> DeleteDevicesBySerialNumbersAsync(List<string> serialNumbers);

        /// <summary>
        /// Checks if a device with the specified serial number exists
        /// </summary>
        /// <param name="serialNumber">The serial number to check</param>
        /// <returns>True if the serial number exists, false otherwise</returns>
        Task<bool> SerialNumberExistsAsync(string serialNumber);
    }
} 