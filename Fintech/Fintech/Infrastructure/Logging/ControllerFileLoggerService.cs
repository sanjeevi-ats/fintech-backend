using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Fintech.Infrastructure.Logging
{
    /// <summary>
    /// Service that creates and manages controller-specific log files.
    /// Each controller gets its own directory with daily log files.
    /// </summary>
    public class ControllerFileLoggerService : IControllerFileLoggerService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ControllerFileLoggerService> _logger;
        private readonly string _logsBaseDirectory;
        private readonly object _lockObject = new();

        public ControllerFileLoggerService(IWebHostEnvironment environment, ILogger<ControllerFileLoggerService> logger)
        {
            _environment = environment;
            _logger = logger;
            _logsBaseDirectory = Path.Combine(environment.ContentRootPath, "Logs", "Controllers");
            
            // Create base Logs/Controllers directory if it doesn't exist
            if (!Directory.Exists(_logsBaseDirectory))
            {
                Directory.CreateDirectory(_logsBaseDirectory);
            }
        }

        public async Task LogInfoAsync(string controllerName, string actionName, 
            object? requestParameters = null, object? requestBody = null, object? responseData = null, 
            long executionTimeMs = 0, string? successMessage = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "INFO",
                    ControllerName = controllerName,
                    ActionName = actionName,
                    RequestParameters = requestParameters,
                    RequestBody = requestBody,
                    ResponseData = responseData,
                    ExecutionTimeMs = executionTimeMs,
                    SuccessMessage = successMessage ?? "Operation completed successfully"
                };

                await WriteLogAsync(controllerName, logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log info for controller {ControllerName}: {Message}", controllerName, ex.Message);
            }
        }

        public async Task LogWarningAsync(string controllerName, string actionName, 
            string warningMessage, long executionTimeMs = 0)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "WARNING",
                    ControllerName = controllerName,
                    ActionName = actionName,
                    WarningMessage = warningMessage,
                    ExecutionTimeMs = executionTimeMs
                };

                await WriteLogAsync(controllerName, logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log warning for controller {ControllerName}: {Message}", controllerName, ex.Message);
            }
        }

        public async Task LogErrorAsync(string controllerName, string actionName, 
            Exception exception, object? requestParameters = null, object? requestBody = null, 
            long executionTimeMs = 0)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "ERROR",
                    ControllerName = controllerName,
                    ActionName = actionName,
                    RequestParameters = requestParameters,
                    RequestBody = requestBody,
                    ExceptionMessage = exception.Message,
                    InnerException = exception.InnerException?.Message,
                    StackTrace = exception.StackTrace,
                    ExecutionTimeMs = executionTimeMs
                };

                await WriteLogAsync(controllerName, logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log error for controller {ControllerName}: {Message}", controllerName, ex.Message);
            }
        }

        public async Task LogStoredProcedureAsync(string controllerName, string procedureName, 
            Dictionary<string, object?>? inputParameters = null, 
            long executionTimeMs = 0, object? result = null, Exception? exception = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = exception == null ? "INFO" : "ERROR",
                    LogType = "STORED_PROCEDURE",
                    ControllerName = controllerName,
                    ProcedureName = procedureName,
                    InputParameters = inputParameters,
                    ExecutionTimeMs = executionTimeMs,
                    Result = result,
                    ExceptionMessage = exception?.Message,
                    InnerException = exception?.InnerException?.Message,
                    StackTrace = exception?.StackTrace
                };

                await WriteLogAsync(controllerName, logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stored procedure for controller {ControllerName}: {Message}", controllerName, ex.Message);
            }
        }

        public IEnumerable<FileInfo> GetControllerLogFiles(string controllerName)
        {
            try
            {
                var controllerLogDir = Path.Combine(_logsBaseDirectory, SanitizeControllerName(controllerName));
                if (!Directory.Exists(controllerLogDir))
                {
                    return Enumerable.Empty<FileInfo>();
                }

                var directoryInfo = new DirectoryInfo(controllerLogDir);
                // Get all .log files (hourly rolling logs) ordered by most recent first
                return directoryInfo.GetFiles("*.log").OrderByDescending(f => f.LastWriteTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get log files for controller {ControllerName}: {Message}", controllerName, ex.Message);
                return Enumerable.Empty<FileInfo>();
            }
        }

        public IEnumerable<string> GetAllControllerLogDirectories()
        {
            try
            {
                if (!Directory.Exists(_logsBaseDirectory))
                {
                    return Enumerable.Empty<string>();
                }

                var directoryInfo = new DirectoryInfo(_logsBaseDirectory);
                return directoryInfo.GetDirectories().Select(d => d.Name).OrderBy(n => n);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get controller log directories: {Message}", ex.Message);
                return Enumerable.Empty<string>();
            }
        }

        private async Task WriteLogAsync(string controllerName, object logEntry)
        {
            lock (_lockObject)
            {
                try
                {
                    // Sanitize controller name for directory
                    var sanitizedControllerName = SanitizeControllerName(controllerName);
                    var controllerLogDir = Path.Combine(_logsBaseDirectory, sanitizedControllerName);

                    // Create controller-specific directory if it doesn't exist
                    if (!Directory.Exists(controllerLogDir))
                    {
                        Directory.CreateDirectory(controllerLogDir);
                    }

                    // Generate hourly rolling log filename: ControllerName_yyyy-MM-dd_HH.log
                    var now = DateTime.UtcNow;
                    var logFileName = $"{sanitizedControllerName}_{now:yyyy-MM-dd}_{now:HH}.log";
                    var logFilePath = Path.Combine(controllerLogDir, logFileName);

                    var jsonLog = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });

                    var logMessage = $"{jsonLog}\n{new string('-', 80)}\n";

                    File.AppendAllText(logFilePath, logMessage, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write log to controller file: {Message}", ex.Message);
                }
            }
        }

        private string SanitizeControllerName(string controllerName)
        {
            // Remove "Controller" suffix if present and ensure safe directory name
            var sanitized = controllerName.EndsWith("Controller") 
                ? controllerName.Substring(0, controllerName.Length - 10) 
                : controllerName;

            // Replace invalid characters for directory names
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar.ToString(), string.Empty);
            }

            return sanitized;
        }
    }
}
