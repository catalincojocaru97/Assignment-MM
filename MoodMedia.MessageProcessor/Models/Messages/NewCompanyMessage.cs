using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MoodMedia.MessageProcessor.Models.Messages
{
    /// <summary>
    /// Message for creating a new company with associated locations and devices
    /// </summary>
    public class NewCompanyMessage : BaseMessage
    {
        /// <summary>
        /// The name of the company to create
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// The unique code for the company
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// The licensing type for the company (Standard, Premium, Enterprise)
        /// </summary>
        [Required]
        public string Licensing { get; set; } = string.Empty;

        /// <summary>
        /// The list of devices to create with this company
        /// </summary>
        [Required]
        public List<DeviceInfo> Devices { get; set; } = new List<DeviceInfo>();
    }

    /// <summary>
    /// Information about a device to be created
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// The order number of the device
        /// </summary>
        [Required]
        public string OrderNo { get; set; } = string.Empty;

        /// <summary>
        /// The type of the device (Standard, Custom)
        /// </summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The address where the device is located (optional for Custom devices)
        /// </summary>
        public string? Address { get; set; }
    }
} 