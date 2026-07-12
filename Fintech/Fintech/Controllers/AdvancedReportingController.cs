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
public class AdvancedReportingController : ControllerBase
{
    private readonly IAdvancedReportingService _reportingService;
    private readonly ILogger<AdvancedReportingController> _logger;

    public AdvancedReportingController(
        IAdvancedReportingService reportingService,
        ILogger<AdvancedReportingController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Compare financial metrics between two periods
    /// </summary>
    [HttpPost("compare/periods")]
    public async Task<IActionResult> ComparePeriodsAsync(
        [FromBody] ComparePeriodsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/advancedreporting/compare/periods - Comparing periods {Period1} and {Period2}",
                request?.Period1Id, request?.Period2Id);

            if (request?.Period1Id == Guid.Empty || request?.Period2Id == Guid.Empty)
                return BadRequest(new { error = "Both Period1Id and Period2Id are required" });

            var result = await _reportingService.ComparePeriodsAsync(request.Period1Id, request.Period2Id, ct);

            if (result == null)
            {
                _logger.LogWarning("POST /api/advancedreporting/compare/periods - One or both periods not found");
                return NotFound(new { error = "One or both periods not found" });
            }

            _logger.LogInformation("POST /api/advancedreporting/compare/periods - Success (200): Revenue Change={Change}%",
                result.RevenueChangePercent.ToString("F2"));

            return Ok(new { data = MapComparativeAnalysisToDto(result), message = "Comparative analysis generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing periods");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze budget vs actual performance
    /// </summary>
    [HttpPost("analyze/budget")]
    public async Task<IActionResult> AnalyzeBudgetVsActualAsync(
        [FromBody] BudgetAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/advancedreporting/analyze/budget - Analyzing period {PeriodId}", request?.PeriodId);

            if (request?.PeriodId == Guid.Empty)
                return BadRequest(new { error = "PeriodId is required" });

            var result = await _reportingService.AnalyzeBudgetVsActualAsync(request.PeriodId, request.BudgetId, ct);

            if (result == null)
            {
                _logger.LogWarning("POST /api/advancedreporting/analyze/budget - Period not found");
                return NotFound(new { error = "Period not found" });
            }

            _logger.LogInformation("POST /api/advancedreporting/analyze/budget - Success (200): Variance={Variance}% ({Status})",
                result.VariancePercent.ToString("F2"), result.VarianceStatus);

            return Ok(new { data = MapBudgetAnalysisToDto(result), message = "Budget analysis generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing budget");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance metrics for a period
    /// </summary>
    [HttpGet("metrics/{periodId}")]
    public async Task<IActionResult> GetPerformanceMetricsAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/advancedreporting/metrics/{PeriodId}", periodId);

            var result = await _reportingService.GetPerformanceMetricsAsync(periodId, ct);

            if (result == null)
            {
                _logger.LogWarning("GET /api/advancedreporting/metrics/{PeriodId} - Period not found", periodId);
                return NotFound(new { error = "Period not found" });
            }

            _logger.LogInformation("GET /api/advancedreporting/metrics/{PeriodId} - Success (200): RoE={RoE}%, RoA={RoA}%",
                periodId, result.RoE.ToString("F2"), result.RoA.ToString("F2"));

            return Ok(new { data = MapPerformanceMetricsToDto(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run what-if scenario analysis
    /// </summary>
    [HttpPost("scenario/run")]
    public async Task<IActionResult> RunScenarioAsync(
        [FromBody] ScenarioAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/advancedreporting/scenario/run - Running scenario for period {PeriodId}",
                request?.PeriodId);

            if (request?.PeriodId == Guid.Empty)
                return BadRequest(new { error = "PeriodId is required" });

            var result = await _reportingService.RunScenarioAsync(
                request.PeriodId,
                request.Assumptions ?? 0,
                ct);

            if (result == null)
            {
                _logger.LogWarning("POST /api/advancedreporting/scenario/run - Scenario generation failed");
                return BadRequest(new { error = "Scenario generation failed" });
            }

            _logger.LogInformation("POST /api/advancedreporting/scenario/run - Success (200): {Scenario}, Impact={Impact}",
                result.ScenarioName, result.Impact);

            return Ok(new { data = MapScenarioAnalysisToDto(result), message = "Scenario analysis generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scenario analysis");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get custom reports for a branch
    /// </summary>
    [HttpGet("custom-reports")]
    public async Task<IActionResult> GetCustomReportsAsync(
        [FromQuery] Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/advancedreporting/custom-reports");

            var branchIdToUse = branchId ?? Guid.NewGuid();
            var result = await _reportingService.GetCustomReportsAsync(branchIdToUse, ct);

            _logger.LogInformation("GET /api/advancedreporting/custom-reports - Success (200): Retrieved {Count} reports",
                result.Count);

            return Ok(new { data = MapCustomReportsToDto(result), count = result.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving custom reports");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new custom report
    /// </summary>
    [HttpPost("custom-reports/create")]
    public async Task<IActionResult> CreateCustomReportAsync(
        [FromBody] CreateCustomReportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/advancedreporting/custom-reports/create - Creating report: {Name}",
                request?.Name);

            if (string.IsNullOrEmpty(request?.Name))
                return BadRequest(new { error = "Report name is required" });

            if (request.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _reportingService.CreateCustomReportAsync(
                request.BranchId,
                request.Name,
                request.QueryJson ?? "{}",
                ct);

            _logger.LogInformation("POST /api/advancedreporting/custom-reports/create - Success (201): Report created with ID={ReportId}",
                result.Id);

            return StatusCode(201, new { data = MapCustomReportToDto(result), message = "Custom report created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get comprehensive financial dashboard
    /// </summary>
    [HttpGet("dashboard/{periodId}")]
    public async Task<IActionResult> GetDashboardAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/advancedreporting/dashboard/{PeriodId}", periodId);

            var metrics = await _reportingService.GetPerformanceMetricsAsync(periodId, ct);
            var summary = new RiskSummary(); // Would get from risk service in real scenario

            if (metrics == null)
            {
                _logger.LogWarning("GET /api/advancedreporting/dashboard/{PeriodId} - Period not found", periodId);
                return NotFound(new { error = "Period not found" });
            }

            _logger.LogInformation("GET /api/advancedreporting/dashboard/{PeriodId} - Success (200)", periodId);

            return Ok(new
            {
                data = new
                {
                    metrics = MapPerformanceMetricsToDto(metrics),
                    summary = new
                    {
                        period = periodId,
                        asOfDate = DateTime.UtcNow,
                        status = "Completed"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private ComparativeAnalysisDto MapComparativeAnalysisToDto(ComparativeAnalysis entity)
    {
        return new ComparativeAnalysisDto
        {
            Period1Id = entity.Period1Id,
            Period2Id = entity.Period2Id,
            RevenueChange = entity.RevenueChange,
            RevenueChangePercent = entity.RevenueChangePercent,
            ExpenseChange = entity.ExpenseChange,
            ExpenseChangePercent = entity.ExpenseChangePercent,
            ProfitChange = entity.ProfitChange,
            ProfitChangePercent = entity.ProfitChangePercent,
            CashFlowChange = entity.CashFlowChange,
            CashFlowChangePercent = entity.CashFlowChangePercent,
            MetricChanges = entity.MetricChanges,
            Insights = entity.Insights
        };
    }

    private BudgetAnalysisDto MapBudgetAnalysisToDto(BudgetAnalysis entity)
    {
        return new BudgetAnalysisDto
        {
            PeriodId = entity.PeriodId,
            BudgetedAmount = entity.BudgetedAmount,
            ActualAmount = entity.ActualAmount,
            Variance = entity.Variance,
            VariancePercent = entity.VariancePercent,
            VarianceStatus = entity.VarianceStatus,
            DetailedAnalysis = entity.DetailedAnalysis,
            Recommendations = entity.Recommendations
        };
    }

    private PerformanceMetricsDto MapPerformanceMetricsToDto(PerformanceMetrics entity)
    {
        return new PerformanceMetricsDto
        {
            PeriodId = entity.PeriodId,
            RoE = entity.RoE,
            RoA = entity.RoA,
            ProfitMargin = entity.ProfitMargin,
            OperatingMargin = entity.OperatingMargin,
            AssetTurnover = entity.AssetTurnover,
            DebtToEquity = entity.DebtToEquity,
            CurrentRatio = entity.CurrentRatio,
            QuickRatio = entity.QuickRatio,
            RevenuePerEmployee = entity.RevenuePerEmployee,
            CustomMetrics = entity.CustomMetrics
        };
    }

    private ScenarioAnalysisDto MapScenarioAnalysisToDto(ScenarioAnalysis entity)
    {
        return new ScenarioAnalysisDto
        {
            PeriodId = entity.PeriodId,
            ScenarioName = entity.ScenarioName,
            Assumptions = entity.Assumptions,
            BaselineRevenue = entity.BaselineRevenue,
            ProjectedRevenue = entity.ProjectedRevenue,
            RevenueDifference = entity.RevenueDifference,
            BaselineProfit = entity.BaselineProfit,
            ProjectedProfit = entity.ProjectedProfit,
            ProfitDifference = entity.ProfitDifference,
            Impact = entity.Impact,
            Implications = entity.Implications
        };
    }

    private CustomReportDto MapCustomReportToDto(CustomReport entity)
    {
        return new CustomReportDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            Name = entity.Name,
            QueryJson = entity.QueryJson,
            CreatedAt = entity.CreatedAt,
            LastRunAt = entity.LastRunAt,
            LastResults = entity.LastResults
        };
    }

    private List<CustomReportDto> MapCustomReportsToDto(List<CustomReport> entities)
    {
        var result = new List<CustomReportDto>();
        foreach (var entity in entities)
        {
            result.Add(MapCustomReportToDto(entity));
        }
        return result;
    }
}

// Request DTOs
public class ComparePeriodsRequest
{
    public Guid Period1Id { get; set; }
    public Guid Period2Id { get; set; }
}

public class BudgetAnalysisRequest
{
    public Guid PeriodId { get; set; }
    public Guid? BudgetId { get; set; }
}

public class ScenarioAnalysisRequest
{
    public Guid PeriodId { get; set; }
    public decimal? Assumptions { get; set; }
}

public class CreateCustomReportRequest
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? QueryJson { get; set; }
}

// Response DTOs
public class ComparativeAnalysisDto
{
    public Guid Period1Id { get; set; }
    public Guid Period2Id { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal ExpenseChange { get; set; }
    public decimal ExpenseChangePercent { get; set; }
    public decimal ProfitChange { get; set; }
    public decimal ProfitChangePercent { get; set; }
    public long CashFlowChange { get; set; }
    public decimal CashFlowChangePercent { get; set; }
    public Dictionary<string, decimal> MetricChanges { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class BudgetAnalysisDto
{
    public Guid PeriodId { get; set; }
    public long BudgetedAmount { get; set; }
    public long ActualAmount { get; set; }
    public long Variance { get; set; }
    public decimal VariancePercent { get; set; }
    public string VarianceStatus { get; set; } = string.Empty;
    public Dictionary<string, BudgetLine> DetailedAnalysis { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class PerformanceMetricsDto
{
    public Guid PeriodId { get; set; }
    public decimal RoE { get; set; }
    public decimal RoA { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal OperatingMargin { get; set; }
    public decimal AssetTurnover { get; set; }
    public decimal DebtToEquity { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal QuickRatio { get; set; }
    public long RevenuePerEmployee { get; set; }
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}

public class ScenarioAnalysisDto
{
    public Guid PeriodId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public decimal Assumptions { get; set; }
    public long BaselineRevenue { get; set; }
    public long ProjectedRevenue { get; set; }
    public long RevenueDifference { get; set; }
    public long BaselineProfit { get; set; }
    public long ProjectedProfit { get; set; }
    public long ProfitDifference { get; set; }
    public string Impact { get; set; } = string.Empty;
    public List<string> Implications { get; set; } = new();
}

public class CustomReportDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public Dictionary<string, object> LastResults { get; set; } = new();
}

