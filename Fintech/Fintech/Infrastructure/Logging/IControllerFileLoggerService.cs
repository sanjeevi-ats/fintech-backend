using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Infrastructure.Logging
{
    /// <summary>
    /// Service for controller-wise logging. Each controller gets its own log file.
    /// </summary>
    public interface IControllerFileLoggerService
    {
        /// <summary>
        /// Log an info-level message to the controller's log file
        /// </summary>
        Task LogInfoAsync(string controllerName, string actionName, 
            object? requestParameters = null, object? requestBody = null, object? responseData = null, 
            long executionTimeMs = 0, string? successMessage = null);

        /// <summary>
        /// Log a warning-level message to the controller's log file
        /// </summary>
        Task LogWarningAsync(string controllerName, string actionName, 
            string warningMessage, long executionTimeMs = 0);

        /// <summary>
        /// Log an error-level message to the controller's log file
        /// </summary>
        Task LogErrorAsync(string controllerName, string actionName, 
            Exception exception, object? requestParameters = null, object? requestBody = null, 
            long executionTimeMs = 0);

        /// <summary>
        /// Log a stored procedure execution to the controller's log file
        /// </summary>
        Task LogStoredProcedureAsync(string controllerName, string procedureName, 
            Dictionary<string, object?>? inputParameters = null, 
            long executionTimeMs = 0, object? result = null, Exception? exception = null);

        /// <summary>
        /// Get all log files for a specific controller
        /// </summary>
        IEnumerable<FileInfo> GetControllerLogFiles(string controllerName);

        /// <summary>
        /// Get all available controller log directories
        /// </summary>
        IEnumerable<string> GetAllControllerLogDirectories();
    }
}
