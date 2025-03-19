using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;

namespace MoodMedia.MessageProcessor.Utilities
{
    /// <summary>
    /// Utility class for managing database transactions
    /// </summary>
    public class TransactionManager
    {
        private readonly ILogger<TransactionManager> _logger;

        /// <summary>
        /// Initializes a new instance of the TransactionManager class
        /// </summary>
        /// <param name="logger">The logger</param>
        public TransactionManager(ILogger<TransactionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Executes an operation within a transaction scope
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="isolationLevel">The transaction isolation level</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = isolationLevel },
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                _logger.LogInformation("Beginning transaction with isolation level {IsolationLevel}", isolationLevel);
                
                var result = await operation();
                
                transactionScope.Complete();
                _logger.LogInformation("Transaction completed successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation within a transaction scope with no return value
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="isolationLevel">The transaction isolation level</param>
        public async Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = isolationLevel },
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                _logger.LogInformation("Beginning transaction with isolation level {IsolationLevel}", isolationLevel);
                
                await operation();
                
                transactionScope.Complete();
                _logger.LogInformation("Transaction completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed: {Message}", ex.Message);
                throw;
            }
        }
    }
} 