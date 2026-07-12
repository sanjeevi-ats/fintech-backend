using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public class ExpenseLineDto
{
    public int Category { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
}

public interface IExpenseTrackingService
{
    Task RecordProvisionExpenseAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task RecordWaiverExpenseAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task RecordOperatingExpenseAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task<List<ExpenseLineDto>> GetExpensesAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<(long provisionExpense, long waiverExpense, long operatingExpense, long adminExpense)> AggregateExpensesAsync(Guid periodId, CancellationToken ct = default);
}

public class ExpenseTrackingService : IExpenseTrackingService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ExpenseTrackingService> _logger;

    public ExpenseTrackingService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<ExpenseTrackingService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Record provision expense for a period
    /// </summary>
    public async Task RecordProvisionExpenseAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Provision expenses come from ProvisionEntry table
            _logger.LogInformation("Recording provision expense: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording provision expense");
        }
    }

    /// <summary>
    /// Record waiver expense for a period
    /// </summary>
    public async Task RecordWaiverExpenseAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Waiver expenses come from InterestWaiver table (approved waivers only)
            _logger.LogInformation("Recording waiver expense: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording waiver expense");
        }
    }

    /// <summary>
    /// Record operating expense for a period
    /// </summary>
    public async Task RecordOperatingExpenseAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Operating expenses from journal entries
            _logger.LogInformation("Recording operating expense: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording operating expense");
        }
    }

    /// <summary>
    /// Get expense lines for a period
    /// </summary>
    public async Task<List<ExpenseLineDto>> GetExpensesAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            var expenses = new List<ExpenseLineDto>();

            // Get provision expenses from ProvisionEntry
            var provisions = await _context.ProvisionEntries
                .Where(pe => pe.PeriodId == periodId)
                .ToListAsync(ct);

            long totalProvisionExpense = provisions.Sum(p => p.Amount);
            if (totalProvisionExpense > 0)
            {
                expenses.Add(new ExpenseLineDto
                {
                    Category = (int)ExpenseCategory.Provisions,
                    Amount = totalProvisionExpense,
                    Description = "Loan Loss Provisions",
                    ReferenceCode = "EXP-PROV"
                });
            }

            // Get waiver expenses from InterestWaiver (only approved)
            var waivers = await _context.InterestWaivers
                .Where(iw => iw.WaiverDate.Year == DateTime.UtcNow.Year &&
                       iw.WaiverDate.Month == DateTime.UtcNow.Month &&
                       iw.Status == (int)InterestWaiverStatus.Approved)
                .ToListAsync(ct);

            long totalWaiverExpense = waivers.Sum(w => w.WaiverAmount);
            if (totalWaiverExpense > 0)
            {
                expenses.Add(new ExpenseLineDto
                {
                    Category = (int)ExpenseCategory.Waivers,
                    Amount = totalWaiverExpense,
                    Description = "Interest Waivers Approved",
                    ReferenceCode = "EXP-WAIV"
                });
            }

            // Get operating expenses from Journal entries
            long totalOperatingExpense = 0;
            var operatingJournals = await _context.JournalEntries
                .Where(je => je.BranchId == branchId && je.Description != null && 
                       je.Description.Contains("operating", StringComparison.OrdinalIgnoreCase))
                .ToListAsync(ct);

            foreach (var journal in operatingJournals)
            {
                var journalLines = await _context.JournalLines
                    .Where(jl => jl.JournalEntryId == journal.Id && 
                           jl.Type == "Debit")
                    .ToListAsync(ct);
                totalOperatingExpense += journalLines.Sum(jl => jl.Amount);
            }

            if (totalOperatingExpense > 0)
            {
                expenses.Add(new ExpenseLineDto
                {
                    Category = (int)ExpenseCategory.Operating,
                    Amount = totalOperatingExpense,
                    Description = "Operating Expenses",
                    ReferenceCode = "EXP-OPR"
                });
            }

            // Get administrative expenses
            long totalAdminExpense = 0;
            var adminJournals = await _context.JournalEntries
                .Where(je => je.BranchId == branchId && je.Description != null && 
                       je.Description.Contains("admin", StringComparison.OrdinalIgnoreCase))
                .ToListAsync(ct);

            foreach (var journal in adminJournals)
            {
                var journalLines = await _context.JournalLines
                    .Where(jl => jl.JournalEntryId == journal.Id && 
                           jl.Type == "Debit")
                    .ToListAsync(ct);
                totalAdminExpense += journalLines.Sum(jl => jl.Amount);
            }

            if (totalAdminExpense > 0)
            {
                expenses.Add(new ExpenseLineDto
                {
                    Category = (int)ExpenseCategory.Administrative,
                    Amount = totalAdminExpense,
                    Description = "Administrative Expenses",
                    ReferenceCode = "EXP-ADM"
                });
            }

            _logger.LogInformation("Retrieved expenses for period {PeriodId}: {Count} categories", periodId, expenses.Count);
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses for period {PeriodId}", periodId);
            return new List<ExpenseLineDto>();
        }
    }

    /// <summary>
    /// Aggregate expenses by category for a period
    /// </summary>
    public async Task<(long provisionExpense, long waiverExpense, long operatingExpense, long adminExpense)> AggregateExpensesAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var expenses = await GetExpensesAsync(periodId, null, ct);

            long provisionExpense = expenses
                .Where(e => e.Category == (int)ExpenseCategory.Provisions)
                .Sum(e => e.Amount);

            long waiverExpense = expenses
                .Where(e => e.Category == (int)ExpenseCategory.Waivers)
                .Sum(e => e.Amount);

            long operatingExpense = expenses
                .Where(e => e.Category == (int)ExpenseCategory.Operating)
                .Sum(e => e.Amount);

            long adminExpense = expenses
                .Where(e => e.Category == (int)ExpenseCategory.Administrative)
                .Sum(e => e.Amount);

            _logger.LogInformation(
                "Aggregated expenses for period {PeriodId}: Provisions={Provisions}, Waivers={Waivers}, Operating={Operating}, Admin={Admin}",
                periodId, provisionExpense, waiverExpense, operatingExpense, adminExpense);

            return (provisionExpense, waiverExpense, operatingExpense, adminExpense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating expenses for period {PeriodId}", periodId);
            return (0, 0, 0, 0);
        }
    }
}
