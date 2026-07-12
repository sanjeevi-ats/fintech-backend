using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Core.Domain;
using Fintech.Application.Services;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfitLossController : ControllerBase
{
    private readonly IProfitLossService _plService;
    private readonly IRevenueTrackingService _revenueService;
    private readonly IExpenseTrackingService _expenseService;
    private readonly IProfitLossReportingService _reportingService;
    private readonly ITenantService _tenantService;
    private readonly IControllerFileLoggerService _logger;

    public ProfitLossController(
        IProfitLossService plService,
        IRevenueTrackingService revenueService,
        IExpenseTrackingService expenseService,
        IProfitLossReportingService reportingService,
        ITenantService tenantService,
        IControllerFileLoggerService logger)
    {
        _plService = plService;
        _revenueService = revenueService;
        _expenseService = expenseService;
        _reportingService = reportingService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Generate P&L statement for a period
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GeneratePLStatement(
        [FromBody] GeneratePLStatementRequest request,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GeneratePLStatement), request);

            if (request.PeriodId == Guid.Empty)
                return BadRequest(new { success = false, message = "PeriodId is required" });

            var plStatement = await _plService.GeneratePLStatementAsync(
                request.PeriodId,
                request.BranchId,
                ct);

            if (plStatement == null)
                return NotFound(new { success = false, message = "Failed to generate P&L statement" });

            var dto = MapToDto(plStatement);
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GeneratePLStatement), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get P&L statement for a period
    /// </summary>
    [HttpGet("statement/{periodId}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetPLStatement(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetPLStatement), new { periodId });

            var plStatement = await _plService.GetPLStatementAsync(periodId, ct);
            if (plStatement == null)
                return NotFound(new { success = false, message = "P&L statement not found" });

            var dto = MapToDto(plStatement);
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetPLStatement), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get revenue breakdown for a period
    /// </summary>
    [HttpGet("revenue/{periodId}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetRevenue(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetRevenue), new { periodId });

            var revenues = await _revenueService.GetRevenueAsync(periodId, null, ct);
            var (interest, fees, penalties, other) = await _revenueService.AggregateRevenueAsync(periodId, ct);

            var summary = new RevenueSummaryDto
            {
                InterestRevenue = interest,
                FeeRevenue = fees,
                PenaltyRevenue = penalties,
                OtherRevenue = other,
                TotalRevenue = interest + fees + penalties + other
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetRevenue), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get expense breakdown for a period
    /// </summary>
    [HttpGet("expenses/{periodId}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetExpenses(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetExpenses), new { periodId });

            var expenses = await _expenseService.GetExpensesAsync(periodId, null, ct);
            var (provisions, waivers, operating, admin) = await _expenseService.AggregateExpensesAsync(periodId, ct);

            var summary = new ExpenseSummaryDto
            {
                ProvisionExpense = provisions,
                WaiverExpense = waivers,
                OperatingExpense = operating,
                AdminExpense = admin,
                TotalExpense = provisions + waivers + operating + admin
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetExpenses), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get P&L history for last N months
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetPLHistory(
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetPLHistory), new { months });

            if (months <= 0 || months > 36)
                return BadRequest(new { success = false, message = "Months must be between 1 and 36" });

            var history = await _plService.GetPLHistoryAsync(months, ct);
            var dtos = history.Select(MapToDto).ToList();

            return Ok(new { success = true, data = dtos });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetPLHistory), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Calculate P&L totals for a period
    /// </summary>
    [HttpGet("calculate/{periodId}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> CalculatePL(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(CalculatePL), new { periodId });

            var (revenue, expenses, profit) = await _plService.CalculatePLAsync(periodId, ct);

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalRevenue = revenue,
                    totalExpenses = expenses,
                    netProfit = profit,
                    profitMargin = revenue > 0 ? ((decimal)profit / revenue * 100) : 0
                }
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(CalculatePL), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Compare P&L statements between two periods
    /// </summary>
    [HttpGet("comparison")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> ComparePeriods(
        [FromQuery] Guid period1Id,
        [FromQuery] Guid period2Id,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(ComparePeriods), new { period1Id, period2Id });

            if (period1Id == Guid.Empty || period2Id == Guid.Empty)
                return BadRequest(new { success = false, message = "Both period IDs are required" });

            var comparison = await _reportingService.GeneratePeriodComparisonAsync(period1Id, period2Id, ct);
            if (comparison.Count < 2)
                return NotFound(new { success = false, message = "Could not find both P&L statements" });

            var dtos = comparison.Select(MapToDto).ToList();
            return Ok(new { success = true, data = dtos });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(ComparePeriods), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get P&L statements for all branches in a period
    /// </summary>
    [HttpGet("branch-wise/{periodId}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetBranchWisePL(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetBranchWisePL), new { periodId });

            var branchPLs = await _reportingService.GenerateBranchPLAsync(periodId, ct);
            var dtos = branchPLs.Select(kvp => new { branchId = kvp.Key, pl = MapToDto(kvp.Value) }).ToList();

            return Ok(new { success = true, data = dtos });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetBranchWisePL), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get Year-to-Date P&L analysis
    /// </summary>
    [HttpGet("ytd/{year}")]
    [Authorize(Policy = "RequireAccountant")]
    public async Task<IActionResult> GetYTDAnalysis(
        int year,
        CancellationToken ct = default)
    {
        try
        {
            await _logger.LogInfoAsync("ProfitLossController", nameof(GetYTDAnalysis), new { year });

            if (year < 2020 || year > DateTime.UtcNow.Year)
                return BadRequest(new { success = false, message = "Invalid year" });

            var ytdStatements = await _reportingService.GenerateYTDAnalysisAsync(year, ct);
            var dtos = ytdStatements.Select(MapToDto).ToList();

            return Ok(new { success = true, data = dtos });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ProfitLossController", nameof(GetYTDAnalysis), ex);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // Helper method to map domain entity to DTO
    private ProfitLossStatementDto MapToDto(ProfitLossStatement entity)
    {
        return new ProfitLossStatementDto
        {
            Id = entity.Id,
            PeriodId = entity.PeriodId,
            BranchId = entity.BranchId,
            TotalRevenue = entity.TotalRevenue,
            TotalExpenses = entity.TotalExpenses,
            GrossProfit = entity.GrossProfit,
            NetProfit = entity.NetProfit,
            ProfitMargin = entity.ProfitMargin,
            StatementDate = entity.StatementDate,
            Status = entity.Status,
            Notes = entity.Notes,
            RevenueLines = entity.RevenueLines?.Select(r => new RevenueLineDto
            {
                Id = r.Id,
                Category = r.Category,
                Amount = r.Amount,
                Description = r.Description,
                ReferenceCode = r.ReferenceCode
            }).ToList() ?? new(),
            ExpenseLines = entity.ExpenseLines?.Select(e => new ExpenseLineDto
            {
                Id = e.Id,
                Category = e.Category,
                Amount = e.Amount,
                Description = e.Description,
                ReferenceCode = e.ReferenceCode
            }).ToList() ?? new(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
