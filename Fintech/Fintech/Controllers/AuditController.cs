using Fintech.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Authorize(Roles = "super_admin,accountant")]
[Route("api/v1/[controller]")]
public class AuditController : BaseApiController
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("{entityName}/{recordId}")]
    public async Task<IActionResult> GetHistory(string entityName, string recordId)
    {
        try
        {
            var logs = await _auditService.GetEntityHistoryAsync(entityName, recordId);
            
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHistory");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving entity history.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get audit log by business code
    /// </summary>
    /// <param name="code">Audit log code (e.g., AUD0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var log = await _auditService.GetByCodeAsync(code);
            if (log == null)
                return NotFound(new { message = $"Audit log with code {code} not found" });
            
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the audit log by code.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 100)
    {
        try
        {
            var logs = await _auditService.GetRecentLogsAsync(count);
            
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRecentLogs");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving recent audit logs.",
                Error = ex.Message
            });
        }
    }
}
