using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IOperatingCashFlowService
{
    Task<long> GetOperatingInflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> GetOperatingOutflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> CalculateOperatingCFAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<List<CashFlowItem>> GetOperatingItemsAsync(Guid cashFlowStatementId, CancellationToken ct = default);
    Task<CashFlowItem> AddOperatingItemAsync(Guid cashFlowStatementId, string type, long amount, string description, CancellationToken ct = default);
}

public class OperatingCashFlowService : IOperatingCashFlowService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<OperatingCashFlowService> _logger;

    public OperatingCashFlowService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<OperatingCashFlowService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get operating cash inflows (collections, receipts, etc.)
    /// </summary>
    public async Task<long> GetOperatingInflowsAsync(
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

            // Get receipts (collections)
            var inflows = await _context.Receipts
                .Where(r => r.BranchId == branchId 
                    && r.CapturedAt >= period.StartDate 
                    && r.CapturedAt <= period.EndDate)
                .SumAsync(r => (long)r.AmountPaid, ct);

            _logger.LogInformation("Operating inflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, inflows);
            return inflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating operating inflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get operating cash outflows (operating expenses, salaries, etc.)
    /// </summary>
    public async Task<long> GetOperatingOutflowsAsync(
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

            // Get operating expenses from journal lines
            var outflows = await _context.JournalLines
                .Include(jl => jl.JournalEntry)
                .Where(jl => jl.JournalEntry.BranchId == branchId
                    && jl.JournalEntry.Date >= period.StartDate
                    && jl.JournalEntry.Date <= period.EndDate
                    && jl.JournalEntry.Description.Contains("Operating", StringComparison.OrdinalIgnoreCase)
                    && jl.Type == "Debit")
                .SumAsync(jl => (long)jl.Amount, ct);

            _logger.LogInformation("Operating outflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, outflows);
            return outflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating operating outflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Calculate net operating cash flow (inflows - outflows)
    /// </summary>
    public async Task<long> CalculateOperatingCFAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            var inflows = await GetOperatingInflowsAsync(periodId, branchId, ct);
            var outflows = await GetOperatingOutflowsAsync(periodId, branchId, ct);
            
            long netOperatingCF = inflows - outflows;

            _logger.LogInformation("Operating CF calculated for period {PeriodId}: Inflows={Inflows}, Outflows={Outflows}, Net={Net}", 
                periodId, inflows, outflows, netOperatingCF);
            
            return netOperatingCF;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating operating CF for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get all operating items for a cash flow statement
    /// </summary>
    public async Task<List<CashFlowItem>> GetOperatingItemsAsync(
        Guid cashFlowStatementId,
        CancellationToken ct = default)
    {
        try
        {
            var items = await _context.CashFlowItems
                .Where(c => c.CashFlowStatementId == cashFlowStatementId 
                    && c.Category == (int)CashFlowCategory.Operating
                    && !c.IsDeleted)
                .ToListAsync(ct);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operating items for statement {StatementId}", cashFlowStatementId);
            return new List<CashFlowItem>();
        }
    }

    /// <summary>
    /// Add operating cash flow item
    /// </summary>
    public async Task<CashFlowItem> AddOperatingItemAsync(
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
                Category = (int)CashFlowCategory.Operating,
                ItemType = itemType,
                Amount = amount,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashFlowItems.Add(item);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Operating CF item added for statement {StatementId}", cashFlowStatementId);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding operating CF item for statement {StatementId}", cashFlowStatementId);
            throw;
        }
    }
}
