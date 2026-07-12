using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IProfitLossService
{
    Task<ProfitLossStatement?> GeneratePLStatementAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<ProfitLossStatement?> GetPLStatementAsync(Guid periodId, CancellationToken ct = default);
    Task<(long totalRevenue, long totalExpenses, long netProfit)> CalculatePLAsync(Guid periodId, CancellationToken ct = default);
    Task<List<ProfitLossStatement>> GetPLHistoryAsync(int months = 12, CancellationToken ct = default);
}

public class ProfitLossService : IProfitLossService
{
    private readonly FinVedaDbContext _context;
    private readonly IRevenueTrackingService _revenueService;
    private readonly IExpenseTrackingService _expenseService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ProfitLossService> _logger;

    public ProfitLossService(
        FinVedaDbContext context,
        IRevenueTrackingService revenueService,
        IExpenseTrackingService expenseService,
        ITenantService tenantService,
        ILogger<ProfitLossService> logger)
    {
        _context = context;
        _revenueService = revenueService;
        _expenseService = expenseService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Generate P&L statement for a given period and branch
    /// </summary>
    public async Task<ProfitLossStatement?> GeneratePLStatementAsync(
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

            // Get existing P&L or create new one
            var plStatement = await _context.ProfitLossStatements
                .FirstOrDefaultAsync(p => p.PeriodId == periodId && p.BranchId == branchId, ct);

            if (plStatement == null)
            {
                plStatement = new ProfitLossStatement
                {
                    Id = Guid.NewGuid(),
                    PeriodId = periodId,
                    BranchId = branchId,
                    StatementDate = DateTime.UtcNow,
                    Status = (int)ProfitLossStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProfitLossStatements.Add(plStatement);
            }
            else
            {
                plStatement.UpdatedAt = DateTime.UtcNow;
            }

            // Clear existing lines
            var existingRevenue = await _context.RevenueLines
                .Where(r => r.ProfitLossStatementId == plStatement.Id)
                .ToListAsync(ct);
            _context.RevenueLines.RemoveRange(existingRevenue);

            var existingExpenses = await _context.ExpenseLines
                .Where(e => e.ProfitLossStatementId == plStatement.Id)
                .ToListAsync(ct);
            _context.ExpenseLines.RemoveRange(existingExpenses);

            // Get revenue data
            var revenues = await _revenueService.GetRevenueAsync(periodId, branchId, ct);
            long totalRevenue = 0;

            foreach (var revenue in revenues)
            {
                var revenueLine = new RevenueLine
                {
                    Id = Guid.NewGuid(),
                    ProfitLossStatementId = plStatement.Id,
                    Category = revenue.Category,
                    Amount = revenue.Amount,
                    Description = revenue.Description,
                    ReferenceCode = revenue.ReferenceCode,
                    CreatedAt = DateTime.UtcNow
                };
                _context.RevenueLines.Add(revenueLine);
                totalRevenue += revenue.Amount;
            }

            // Get expense data
            var expenses = await _expenseService.GetExpensesAsync(periodId, branchId, ct);
            long totalExpenses = 0;

            foreach (var expense in expenses)
            {
                var expenseLine = new ExpenseLine
                {
                    Id = Guid.NewGuid(),
                    ProfitLossStatementId = plStatement.Id,
                    Category = expense.Category,
                    Amount = expense.Amount,
                    Description = expense.Description,
                    ReferenceCode = expense.ReferenceCode,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ExpenseLines.Add(expenseLine);
                totalExpenses += expense.Amount;
            }

            // Calculate totals
            plStatement.TotalRevenue = totalRevenue;
            plStatement.TotalExpenses = totalExpenses;
            plStatement.GrossProfit = totalRevenue - totalExpenses;
            plStatement.NetProfit = plStatement.GrossProfit; // For now, same as gross profit
            plStatement.ProfitMargin = totalRevenue > 0 
                ? (decimal)plStatement.GrossProfit / totalRevenue * 100 
                : 0;
            plStatement.Status = (int)ProfitLossStatus.Generated;

            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("P&L statement generated for period {PeriodId}, branch {BranchId}", periodId, branchId);
            return plStatement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating P&L statement for period {PeriodId}", periodId);
            return null;
        }
    }

    /// <summary>
    /// Get P&L statement for a given period
    /// </summary>
    public async Task<ProfitLossStatement?> GetPLStatementAsync(Guid periodId, CancellationToken ct = default)
    {
        try
        {
            var plStatement = await _context.ProfitLossStatements
                .Include(p => p.RevenueLines)
                .Include(p => p.ExpenseLines)
                .FirstOrDefaultAsync(p => p.PeriodId == periodId, ct);

            return plStatement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving P&L statement for period {PeriodId}", periodId);
            return null;
        }
    }

    /// <summary>
    /// Calculate P&L totals for a given period
    /// </summary>
    public async Task<(long totalRevenue, long totalExpenses, long netProfit)> CalculatePLAsync(
        Guid periodId, 
        CancellationToken ct = default)
    {
        try
        {
            var plStatement = await GetPLStatementAsync(periodId, ct);
            if (plStatement == null)
                return (0, 0, 0);

            return (plStatement.TotalRevenue, plStatement.TotalExpenses, plStatement.GrossProfit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating P&L for period {PeriodId}", periodId);
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Get P&L history for specified number of months
    /// </summary>
    public async Task<List<ProfitLossStatement>> GetPLHistoryAsync(int months = 12, CancellationToken ct = default)
    {
        try
        {
            var history = await _context.ProfitLossStatements
                .OrderByDescending(p => p.StatementDate)
                .Take(months)
                .ToListAsync(ct);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving P&L history");
            return new List<ProfitLossStatement>();
        }
    }
}
