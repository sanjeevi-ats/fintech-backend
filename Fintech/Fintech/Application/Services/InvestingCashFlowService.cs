using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IInvestingCashFlowService
{
    Task<long> GetInvestingInflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> GetInvestingOutflowsAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<long> CalculateInvestingCFAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<List<CashFlowItem>> GetInvestingItemsAsync(Guid cashFlowStatementId, CancellationToken ct = default);
    Task<CashFlowItem> AddInvestingItemAsync(Guid cashFlowStatementId, string type, long amount, string description, CancellationToken ct = default);
}

public class InvestingCashFlowService : IInvestingCashFlowService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<InvestingCashFlowService> _logger;

    public InvestingCashFlowService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<InvestingCashFlowService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get investing cash inflows (asset sales, loan repayments, etc.)
    /// </summary>
    public async Task<long> GetInvestingInflowsAsync(
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

            // Get receipts for repayments (investing inflows approximated)
            var loanRepayments = await _context.Receipts
                .Where(r => r.BranchId == branchId)
                .SumAsync(r => (long)r.AmountPaid, ct);

            _logger.LogInformation("Investing inflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, loanRepayments);
            return loanRepayments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating investing inflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get investing cash outflows (asset purchases, loan disbursements, etc.)
    /// </summary>
    public async Task<long> GetInvestingOutflowsAsync(
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

            // Get loan disbursements (investing outflows)
            var loanDisbursements = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .SumAsync(l => (long)l.Principal, ct);

            _logger.LogInformation("Investing outflows calculated for period {PeriodId}, branch {BranchId}: {Amount}", periodId, branchId, loanDisbursements);
            return loanDisbursements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating investing outflows for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Calculate net investing cash flow (inflows - outflows)
    /// </summary>
    public async Task<long> CalculateInvestingCFAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            var inflows = await GetInvestingInflowsAsync(periodId, branchId, ct);
            var outflows = await GetInvestingOutflowsAsync(periodId, branchId, ct);
            
            long netInvestingCF = inflows - outflows;

            _logger.LogInformation("Investing CF calculated for period {PeriodId}: Inflows={Inflows}, Outflows={Outflows}, Net={Net}", 
                periodId, inflows, outflows, netInvestingCF);
            
            return netInvestingCF;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating investing CF for period {PeriodId}", periodId);
            return 0;
        }
    }

    /// <summary>
    /// Get all investing items for a cash flow statement
    /// </summary>
    public async Task<List<CashFlowItem>> GetInvestingItemsAsync(
        Guid cashFlowStatementId,
        CancellationToken ct = default)
    {
        try
        {
            var items = await _context.CashFlowItems
                .Where(c => c.CashFlowStatementId == cashFlowStatementId 
                    && c.Category == (int)CashFlowCategory.Investing
                    && !c.IsDeleted)
                .ToListAsync(ct);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investing items for statement {StatementId}", cashFlowStatementId);
            return new List<CashFlowItem>();
        }
    }

    /// <summary>
    /// Add investing cash flow item
    /// </summary>
    public async Task<CashFlowItem> AddInvestingItemAsync(
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
                Category = (int)CashFlowCategory.Investing,
                ItemType = itemType,
                Amount = amount,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashFlowItems.Add(item);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Investing CF item added for statement {StatementId}", cashFlowStatementId);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding investing CF item for statement {StatementId}", cashFlowStatementId);
            throw;
        }
    }
}
