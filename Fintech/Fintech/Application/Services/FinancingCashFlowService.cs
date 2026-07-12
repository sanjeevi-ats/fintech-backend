using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IFinancingCashFlowService
{
    Task<long> GetFinancingInflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> GetFinancingOutflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> CalculateFinancingCFAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<List<CashFlowItem>> GetFinancingItemsAsync(Guid cashFlowStatementId, CancellationToken ct = default);
    Task<CashFlowItem> AddFinancingItemAsync(Guid cashFlowStatementId, string type, long amount, string description, CancellationToken ct = default);
}

public class FinancingCashFlowService : IFinancingCashFlowService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<FinancingCashFlowService> _logger;

    public FinancingCashFlowService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<FinancingCashFlowService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get financing cash inflows (equity invested, debt raised, etc.)
    /// </summary>
    public async Task<long> GetFinancingInflowsAsync(
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
                return 0;
            }

            // Get equity investments (financing inflows)
            var equityInflows = await _context.CapitalTransactions
                .Where(ct => ct.TransactionType == "Contribution")
                .SumAsync(ct => (long)ct.Amount, ct);

            _logger.LogInformation("Financing inflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, equityInflows);
            return equityInflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating financing inflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get financing cash outflows (equity withdrawn, dividends paid, debt repayment, etc.)
    /// </summary>
    public async Task<long> GetFinancingOutflowsAsync(
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
                return 0;
            }

            // Get equity withdrawals (financing outflows)
            var equityOutflows = await _context.CapitalTransactions
                .Where(ct => ct.TransactionType == "Withdrawal")
                .SumAsync(ct => (long)ct.Amount, ct);

            _logger.LogInformation("Financing outflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, equityOutflows);
            return equityOutflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating financing outflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Calculate net financing cash flow (inflows - outflows)
    /// </summary>
    public async Task<long> CalculateFinancingCFAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            var inflows = await GetFinancingInflowsAsync(periodId, branchId, ct);
            var outflows = await GetFinancingOutflowsAsync(periodId, branchId, ct);
            
            long netFinancingCF = inflows - outflows;

            _logger.LogInformation("Financing CF calculated for period {PeriodId}: Inflows={Inflows}, Outflows={Outflows}, Net={Net}", 
                periodId, inflows, outflows, netFinancingCF);
            
            return netFinancingCF;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating financing CF for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get all financing items for a cash flow statement
    /// </summary>
    public async Task<List<CashFlowItem>> GetFinancingItemsAsync(
        Guid cashFlowStatementId,
        CancellationToken ct = default)
    {
        try
        {
            var items = await _context.CashFlowItems
                .Where(c => c.CashFlowStatementId == cashFlowStatementId 
                    && c.Category == (int)CashFlowCategory.Financing
                    && !c.IsDeleted)
                .ToListAsync(ct);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financing items for statement {StatementId}", cashFlowStatementId);
            return new List<CashFlowItem>();
        }
    }

    /// <summary>
    /// Add financing cash flow item
    /// </summary>
    public async Task<CashFlowItem> AddFinancingItemAsync(
        Guid cashFlowStatementId,
        string type,
        long amount,
        string description,
        CancellationToken ct = default)
    {
        try
        {
            // Parse type (Inflow=1, Outflow=2)
            var itemType = type.Equals("Inflow", StringComparison.OrdinalIgnoreCase) 
                ? (int)CashFlowItemType.Inflow 
                : (int)CashFlowItemType.Outflow;

            var item = new CashFlowItem
            {
                Id = Guid.NewGuid(),
                CashFlowStatementId = cashFlowStatementId,
                Category = (int)CashFlowCategory.Financing,
                ItemType = itemType,
                Amount = amount,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashFlowItems.Add(item);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Financing CF item added for statement {StatementId}", cashFlowStatementId);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding financing CF item for statement {StatementId}", cashFlowStatementId);
            throw;
        }
    }
}
