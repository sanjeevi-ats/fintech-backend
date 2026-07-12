using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IProfitLossReportingService
{
    Task<List<ProfitLossStatement>> GeneratePeriodComparisonAsync(Guid period1Id, Guid period2Id, CancellationToken ct = default);
    Task<Dictionary<Guid, ProfitLossStatement>> GenerateBranchPLAsync(Guid periodId, CancellationToken ct = default);
    Task<(long revenue, long expenses, long profit, decimal margin)> GetPeriodSummaryAsync(Guid periodId, CancellationToken ct = default);
    Task<List<ProfitLossStatement>> GenerateYTDAnalysisAsync(int year, CancellationToken ct = default);
}

public class ProfitLossReportingService : IProfitLossReportingService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ProfitLossReportingService> _logger;

    public ProfitLossReportingService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<ProfitLossReportingService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Compare P&L statements for two periods
    /// </summary>
    public async Task<List<ProfitLossStatement>> GeneratePeriodComparisonAsync(
        Guid period1Id,
        Guid period2Id,
        CancellationToken ct = default)
    {
        try
        {
            var results = await _context.ProfitLossStatements
                .Where(p => (p.PeriodId == period1Id || p.PeriodId == period2Id) && !p.IsDeleted)
                .OrderBy(p => p.StatementDate)
                .ToListAsync(ct);

            if (results.Count != 2)
            {
                _logger.LogWarning("Period comparison: Expected 2 P&L statements, got {Count}", results.Count);
                return new List<ProfitLossStatement>();
            }

            _logger.LogInformation("Generated P&L comparison between periods {Period1} and {Period2}",
                period1Id, period2Id);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating period comparison");
            return new List<ProfitLossStatement>();
        }
    }

    /// <summary>
    /// Generate P&L for each branch in a period
    /// </summary>
    public async Task<Dictionary<Guid, ProfitLossStatement>> GenerateBranchPLAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var branchPLs = await _context.ProfitLossStatements
                .Where(p => p.PeriodId == periodId && p.BranchId != null && !p.IsDeleted)
                .ToListAsync(ct);

            var results = branchPLs.GroupBy(p => p.BranchId)
                .ToDictionary(
                    g => g.Key ?? Guid.Empty,
                    g => g.FirstOrDefault() ?? new ProfitLossStatement());

            _logger.LogInformation("Generated branch P&L for period {PeriodId}: {Count} branches",
                periodId, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating branch P&L");
            return new Dictionary<Guid, ProfitLossStatement>();
        }
    }

    /// <summary>
    /// Get P&L summary for a period
    /// </summary>
    public async Task<(long revenue, long expenses, long profit, decimal margin)> GetPeriodSummaryAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var plStatement = await _context.ProfitLossStatements
                .FirstOrDefaultAsync(p => p.PeriodId == periodId && !p.IsDeleted, ct);

            if (plStatement == null)
                return (0, 0, 0, 0);

            _logger.LogInformation("Retrieved P&L summary for period {PeriodId}: Revenue={Revenue}, Expenses={Expenses}, Profit={Profit}",
                periodId, plStatement.TotalRevenue, plStatement.TotalExpenses, plStatement.GrossProfit);

            return (
                plStatement.TotalRevenue,
                plStatement.TotalExpenses,
                plStatement.GrossProfit,
                plStatement.ProfitMargin
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting period summary");
            return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Generate Year-to-Date P&L analysis
    /// </summary>
    public async Task<List<ProfitLossStatement>> GenerateYTDAnalysisAsync(
        int year,
        CancellationToken ct = default)
    {
        try
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            var ytdStatements = await _context.ProfitLossStatements
                .Where(p => p.StatementDate >= startDate && p.StatementDate <= endDate && !p.IsDeleted)
                .OrderBy(p => p.StatementDate)
                .ToListAsync(ct);

            _logger.LogInformation("Generated YTD P&L analysis for year {Year}: {Count} periods",
                year, ytdStatements.Count);
            return ytdStatements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating YTD analysis");
            return new List<ProfitLossStatement>();
        }
    }
}
