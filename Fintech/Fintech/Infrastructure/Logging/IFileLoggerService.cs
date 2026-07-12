using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Infrastructure.Logging
{
    public interface IFileLoggerService
    {
        Task LogInfoAsync(string apiName, string controllerName, string actionName, 
            object? requestParameters = null, object? requestBody = null, object? responseData = null, 
            long executionTimeMs = 0, string? successMessage = null);

        Task LogWarningAsync(string apiName, string controllerName, string actionName, 
            string warningMessage, long executionTimeMs = 0);

        Task LogErrorAsync(string apiName, string controllerName, string actionName, 
            Exception exception, object? requestParameters = null, object? requestBody = null, 
            long executionTimeMs = 0);

        Task LogStoredProcedureAsync(string procedureName, Dictionary<string, object?>? inputParameters = null, 
            long executionTimeMs = 0, object? result = null, Exception? exception = null);
    }
}
