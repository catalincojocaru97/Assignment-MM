using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoodMedia.MessageProcessor.Models.Entities;
using MoodMedia.MessageProcessor.Repositories.Interfaces;

namespace MoodMedia.MessageProcessor.Repositories
{
    /// <summary>
    /// Implementation of ICompanyRepository using Dapper and ADO.NET
    /// </summary>
    public class CompanyRepository : ICompanyRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<CompanyRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the CompanyRepository class
        /// </summary>
        /// <param name="dbContext">The database context</param>
        /// <param name="logger">The logger</param>
        public CompanyRepository(DatabaseContext dbContext, ILogger<CompanyRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> CreateCompanyAsync(Company company)
        {
            try
            {
                _logger.LogInformation("Creating company with code {CompanyCode}", company.Code);
                
                const string sql = @"
                    INSERT INTO Company (Name, Code, Licensing)
                    VALUES (@Name, @Code, @Licensing);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                using (var connection = _dbContext.CreateConnection())
                {
                    var companyId = await connection.QuerySingleAsync<int>(sql, new
                    {
                        company.Name,
                        company.Code,
                        company.Licensing
                    });
                    
                    _logger.LogInformation("Company created successfully with ID {CompanyId}", companyId);
                    return companyId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Company?> GetCompanyByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Getting company by code {CompanyCode}", code);
                
                const string sql = @"SELECT Id, Name, Code, Licensing FROM Company WHERE Code = @Code";

                using (var connection = _dbContext.CreateConnection())
                {
                    var company = await connection.QuerySingleOrDefaultAsync<Company>(sql, new { Code = code });
                    
                    if (company == null)
                    {
                        _logger.LogWarning("Company with code {CompanyCode} not found", code);
                    }
                    else
                    {
                        _logger.LogInformation("Company with code {CompanyCode} found", code);
                    }
                    
                    return company;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company by code {CompanyCode}: {Message}", code, ex.Message);
                throw;
            }
        }
    }
} 