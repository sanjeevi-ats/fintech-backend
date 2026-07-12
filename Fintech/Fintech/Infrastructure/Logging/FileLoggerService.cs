using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Fintech.Infrastructure.Logging
{
    public class FileLoggerService : IFileLoggerService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileLoggerService> _logger;
        private readonly string _logsDirectory;
        private readonly object _lockObject = new();

        public FileLoggerService(IWebHostEnvironment environment, ILogger<FileLoggerService> logger)
        {
            _environment = environment;
            _logger = logger;
            _logsDirectory = Path.Combine(environment.ContentRootPath, "Logs");
            
            // Create Logs directory if it doesn't exist
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }

        public async Task LogInfoAsync(string apiName, string controllerName, string actionName, 
            object? requestParameters = null, object? requestBody = null, object? responseData = null, 
            long executionTimeMs = 0, string? successMessage = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "INFO",
                    ApiName = apiName,
                    ControllerName = controllerName,
                    ActionName = actionName,
                    RequestParameters = requestParameters,
                    RequestBody = requestBody,
                    ResponseData = responseData,
                    ExecutionTimeMs = executionTimeMs,
                    SuccessMessage = successMessage ?? "Operation completed successfully"
                };

                await WriteLogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log info: {Message}", ex.Message);
            }
        }

        public async Task LogWarningAsync(string apiName, string controllerName, string actionName, 
            string warningMessage, long executionTimeMs = 0)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "WARNING",
                    ApiName = apiName,
                    ControllerName = controllerName,
                    ActionName = actionName,
                    WarningMessage = warningMessage,
                    ExecutionTimeMs = executionTimeMs
                };

                await WriteLogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log warning: {Message}", ex.Message);
            }
        }

        public async Task LogErrorAsync(string apiName, string controllerName, string actionName, 
            Exception exception, object? requestParameters = null, object? requestBody = null, 
            long executionTimeMs = 0)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "ERROR",
                    ApiName = apiName,
                    ControllerName = controllerName,
                    ActionName = actionName,
                    RequestParameters = requestParameters,
                    RequestBody = requestBody,
                    ExceptionMessage = exception.Message,
                    InnerException = exception.InnerException?.Message,
                    StackTrace = exception.StackTrace,
                    ExecutionTimeMs = executionTimeMs
                };

                await WriteLogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log error: {Message}", ex.Message);
            }
        }

        public async Task LogStoredProcedureAsync(string procedureName, Dictionary<string, object?>? inputParameters = null, 
            long executionTimeMs = 0, object? result = null, Exception? exception = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = exception == null ? "INFO" : "ERROR",
                    LogType = "STORED_PROCEDURE",
                    ProcedureName = procedureName,
                    InputParameters = inputParameters,
                    ExecutionTimeMs = executionTimeMs,
                    Result = result,
                    ExceptionMessage = exception?.Message,
                    InnerException = exception?.InnerException?.Message,
                    StackTrace = exception?.StackTrace
                };

                await WriteLogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stored procedure: {Message}", ex.Message);
            }
        }

        private async Task WriteLogAsync(object logEntry)
        {
            lock (_lockObject)
            {
                try
                {
                    var logFileName = $"log-{DateTime.UtcNow:yyyy-MM-dd}.txt";
                    var logFilePath = Path.Combine(_logsDirectory, logFileName);

                    var jsonLog = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });

                    var logMessage = $"{jsonLog}\n{new string('-', 80)}\n";

                    File.AppendAllText(logFilePath, logMessage, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write log to file: {Message}", ex.Message);
                }
            }
        }
    }
}
