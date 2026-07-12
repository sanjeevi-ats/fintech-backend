using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CashFlowController : ControllerBase
{
    private readonly ICashFlowService _cashFlowService;
    private readonly IOperatingCashFlowService _operatingCFService;
    private readonly IInvestingCashFlowService _investingCFService;
    private readonly IFinancingCashFlowService _financingCFService;
    private readonly ICashFlowForecastingService _forecastingService;
    private readonly ILiquidityAnalysisService _liquidityService;
    private readonly ILogger<CashFlowController> _logger;

    public CashFlowController(
        ICashFlowService cashFlowService,
        IOperatingCashFlowService operatingCFService,
        IInvestingCashFlowService investingCFService,
        IFinancingCashFlowService financingCFService,
        ICashFlowForecastingService forecastingService,
        ILiquidityAnalysisService liquidityService,
        ILogger<CashFlowController> logger)
    {
        _cashFlowService = cashFlowService;
        _operatingCFService = operatingCFService;
        _investingCFService = investingCFService;
        _financingCFService = financingCFService;
        _forecastingService = forecastingService;
        _liquidityService = liquidityService;
        _logger = logger;
    }

    /// <summary>
    /// Generate cash flow statement for a period
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateCashFlowStatement(
        [FromBody] GenerateCashFlowRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/cashflow/generate - Generating CF statement for period {PeriodId}", request?.PeriodId);

            if (request?.PeriodId == Guid.Empty)
                return BadRequest(new { error = "PeriodId is required" });

            var result = await _cashFlowService.GenerateCashFlowStatementAsync(request.PeriodId, request.BranchId, ct);

            if (result == null)
                return NotFound(new { error = "Period not found or CF statement generation failed" });

            _logger.LogInformation("POST /api/cashflow/generate - Success (200): CF statement generated");
            return Ok(new { data = MapToDto(result), message = "Cash flow statement generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow statement");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cash flow statement for a period
    /// </summary>
    [HttpGet("statement/{periodId}")]
    public async Task<IActionResult> GetCashFlowStatement(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/statement/{PeriodId}", periodId);

            var result = await _cashFlowService.GetCashFlowStatementAsync(periodId, ct);

            if (result == null)
            {
                _logger.LogWarning("GET /api/cashflow/statement/{PeriodId} - Not found (404)", periodId);
                return NotFound(new { error = "Cash flow statement not found" });
            }

            _logger.LogInformation("GET /api/cashflow/statement/{PeriodId} - Success (200)", periodId);
            return Ok(new { data = MapToDto(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow statement");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate cash flow totals for a period
    /// </summary>
    [HttpGet("calculate/{periodId}")]
    public async Task<IActionResult> CalculateCashFlow(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/calculate/{PeriodId}", periodId);

            var (operating, investing, financing, net) = await _cashFlowService.CalculateCashFlowAsync(periodId, ct);

            _logger.LogInformation("GET /api/cashflow/calculate/{PeriodId} - Success (200)", periodId);
            return Ok(new
            {
                data = new
                {
                    operatingCF = operating,
                    investingCF = investing,
                    financingCF = financing,
                    netCF = net
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cash flow history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetCashFlowHistory(
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/history - months={Months}", months);

            var result = await _cashFlowService.GetCashFlowHistoryAsync(months, ct);

            _logger.LogInformation("GET /api/cashflow/history - Success (200): Retrieved {Count} records", result.Count);
            return Ok(new { data = result, count = result.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow history");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get operating cash flow breakdown
    /// </summary>
    [HttpGet("operating/{periodId}")]
    public async Task<IActionResult> GetOperatingCashFlow(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/operating/{PeriodId}", periodId);

            var inflows = await _operatingCFService.GetOperatingInflowsAsync(periodId, null, ct);
            var outflows = await _operatingCFService.GetOperatingOutflowsAsync(periodId, null, ct);
            var net = await _operatingCFService.CalculateOperatingCFAsync(periodId, null, ct);

            _logger.LogInformation("GET /api/cashflow/operating/{PeriodId} - Success (200)", periodId);
            return Ok(new
            {
                data = new
                {
                    inflows = inflows,
                    outflows = outflows,
                    netCF = net
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operating cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get investing cash flow breakdown
    /// </summary>
    [HttpGet("investing/{periodId}")]
    public async Task<IActionResult> GetInvestingCashFlow(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/investing/{PeriodId}", periodId);

            var inflows = await _investingCFService.GetInvestingInflowsAsync(periodId, null, ct);
            var outflows = await _investingCFService.GetInvestingOutflowsAsync(periodId, null, ct);
            var net = await _investingCFService.CalculateInvestingCFAsync(periodId, null, ct);

            _logger.LogInformation("GET /api/cashflow/investing/{PeriodId} - Success (200)", periodId);
            return Ok(new
            {
                data = new
                {
                    inflows = inflows,
                    outflows = outflows,
                    netCF = net
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investing cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get financing cash flow breakdown
    /// </summary>
    [HttpGet("financing/{periodId}")]
    public async Task<IActionResult> GetFinancingCashFlow(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/financing/{PeriodId}", periodId);

            var inflows = await _financingCFService.GetFinancingInflowsAsync(periodId, null, ct);
            var outflows = await _financingCFService.GetFinancingOutflowsAsync(periodId, null, ct);
            var net = await _financingCFService.CalculateFinancingCFAsync(periodId, null, ct);

            _logger.LogInformation("GET /api/cashflow/financing/{PeriodId} - Success (200)", periodId);
            return Ok(new
            {
                data = new
                {
                    inflows = inflows,
                    outflows = outflows,
                    netCF = net
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financing cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate cash flow forecast
    /// </summary>
    [HttpPost("forecast")]
    public async Task<IActionResult> GenerateForecast(
        [FromBody] GenerateCashFlowForecastRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/cashflow/forecast - Generating forecast for period {PeriodId}", request?.PeriodId);

            if (request?.PeriodId == Guid.Empty)
                return BadRequest(new { error = "PeriodId is required" });

            var result = await _forecastingService.GenerateForecastAsync(
                request.PeriodId, 
                request.MonthsAhead ?? 3, 
                request.BranchId, 
                ct);

            if (result == null)
            {
                _logger.LogWarning("POST /api/cashflow/forecast - Forecast generation failed");
                return BadRequest(new { error = "Forecast generation failed" });
            }

            _logger.LogInformation("POST /api/cashflow/forecast - Success (200)");
            return Ok(new { data = MapForecastToDto(result), message = "Forecast generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow forecast");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get liquidity analysis
    /// </summary>
    [HttpGet("liquidity/{periodId}")]
    public async Task<IActionResult> GetLiquidityAnalysis(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/liquidity/{PeriodId}", periodId);

            var (assets, liabilities, ratio) = await _liquidityService.GetLiquidityPositionAsync(periodId, null, ct);
            var (operating, cash, quick) = await _liquidityService.CalculateCoverageRatiosAsync(periodId, null, ct);
            var (riskLevel, recommendation, riskScore) = await _liquidityService.AssessRiskAsync(periodId, null, ct);
            var breakdown = await _liquidityService.GetLiquidityBreakdownAsync(periodId, ct);

            _logger.LogInformation("GET /api/cashflow/liquidity/{PeriodId} - Success (200)", periodId);
            return Ok(new
            {
                data = new
                {
                    position = new { currentAssets = assets, currentLiabilities = liabilities, ratio = ratio },
                    ratios = new { operatingRatio = operating, cashRatio = cash, quickRatio = quick },
                    risk = new { level = riskLevel, score = riskScore, recommendation = recommendation },
                    breakdown = breakdown
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving liquidity analysis");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cash flow trends
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetCashFlowTrends(
        Guid periodId,
        [FromQuery] int monthsHistory = 12,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/cashflow/trends - monthsHistory={MonthsHistory}", monthsHistory);

            var (min, max, expected) = await _forecastingService.AnalyzeTrendsAsync(periodId, monthsHistory, ct);

            _logger.LogInformation("GET /api/cashflow/trends - Success (200)");
            return Ok(new
            {
                data = new
                {
                    min = min,
                    max = max,
                    expected = expected,
                    range = max - min
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow trends");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private CashFlowStatementDto MapToDto(CashFlowStatement entity)
    {
        return new CashFlowStatementDto
        {
            Id = entity.Id,
            PeriodId = entity.PeriodId,
            BranchId = entity.BranchId,
            StatementDate = entity.StatementDate,
            BeginningBalance = entity.BeginningBalance,
            OperatingCashFlow = entity.OperatingCashFlow,
            InvestingCashFlow = entity.InvestingCashFlow,
            FinancingCashFlow = entity.FinancingCashFlow,
            NetCashFlow = entity.NetCashFlow,
            EndingBalance = entity.EndingBalance,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private CashFlowForecastDto MapForecastToDto(CashFlowForecast entity)
    {
        return new CashFlowForecastDto
        {
            Id = entity.Id,
            PeriodId = entity.PeriodId,
            ForecastPeriod = entity.ForecastPeriod,
            ProjectedCashFlow = entity.ProjectedCashFlow,
            ConfidenceLevel = entity.ConfidenceLevel,
            Assumptions = entity.Assumptions,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}

// DTOs for request/response mapping
public class GenerateCashFlowRequest
{
    public Guid PeriodId { get; set; }
    public Guid? BranchId { get; set; }
}

public class GenerateCashFlowForecastRequest
{
    public Guid PeriodId { get; set; }
    public int? MonthsAhead { get; set; }
    public Guid? BranchId { get; set; }
}
