using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiskManagementController : ControllerBase
{
    private readonly IRiskManagementService _riskService;
    private readonly ILogger<RiskManagementController> _logger;

    public RiskManagementController(
        IRiskManagementService riskService,
        ILogger<RiskManagementController> logger)
    {
        _riskService = riskService;
        _logger = logger;
    }

    /// <summary>
    /// Generate risk profile for a period
    /// </summary>
    [HttpPost("profile/generate")]
    public async Task<IActionResult> GenerateRiskProfile(
        [FromBody] GenerateRiskProfileRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/riskmanagement/profile/generate - Generating profile for period {PeriodId}", request?.PeriodId);

            if (request?.PeriodId == Guid.Empty)
                return BadRequest(new { error = "PeriodId is required" });

            var result = await _riskService.GenerateRiskProfileAsync(request.PeriodId, request.BranchId, ct);

            if (result == null)
                return NotFound(new { error = "Period not found or profile generation failed" });

            _logger.LogInformation("POST /api/riskmanagement/profile/generate - Success (200): Risk profile generated (Level={Level}, Score={Score})",
                result.RiskLevel, result.OverallRiskScore);

            return Ok(new { data = MapRiskProfileToDto(result), message = "Risk profile generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risk profile");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get risk profile for a period
    /// </summary>
    [HttpGet("profile/{periodId}")]
    public async Task<IActionResult> GetRiskProfile(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/riskmanagement/profile/{PeriodId}", periodId);

            var result = await _riskService.GenerateRiskProfileAsync(periodId, branchId, ct);

            if (result == null)
            {
                _logger.LogWarning("GET /api/riskmanagement/profile/{PeriodId} - Not found (404)", periodId);
                return NotFound(new { error = "Risk profile not found" });
            }

            _logger.LogInformation("GET /api/riskmanagement/profile/{PeriodId} - Success (200)", periodId);
            return Ok(new { data = MapRiskProfileToDto(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk profile");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get risk alerts for a branch
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] Guid? branchId = null,
        [FromQuery] string severity = "",
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/riskmanagement/alerts - severity={Severity}", severity);

            var branchIdToUse = branchId ?? Guid.NewGuid(); // In real scenario, would use tenant service
            var result = await _riskService.GetAlertsAsync(branchIdToUse, severity, ct);

            _logger.LogInformation("GET /api/riskmanagement/alerts - Success (200): Retrieved {Count} alerts", result.Count);
            return Ok(new { data = MapAlertsToDto(result), count = result.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk alerts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new risk alert
    /// </summary>
    [HttpPost("alerts/create")]
    public async Task<IActionResult> CreateAlert(
        [FromBody] CreateRiskAlertRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/riskmanagement/alerts/create - Creating alert: {Title}", request?.Title);

            if (string.IsNullOrEmpty(request?.Title))
                return BadRequest(new { error = "Title is required" });

            if (request.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _riskService.CreateAlertAsync(
                request.BranchId,
                request.Title,
                request.Message ?? "",
                request.Severity ?? "Medium",
                ct);

            _logger.LogInformation("POST /api/riskmanagement/alerts/create - Success (201): Alert created (Severity={Severity})",
                result.Severity);

            return StatusCode(201, new { data = MapAlertToDto(result), message = "Risk alert created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating risk alert");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update alert status
    /// </summary>
    [HttpPatch("alerts/{alertId}/status")]
    public async Task<IActionResult> UpdateAlertStatus(
        Guid alertId,
        [FromBody] UpdateAlertStatusRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("PATCH /api/riskmanagement/alerts/{AlertId}/status - New status={Status}", alertId, request?.Status);

            if (string.IsNullOrEmpty(request?.Status))
                return BadRequest(new { error = "Status is required" });

            var success = await _riskService.UpdateAlertStatusAsync(alertId, request.Status, ct);

            if (!success)
            {
                _logger.LogWarning("PATCH /api/riskmanagement/alerts/{AlertId}/status - Failed to update", alertId);
                return NotFound(new { error = "Alert not found or update failed" });
            }

            _logger.LogInformation("PATCH /api/riskmanagement/alerts/{AlertId}/status - Success (200)", alertId);
            return Ok(new { message = "Alert status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get risk summary
    /// </summary>
    [HttpGet("summary/{periodId}")]
    public async Task<IActionResult> GetRiskSummary(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/riskmanagement/summary/{PeriodId}", periodId);

            var result = await _riskService.GetRiskSummaryAsync(periodId, ct);

            _logger.LogInformation("GET /api/riskmanagement/summary/{PeriodId} - Success (200): Total Alerts={Total}, Critical={Critical}",
                periodId, result.TotalAlerts, result.CriticalAlerts);

            return Ok(new { data = MapRiskSummaryToDto(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk summary");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get critical alerts only
    /// </summary>
    [HttpGet("alerts/critical")]
    public async Task<IActionResult> GetCriticalAlerts(
        [FromQuery] Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/riskmanagement/alerts/critical");

            var branchIdToUse = branchId ?? Guid.NewGuid();
            var result = await _riskService.GetAlertsAsync(branchIdToUse, "Critical", ct);

            _logger.LogInformation("GET /api/riskmanagement/alerts/critical - Success (200): Retrieved {Count} critical alerts", result.Count);
            return Ok(new { data = MapAlertsToDto(result), count = result.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving critical alerts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private RiskProfileDto MapRiskProfileToDto(RiskProfile entity)
    {
        return new RiskProfileDto
        {
            Id = entity.Id,
            PeriodId = entity.PeriodId,
            BranchId = entity.BranchId,
            OverallRiskScore = entity.OverallRiskScore,
            RiskLevel = entity.RiskLevel,
            LiquidityRisk = entity.LiquidityRisk,
            CreditRisk = entity.CreditRisk,
            OperationalRisk = entity.OperationalRisk,
            MarketRisk = entity.MarketRisk,
            KeyRisks = entity.KeyRisks,
            Recommendations = entity.Recommendations,
            CreatedAt = entity.CreatedAt
        };
    }

    private RiskAlertDto MapAlertToDto(RiskAlert entity)
    {
        return new RiskAlertDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            Title = entity.Title,
            Message = entity.Message,
            Severity = entity.Severity,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            ResolvedAt = entity.ResolvedAt
        };
    }

    private List<RiskAlertDto> MapAlertsToDto(List<RiskAlert> entities)
    {
        var result = new List<RiskAlertDto>();
        foreach (var entity in entities)
        {
            result.Add(MapAlertToDto(entity));
        }
        return result;
    }

    private RiskSummaryDto MapRiskSummaryToDto(RiskSummary entity)
    {
        return new RiskSummaryDto
        {
            TotalAlerts = entity.TotalAlerts,
            CriticalAlerts = entity.CriticalAlerts,
            HighAlerts = entity.HighAlerts,
            MediumAlerts = entity.MediumAlerts,
            LowAlerts = entity.LowAlerts,
            OverallRiskScore = entity.OverallRiskScore,
            RiskTrend = entity.RiskTrend
        };
    }
}

// DTOs for request/response mapping
public class GenerateRiskProfileRequest
{
    public Guid PeriodId { get; set; }
    public Guid? BranchId { get; set; }
}

public class CreateRiskAlertRequest
{
    public Guid BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Severity { get; set; }
}

public class UpdateAlertStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// Response DTOs
public class RiskProfileDto
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid BranchId { get; set; }
    public decimal OverallRiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public int LiquidityRisk { get; set; }
    public int CreditRisk { get; set; }
    public int OperationalRisk { get; set; }
    public int MarketRisk { get; set; }
    public List<string> KeyRisks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RiskAlertDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class RiskSummaryDto
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public decimal OverallRiskScore { get; set; }
    public string RiskTrend { get; set; } = string.Empty;
}

