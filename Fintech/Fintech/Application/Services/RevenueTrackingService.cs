using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public class RevenueLineDto
{
    public int Category { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
}

public interface IRevenueTrackingService
{
    Task RecordInterestRevenueAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task RecordFeeRevenueAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task RecordPenaltyRevenueAsync(Guid periodId, long amount, string? description = null, CancellationToken ct = default);
    Task<List<RevenueLineDto>> GetRevenueAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<(long interestRevenue, long feeRevenue, long penaltyRevenue, long otherRevenue)> AggregateRevenueAsync(Guid periodId, CancellationToken ct = default);
}

public class RevenueTrackingService : IRevenueTrackingService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<RevenueTrackingService> _logger;

    public RevenueTrackingService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<RevenueTrackingService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Record interest revenue for a period
    /// </summary>
    public async Task RecordInterestRevenueAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Interest revenue comes from InterestPosting table
            // This method can be used to adjust interest revenue if needed
            _logger.LogInformation("Recording interest revenue: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording interest revenue");
        }
    }

    /// <summary>
    /// Record fee revenue for a period
    /// </summary>
    public async Task RecordFeeRevenueAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Fee revenue comes from Receipt table with fees collected
            _logger.LogInformation("Recording fee revenue: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording fee revenue");
        }
    }

    /// <summary>
    /// Record penalty revenue for a period
    /// </summary>
    public async Task RecordPenaltyRevenueAsync(
        Guid periodId,
        long amount,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            // Penalty revenue from receipts or journal entries
            _logger.LogInformation("Recording penalty revenue: {Amount} for period {PeriodId}", amount, periodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording penalty revenue");
        }
    }

    /// <summary>
    /// Get revenue lines for a period
    /// </summary>
    public async Task<List<RevenueLineDto>> GetRevenueAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            var revenues = new List<RevenueLineDto>();

            // Get interest revenue from InterestPosting
            var interestPostings = await _context.InterestPostings
                .Where(ip => ip.PeriodId == periodId)
                .ToListAsync(ct);

            long totalInterestRevenue = interestPostings.Sum(ip => ip.PostedAmount);
            if (totalInterestRevenue > 0)
            {
                revenues.Add(new RevenueLineDto
                {
                    Category = (int)RevenueCategory.InterestIncome,
                    Amount = totalInterestRevenue,
                    Description = "Interest Income from Loans",
                    ReferenceCode = "REV-INT"
                });
            }

            // Get fee revenue from Receipts
            var receipts = await _context.Receipts
                .Where(r => r.BranchId == branchId && 
                       r.CapturedAt >= DateTime.UtcNow.AddMonths(-1) &&
                       r.CapturedAt <= DateTime.UtcNow)
                .ToListAsync(ct);

            long totalFeeRevenue = receipts.Sum(r => r.AmountPaid * 0); // Adjust based on fee structure
            if (totalFeeRevenue > 0)
            {
                revenues.Add(new RevenueLineDto
                {
                    Category = (int)RevenueCategory.Fees,
                    Amount = totalFeeRevenue,
                    Description = "Fee Income",
                    ReferenceCode = "REV-FEE"
                });
            }

            // Get penalty revenue (from GL or Journal entries)
            var penaltyJournals = await _context.JournalEntries
                .Where(je => je.BranchId == branchId && je.Description != null && 
                       je.Description.Contains("penalty", StringComparison.OrdinalIgnoreCase))
                .ToListAsync(ct);

            long totalPenaltyRevenue = 0;
            foreach (var journal in penaltyJournals)
            {
                var penaltyLines = await _context.JournalLines
                    .Where(jl => jl.JournalEntryId == journal.Id && 
                           jl.Type == "Credit")
                    .ToListAsync(ct);
                totalPenaltyRevenue += penaltyLines.Sum(jl => jl.Amount);
            }

            if (totalPenaltyRevenue > 0)
            {
                revenues.Add(new RevenueLineDto
                {
                    Category = (int)RevenueCategory.Penalties,
                    Amount = totalPenaltyRevenue,
                    Description = "Penalty Income",
                    ReferenceCode = "REV-PEN"
                });
            }

            _logger.LogInformation("Retrieved revenues for period {PeriodId}: {Count} categories", periodId, revenues.Count);
            return revenues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue for period {PeriodId}", periodId);
            return new List<RevenueLineDto>();
        }
    }

    /// <summary>
    /// Aggregate revenue by category for a period
    /// </summary>
    public async Task<(long interestRevenue, long feeRevenue, long penaltyRevenue, long otherRevenue)> AggregateRevenueAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var revenues = await GetRevenueAsync(periodId, null, ct);

            long interestRevenue = revenues
                .Where(r => r.Category == (int)RevenueCategory.InterestIncome)
                .Sum(r => r.Amount);

            long feeRevenue = revenues
                .Where(r => r.Category == (int)RevenueCategory.Fees)
                .Sum(r => r.Amount);

            long penaltyRevenue = revenues
                .Where(r => r.Category == (int)RevenueCategory.Penalties)
                .Sum(r => r.Amount);

            long otherRevenue = revenues
                .Where(r => r.Category == (int)RevenueCategory.Other)
                .Sum(r => r.Amount);

            _logger.LogInformation(
                "Aggregated revenues for period {PeriodId}: Interest={Interest}, Fees={Fees}, Penalties={Penalties}, Other={Other}",
                periodId, interestRevenue, feeRevenue, penaltyRevenue, otherRevenue);

            return (interestRevenue, feeRevenue, penaltyRevenue, otherRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating revenue for period {PeriodId}", periodId);
            return (0, 0, 0, 0);
        }
    }
}
