using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

/// <summary>
/// API for analyzing controller-wise logs.
/// Provides endpoints for debugging, monitoring, and performance analysis.
/// 
/// Note: In production, consider protecting these endpoints with additional authorization
/// to restrict log access to admin/operations teams only.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogAnalysisController : BaseApiController
{
    private readonly IControllerLogAnalyzerService _analyzer;
    private readonly IControllerFileLoggerService _fileLogger;

    public LogAnalysisController(
        IControllerLogAnalyzerService analyzer,
        IControllerFileLoggerService fileLogger)
    {
        _analyzer = analyzer;
        _fileLogger = fileLogger;
    }

    /// <summary>
    /// Get all available controller log directories
    /// </summary>
    [HttpGet("controllers")]
    public IActionResult GetAllControllers()
    {
        var controllers = _fileLogger.GetAllControllerLogDirectories().ToList();
        return Ok(new
        {
            Count = controllers.Count,
            Controllers = controllers
        });
    }

    /// <summary>
    /// Get all logs for a specific controller (last 7 days by default)
    /// </summary>
    [HttpGet("controller/{controllerName}")]
    public async Task<IActionResult> GetControllerLogs(
        string controllerName,
        [FromQuery] int? limitDays = 7)
    {
        var logs = await _analyzer.GetControllerLogsAsync(controllerName, limitDays);
        return Ok(new
        {
            ControllerName = controllerName,
            LimitDays = limitDays,
            TotalEntries = logs.Count(),
            Logs = logs
        });
    }

    /// <summary>
    /// Get only ERROR level logs for a specific controller
    /// </summary>
    [HttpGet("controller/{controllerName}/errors")]
    public async Task<IActionResult> GetControllerErrors(
        string controllerName,
        [FromQuery] int? limitDays = 7)
    {
        var errors = await _analyzer.GetControllerErrorsAsync(controllerName, limitDays);
        var errorList = errors.ToList();
        return Ok(new
        {
            ControllerName = controllerName,
            LimitDays = limitDays,
            TotalErrors = errorList.Count,
            Errors = errorList
        });
    }

    /// <summary>
    /// Get operations that executed slower than the specified threshold
    /// </summary>
    [HttpGet("controller/{controllerName}/slow")]
    public async Task<IActionResult> GetSlowOperations(
        string controllerName,
        [FromQuery] long thresholdMs = 1000,
        [FromQuery] int? limitDays = 7)
    {
        var slowOps = await _analyzer.GetSlowOperationsAsync(controllerName, thresholdMs, limitDays);
        var slowOpsList = slowOps.ToList();
        return Ok(new
        {
            ControllerName = controllerName,
            ThresholdMs = thresholdMs,
            LimitDays = limitDays,
            SlowOperationCount = slowOpsList.Count,
            Operations = slowOpsList
        });
    }

    /// <summary>
    /// Get execution time statistics for a specific action in a controller
    /// </summary>
    [HttpGet("controller/{controllerName}/action/{actionName}/stats")]
    public async Task<IActionResult> GetActionStats(
        string controllerName,
        string actionName,
        [FromQuery] int? limitDays = 30)
    {
        var stats = await _analyzer.GetActionStatsAsync(controllerName, actionName, limitDays);
        return Ok(new
        {
            ControllerName = controllerName,
            ActionName = actionName,
            LimitDays = limitDays,
            Statistics = stats
        });
    }

    /// <summary>
    /// Get summary statistics for all controllers
    /// Shows total entries, errors, warnings, and last log time
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetAllControllersSummary()
    {
        var summaries = await _analyzer.GetAllControllersSummaryAsync();
        var summaryList = summaries.ToList();

        return Ok(new
        {
            TotalControllers = summaryList.Count,
            TotalLogEntries = summaryList.Sum(s => s.TotalLogEntries),
            TotalErrors = summaryList.Sum(s => s.ErrorCount),
            TotalWarnings = summaryList.Sum(s => s.WarningCount),
            Summaries = summaryList
        });
    }

    /// <summary>
    /// Get performance summary: slowest operations across all controllers
    /// </summary>
    [HttpGet("performance/slowest")]
    public async Task<IActionResult> GetSlowestOperations(
        [FromQuery] int limit = 20,
        [FromQuery] int? limitDays = 7)
    {
        var summaries = await _analyzer.GetAllControllersSummaryAsync();
        var slowestOps = new List<dynamic>();

        foreach (var summary in summaries)
        {
            var slowOps = await _analyzer.GetSlowOperationsAsync(
                summary.ControllerName,
                thresholdMs: 500,
                limitDays: limitDays);

            slowestOps.AddRange(slowOps.Take(5).Select(op => new
            {
                ControllerName = summary.ControllerName,
                Operation = op
            }));
        }

        return Ok(new
        {
            LimitDays = limitDays,
            Limit = limit,
            SlowestOperations = slowestOps.OrderByDescending(op => 
            {
                var execTimeElement = ((Dictionary<string, System.Text.Json.JsonElement>)op.Operation)
                    .TryGetValue("ExecutionTimeMs", out var val) ? val.GetInt64() : 0;
                return execTimeElement;
            }).Take(limit)
        });
    }

    /// <summary>
    /// Get error summary: most common errors across controllers
    /// </summary>
    [HttpGet("errors/summary")]
    public async Task<IActionResult> GetErrorSummary(
        [FromQuery] int limitDays = 7)
    {
        var summaries = await _analyzer.GetAllControllersSummaryAsync();
        var errorsByController = new List<dynamic>();

        foreach (var summary in summaries)
        {
            if (summary.ErrorCount > 0)
            {
                errorsByController.Add(new
                {
                    ControllerName = summary.ControllerName,
                    ErrorCount = summary.ErrorCount,
                    LastErrorTime = summary.LastLogTime
                });
            }
        }

        return Ok(new
        {
            LimitDays = limitDays,
            ControllersWithErrors = errorsByController.Count,
            TotalErrors = errorsByController.Sum(e => (int)e.ErrorCount),
            Details = errorsByController.OrderByDescending(e => (int)e.ErrorCount)
        });
    }

    /// <summary>
    /// Health check endpoint for log analysis service
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        try
        {
            var controllers = _fileLogger.GetAllControllerLogDirectories().Count();
            return Ok(new
            {
                Status = "Healthy",
                LogAnalysisEnabled = true,
                ControllersMonitored = controllers,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Message = ex.Message
            });
        }
    }
}
