using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services
{
    public class InterestCalculationService
    {
        private readonly FinVedaDbContext _context;

        public InterestCalculationService(FinVedaDbContext context)
        {
            _context = context;
        }

        public async Task<InterestCalculation?> CalculateDailyInterestAsync(
            Guid loanId, 
            decimal annualRate, 
            DateTime calculationDate,
            int interestType = 1,
            CancellationToken ct = default)
        {
            try
            {
                var loan = await _context.LoanCases.FindAsync(new object[] { loanId }, cancellationToken: ct);
                if (loan == null) return null;

                // Calculate daily rate
                decimal dailyRate = annualRate / 365;
                
                // Calculate daily accrual amount in Paise
                // Using remaining balance for declining interest or principal for fixed
                long balance = interestType == (int)InterestType.Declining 
                    ? loan.Principal - GetPaidAmount(loanId)
                    : loan.Principal;
                
                if (balance <= 0) return null;

                // Daily interest = (Balance × Annual Rate / 365)
                long accrualAmount = (long)(balance * (double)annualRate / 365);

                if (accrualAmount <= 0) return null;

                var calculation = new InterestCalculation
                {
                    Id = Guid.NewGuid(),
                    LoanId = loanId,
                    DailyRate = dailyRate,
                    AccrualAmount = accrualAmount,
                    CalculationDate = calculationDate,
                    Status = (int)InterestCalculationStatus.Calculated,
                    InterestType = interestType,
                    Description = $"Daily interest calculation: ₹{accrualAmount / 100.0:F2}",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.InterestCalculations.Add(calculation);
                await _context.SaveChangesAsync(ct);

                return calculation;
            }
            catch { return null; }
        }

        /// <summary>
        /// Calculate interest for all active loans in a period
        /// </summary>
        public async Task<List<InterestCalculation>> CalculatePeriodInterestAsync(
            Guid periodId,
            decimal defaultAnnualRate = 0.10m,
            CancellationToken ct = default)
        {
            try
            {
                var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId }, cancellationToken: ct);
                if (period == null) return new List<InterestCalculation>();

                var calculations = new List<InterestCalculation>();
                var activeLoans = await _context.LoanCases
                    .Where(l => l.Status == LoanStatus.active)
                    .ToListAsync(ct);

                foreach (var loan in activeLoans)
                {
                    var calculation = await CalculateDailyInterestAsync(
                        loan.Id,
                        defaultAnnualRate,
                        period.EndDate,
                        (int)InterestType.Fixed,
                        ct);

                    if (calculation != null)
                    {
                        calculation.PeriodId = periodId;
                        calculations.Add(calculation);
                    }
                }

                if (calculations.Any())
                {
                    await _context.SaveChangesAsync(ct);
                }

                return calculations;
            }
            catch { return new List<InterestCalculation>(); }
        }

        /// <summary>
        /// Get all interest calculations for a loan
        /// </summary>
        public async Task<List<InterestCalculation>> GetInterestByLoanAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestCalculations
                    .Where(ic => ic.LoanId == loanId && !ic.IsDeleted)
                    .OrderByDescending(ic => ic.CalculationDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestCalculation>(); }
        }

        /// <summary>
        /// Get interest calculations for a period
        /// </summary>
        public async Task<List<InterestCalculation>> GetInterestByPeriodAsync(
            Guid periodId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestCalculations
                    .Where(ic => ic.PeriodId == periodId && !ic.IsDeleted)
                    .OrderBy(ic => ic.LoanId)
                    .ThenByDescending(ic => ic.CalculationDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestCalculation>(); }
        }

        /// <summary>
        /// Get total pending interest for a loan
        /// </summary>
        public async Task<long> GetPendingInterestAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                // Sum all calculated interest that hasn't been posted yet
                var pendingInterest = await _context.InterestCalculations
                    .Where(ic => ic.LoanId == loanId && 
                                 ic.Status == (int)InterestCalculationStatus.Calculated &&
                                 !ic.IsDeleted)
                    .SumAsync(ic => ic.AccrualAmount, ct);

                return pendingInterest;
            }
            catch { return 0L; }
        }

        /// <summary>
        /// Get interest summary for a loan
        /// </summary>
        public async Task<InterestSummaryDto> GetInterestSummaryAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                var calculations = await GetInterestByLoanAsync(loanId, ct);
                var postings = await _context.InterestPostings
                    .Where(ip => ip.LoanId == loanId && !ip.IsDeleted)
                    .ToListAsync(ct);
                var waivers = await _context.InterestWaivers
                    .Where(iw => iw.LoanId == loanId && !iw.IsDeleted)
                    .ToListAsync(ct);

                long totalCalculated = calculations.Sum(c => c.AccrualAmount);
                long totalPosted = postings.Sum(p => p.PostedAmount);
                long totalWaived = waivers.Where(w => w.Status == (int)InterestWaiverStatus.Approved)
                    .Sum(w => w.WaiverAmount);
                long pending = totalCalculated - totalPosted - totalWaived;

                return new InterestSummaryDto
                {
                    LoanId = loanId,
                    TotalCalculated = totalCalculated,
                    TotalPosted = totalPosted,
                    TotalWaived = totalWaived,
                    PendingInterest = pending,
                    CalculationCount = calculations.Count,
                    PostingCount = postings.Count,
                    WaiverCount = waivers.Count
                };
            }
            catch { return new InterestSummaryDto { LoanId = loanId }; }
        }

        private long GetPaidAmount(Guid loanId)
        {
            // Get total collected receipts for this loan
            try
            {
                var paid = _context.Receipts
                    .Where(r => r.LoanCaseId == loanId)
                    .Sum(r => r.AmountPaid);
                return paid;
            }
            catch
            {
                return 0L;
            }
        }
    }

    public class InterestSummaryDto
    {
        public Guid LoanId { get; set; }
        public long TotalCalculated { get; set; }
        public long TotalPosted { get; set; }
        public long TotalWaived { get; set; }
        public long PendingInterest { get; set; }
        public int CalculationCount { get; set; }
        public int PostingCount { get; set; }
        public int WaiverCount { get; set; }
    }
}
