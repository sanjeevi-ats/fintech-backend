using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Fintech.Infrastructure.Logging
{
    /// <summary>
    /// Service for analyzing and querying controller-wise logs
    /// </summary>
    public interface IControllerLogAnalyzerService
    {
        /// <summary>
        /// Get log entries for a specific controller
        /// </summary>
        Task<IEnumerable<Dictionary<string, JsonElement>>> GetControllerLogsAsync(string controllerName, int? limitDays = null);

        /// <summary>
        /// Get error logs for a specific controller
        /// </summary>
        Task<IEnumerable<Dictionary<string, JsonElement>>> GetControllerErrorsAsync(string controllerName, int? limitDays = null);

        /// <summary>
        /// Get slow operations (above threshold) for a controller
        /// </summary>
        Task<IEnumerable<Dictionary<string, JsonElement>>> GetSlowOperationsAsync(string controllerName, long thresholdMs = 1000, int? limitDays = null);

        /// <summary>
        /// Get execution time statistics for a controller action
        /// </summary>
        Task<ExecutionTimeStats> GetActionStatsAsync(string controllerName, string actionName, int? limitDays = null);

        /// <summary>
        /// Get all controllers with logging data
        /// </summary>
        Task<IEnumerable<ControllerLogSummary>> GetAllControllersSummaryAsync();
    }

    public class ExecutionTimeStats
    {
        public string ControllerName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public long AverageExecutionMs { get; set; }
        public long MinExecutionMs { get; set; }
        public long MaxExecutionMs { get; set; }
        public int ErrorCount { get; set; }
    }

    public class ControllerLogSummary
    {
        public string ControllerName { get; set; } = string.Empty;
        public int TotalLogEntries { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public DateTime LastLogTime { get; set; }
    }

    public class ControllerLogAnalyzerService : IControllerLogAnalyzerService
    {
        private readonly IControllerFileLoggerService _fileLogger;
        private readonly ILogger<ControllerLogAnalyzerService> _logger;
        private readonly string _logsBaseDirectory;

        public ControllerLogAnalyzerService(IWebHostEnvironment environment, IControllerFileLoggerService fileLogger, ILogger<ControllerLogAnalyzerService> logger)
        {
            _fileLogger = fileLogger;
            _logger = logger;
            _logsBaseDirectory = Path.Combine(environment.ContentRootPath, "Logs", "Controllers");
        }

        public async Task<IEnumerable<Dictionary<string, JsonElement>>> GetControllerLogsAsync(string controllerName, int? limitDays = null)
        {
            return await ReadControllerLogsAsync(controllerName, logLevel: null, limitDays);
        }

        public async Task<IEnumerable<Dictionary<string, JsonElement>>> GetControllerErrorsAsync(string controllerName, int? limitDays = null)
        {
            return await ReadControllerLogsAsync(controllerName, logLevel: "ERROR", limitDays);
        }

        public async Task<IEnumerable<Dictionary<string, JsonElement>>> GetSlowOperationsAsync(string controllerName, long thresholdMs = 1000, int? limitDays = null)
        {
            var logs = await ReadControllerLogsAsync(controllerName, logLevel: null, limitDays);
            return logs.Where(log => 
                log.ContainsKey("ExecutionTimeMs") && 
                long.TryParse(log["ExecutionTimeMs"].GetRawText(), out var execTime) &&
                execTime > thresholdMs
            );
        }

        public async Task<ExecutionTimeStats> GetActionStatsAsync(string controllerName, string actionName, int? limitDays = null)
        {
            var logs = await ReadControllerLogsAsync(controllerName, logLevel: null, limitDays);
            var actionLogs = logs.Where(log =>
                log.ContainsKey("ActionName") &&
                log["ActionName"].GetString() == actionName
            ).ToList();

            if (actionLogs.Count == 0)
            {
                return new ExecutionTimeStats
                {
                    ControllerName = controllerName,
                    ActionName = actionName
                };
            }

            var executionTimes = new List<long>();
            var errorCount = 0;

            foreach (var log in actionLogs)
            {
                if (log.ContainsKey("ExecutionTimeMs") && 
                    long.TryParse(log["ExecutionTimeMs"].GetRawText(), out var execTime))
                {
                    executionTimes.Add(execTime);
                }

                if (log.ContainsKey("LogLevel") && 
                    log["LogLevel"].GetString() == "ERROR")
                {
                    errorCount++;
                }
            }

            return new ExecutionTimeStats
            {
                ControllerName = controllerName,
                ActionName = actionName,
                TotalCalls = actionLogs.Count,
                AverageExecutionMs = executionTimes.Any() ? (long)executionTimes.Average() : 0,
                MinExecutionMs = executionTimes.Any() ? executionTimes.Min() : 0,
                MaxExecutionMs = executionTimes.Any() ? executionTimes.Max() : 0,
                ErrorCount = errorCount
            };
        }

        public async Task<IEnumerable<ControllerLogSummary>> GetAllControllersSummaryAsync()
        {
            var summaries = new List<ControllerLogSummary>();
            var controllerDirs = _fileLogger.GetAllControllerLogDirectories();

            foreach (var controllerDir in controllerDirs)
            {
                try
                {
                    var logs = await ReadControllerLogsAsync(controllerDir, logLevel: null, limitDays: null);
                    var logList = logs.ToList();

                    if (logList.Count == 0)
                        continue;

                    var summary = new ControllerLogSummary
                    {
                        ControllerName = controllerDir,
                        TotalLogEntries = logList.Count,
                        ErrorCount = logList.Count(log => log.ContainsKey("LogLevel") && log["LogLevel"].GetString() == "ERROR"),
                        WarningCount = logList.Count(log => log.ContainsKey("LogLevel") && log["LogLevel"].GetString() == "WARNING"),
                        LastLogTime = logList
                            .Where(log => log.ContainsKey("Timestamp") && 
                                   DateTime.TryParse(log["Timestamp"].GetString(), out _))
                            .Select(log => DateTime.Parse(log["Timestamp"].GetString()!))
                            .Max()
                    };

                    summaries.Add(summary);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting summary for controller {ControllerName}", controllerDir);
                }
            }

            return summaries.OrderByDescending(s => s.LastLogTime);
        }

        private async Task<IEnumerable<Dictionary<string, JsonElement>>> ReadControllerLogsAsync(string controllerName, string? logLevel = null, int? limitDays = null)
        {
            var logEntries = new List<Dictionary<string, JsonElement>>();

            try
            {
                var logFiles = _fileLogger.GetControllerLogFiles(controllerName);
                var cutoffDate = limitDays.HasValue 
                    ? DateTime.UtcNow.AddDays(-limitDays.Value) 
                    : DateTime.MinValue;

                var filesToRead = logFiles
                    .Where(f => f.LastWriteTimeUtc >= cutoffDate)
                    .ToList();

                foreach (var logFile in filesToRead)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(logFile.FullName);
                        var entries = ParseLogEntries(content);

                        if (!string.IsNullOrEmpty(logLevel))
                        {
                            entries = entries.Where(e => 
                                e.ContainsKey("LogLevel") && 
                                e["LogLevel"].GetString() == logLevel
                            ).ToList();
                        }

                        logEntries.AddRange(entries);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading log file {FilePath}", logFile.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading controller logs for {ControllerName}", controllerName);
            }

            return logEntries.OrderByDescending(e => 
                e.ContainsKey("Timestamp") ? DateTime.Parse(e["Timestamp"].GetString()!) : DateTime.MinValue
            );
        }

        private List<Dictionary<string, JsonElement>> ParseLogEntries(string content)
        {
            var entries = new List<Dictionary<string, JsonElement>>();
            var separator = new string('-', 80);

            var parts = content.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart))
                    continue;

                try
                {
                    using (var doc = JsonDocument.Parse(trimmedPart))
                    {
                        var entry = new Dictionary<string, JsonElement>();
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            entry[property.Name] = property.Value.Clone();
                        }
                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing log entry: {Content}", trimmedPart[..Math.Min(100, trimmedPart.Length)]);
                }
            }

            return entries;
        }
    }
}
