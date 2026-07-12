using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface ICashFlowService
{
    Task<CashFlowStatement?> GenerateCashFlowStatementAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<CashFlowStatement?> GetCashFlowStatementAsync(Guid periodId, CancellationToken ct = default);
    Task<(long operating, long investing, long financing, long net)> CalculateCashFlowAsync(Guid periodId, CancellationToken ct = default);
    Task<List<CashFlowStatement>> GetCashFlowHistoryAsync(int months = 12, CancellationToken ct = default);
}

public class CashFlowService : ICashFlowService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CashFlowService> _logger;

    public CashFlowService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<CashFlowService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Generate cash flow statement for a period
    /// </summary>
    public async Task<CashFlowStatement?> GenerateCashFlowStatementAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            // Get period
            var period = await _context.AccountingPeriods
                .FirstOrDefaultAsync(p => p.Id == periodId, ct);

            if (period == null)
            {
                _logger.LogWarning("Period not found: {PeriodId}", periodId);
                return null;
            }

            // Get existing CF or create new one
            var cfStatement = await _context.CashFlowStatements
                .FirstOrDefaultAsync(c => c.PeriodId == periodId && c.BranchId == branchId, ct);

            if (cfStatement == null)
            {
                cfStatement = new CashFlowStatement
                {
                    Id = Guid.NewGuid(),
                    PeriodId = periodId,
                    BranchId = branchId,
                    StatementDate = DateTime.UtcNow,
                    Status = (int)CashFlowStatementStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CashFlowStatements.Add(cfStatement);
            }
            else
            {
                cfStatement.UpdatedAt = DateTime.UtcNow;
            }

            // Clear existing items
            var existingItems = await _context.CashFlowItems
                .Where(c => c.CashFlowStatementId == cfStatement.Id)
                .ToListAsync(ct);
            _context.CashFlowItems.RemoveRange(existingItems);

            // Get Beginning Balance from prior period
            var priorCFStatement = await _context.CashFlowStatements
                .Where(c => c.BranchId == branchId && c.StatementDate < cfStatement.StatementDate && !c.IsDeleted)
                .OrderByDescending(c => c.StatementDate)
                .FirstOrDefaultAsync(ct);

            cfStatement.BeginningBalance = priorCFStatement?.EndingBalance ?? 0;

            // Calculate cash flows (will be populated by specific CF services)
            long operatingCF = 0;
            long investingCF = 0;
            long financingCF = 0;

            // Get receipt data for operating CF (simplified)
            var receipts = await _context.Receipts
                .Where(r => r.BranchId == branchId && r.CapturedAt >= period.StartDate && r.CapturedAt <= period.EndDate)
                .ToListAsync(ct);
            operatingCF = receipts.Sum(r => r.AmountPaid);

            // Calculate net CF
            long netCF = operatingCF + investingCF + financingCF;
            cfStatement.OperatingCashFlow = operatingCF;
            cfStatement.InvestingCashFlow = investingCF;
            cfStatement.FinancingCashFlow = financingCF;
            cfStatement.NetCashFlow = netCF;
            cfStatement.EndingBalance = cfStatement.BeginningBalance + netCF;
            cfStatement.Status = (int)CashFlowStatementStatus.Generated;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Cash flow statement generated for period {PeriodId}, branch {BranchId}", periodId, branchId);
            return cfStatement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow statement for period {PeriodId}", periodId);
            return null;
        }
    }

    /// <summary>
    /// Get cash flow statement for a period
    /// </summary>
    public async Task<CashFlowStatement?> GetCashFlowStatementAsync(Guid periodId, CancellationToken ct = default)
    {
        try
        {
            var cfStatement = await _context.CashFlowStatements
                .Include(c => c.CashFlowItems)
                .FirstOrDefaultAsync(c => c.PeriodId == periodId, ct);

            return cfStatement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow statement for period {PeriodId}", periodId);
            return null;
        }
    }

    /// <summary>
    /// Calculate cash flow totals for a period
    /// </summary>
    public async Task<(long operating, long investing, long financing, long net)> CalculateCashFlowAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var cfStatement = await GetCashFlowStatementAsync(periodId, ct);
            if (cfStatement == null)
                return (0, 0, 0, 0);

            return (cfStatement.OperatingCashFlow, cfStatement.InvestingCashFlow, 
                    cfStatement.FinancingCashFlow, cfStatement.NetCashFlow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cash flow for period {PeriodId}", periodId);
            return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Get cash flow history for specified number of months
    /// </summary>
    public async Task<List<CashFlowStatement>> GetCashFlowHistoryAsync(int months = 12, CancellationToken ct = default)
    {
        try
        {
            var history = await _context.CashFlowStatements
                .OrderByDescending(c => c.StatementDate)
                .Take(months)
                .ToListAsync(ct);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow history");
            return new List<CashFlowStatement>();
        }
    }
}
