using System.Threading.Tasks;
using MoodMedia.MessageProcessor.Models.Entities;

namespace MoodMedia.MessageProcessor.Repositories.Interfaces
{
    /// <summary>
    /// Interface for company data access operations
    /// </summary>
    public interface ICompanyRepository
    {
        /// <summary>
        /// Creates a new company record in the database
        /// </summary>
        /// <param name="company">The company entity to create</param>
        /// <returns>The ID of the newly created company</returns>
        Task<int> CreateCompanyAsync(Company company);

        /// <summary>
        /// Retrieves a company by its unique code
        /// </summary>
        /// <param name="code">The company code to search for</param>
        /// <returns>The company if found, null otherwise</returns>
        Task<Company?> GetCompanyByCodeAsync(string code);
    }
} 