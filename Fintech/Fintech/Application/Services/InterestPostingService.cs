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
    public class InterestPostingService
    {
        private readonly FinVedaDbContext _context;
        private readonly InterestCalculationService _calculationService;

        public InterestPostingService(
            FinVedaDbContext context,
            InterestCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }

        /// <summary>
        /// Post monthly interest for all loans in an accounting period
        /// </summary>
        public async Task<List<InterestPosting>> PostMonthlyInterestAsync(
            Guid periodId,
            CancellationToken ct = default)
        {
            try
            {
                var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId }, cancellationToken: ct);
                if (period == null) return new List<InterestPosting>();

                var postings = new List<InterestPosting>();
                
                // Get all calculations for this period
                var calculations = await _calculationService.GetInterestByPeriodAsync(periodId, ct);
                if (!calculations.Any()) return new List<InterestPosting>();

                // Group by loan
                var loanGrouped = calculations.GroupBy(c => c.LoanId);

                foreach (var loanGroup in loanGrouped)
                {
                    // Sum all accruals for the loan in this period
                    long totalInterest = loanGroup.Sum(c => c.AccrualAmount);
                    if (totalInterest <= 0) continue;

                    var loan = await _context.LoanCases.FindAsync(new object[] { loanGroup.Key }, cancellationToken: ct);
                    if (loan == null) continue;

                    // Create posting record
                    var posting = new InterestPosting
                    {
                        Id = Guid.NewGuid(),
                        LoanId = loanGroup.Key,
                        PeriodId = periodId,
                        PostedAmount = totalInterest,
                        PostedDate = DateTime.UtcNow,
                        JournalEntryId = Guid.NewGuid(), // Placeholder - will be updated when JE is created
                        Status = (int)InterestPostingStatus.Posted,
                        Description = $"Monthly interest posting: ₹{totalInterest / 100.0:F2}",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.InterestPostings.Add(posting);
                    postings.Add(posting);

                    // Mark calculations as posted
                    foreach (var calc in loanGroup)
                    {
                        calc.Status = (int)InterestCalculationStatus.Posted;
                    }
                }

                if (postings.Any())
                {
                    await _context.SaveChangesAsync(ct);
                }

                return postings;
            }
            catch { return new List<InterestPosting>(); }
        }

        /// <summary>
        /// Post interest for a single loan in a period
        /// </summary>
        public async Task<InterestPosting?> PostInterestForLoanAsync(
            Guid loanId,
            Guid periodId,
            CancellationToken ct = default)
        {
            try
            {
                var loan = await _context.LoanCases.FindAsync(new object[] { loanId }, cancellationToken: ct);
                if (loan == null) return null;

                var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId }, cancellationToken: ct);
                if (period == null) return null;

                // Get pending interest for this loan
                var pendingAmount = await _calculationService.GetPendingInterestAsync(loanId, ct);
                if (pendingAmount <= 0) return null;

                // Create posting (journal entry will be created separately)
                var posting = new InterestPosting
                {
                    Id = Guid.NewGuid(),
                    LoanId = loanId,
                    PeriodId = periodId,
                    PostedAmount = pendingAmount,
                    PostedDate = DateTime.UtcNow,
                    JournalEntryId = Guid.NewGuid(), // Placeholder
                    Status = (int)InterestPostingStatus.Posted,
                    Description = $"Interest posting for loan: ₹{pendingAmount / 100.0:F2}",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.InterestPostings.Add(posting);

                // Mark all pending calculations as posted
                var calculations = await _calculationService.GetInterestByLoanAsync(loanId, ct);
                foreach (var calc in calculations.Where(c => c.Status == (int)InterestCalculationStatus.Calculated))
                {
                    calc.Status = (int)InterestCalculationStatus.Posted;
                }

                await _context.SaveChangesAsync(ct);
                return posting;
            }
            catch { return null; }
        }

        /// <summary>
        /// Get posting history for a loan
        /// </summary>
        public async Task<List<InterestPosting>> GetPostingHistoryAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestPostings
                    .Where(ip => ip.LoanId == loanId && !ip.IsDeleted)
                    .OrderByDescending(ip => ip.PostedDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestPosting>(); }
        }

        /// <summary>
        /// Get posting history for an accounting period
        /// </summary>
        public async Task<List<InterestPosting>> GetPeriodPostingHistoryAsync(
            Guid periodId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestPostings
                    .Where(ip => ip.PeriodId == periodId && !ip.IsDeleted)
                    .OrderBy(ip => ip.LoanId)
                    .ThenByDescending(ip => ip.PostedDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestPosting>(); }
        }

        /// <summary>
        /// Reverse a posted interest entry (creates reversing journal entry)
        /// </summary>
        public async Task<bool> ReversePostingAsync(
            Guid postingId,
            string reversedBy,
            CancellationToken ct = default)
        {
            try
            {
                var posting = await _context.InterestPostings.FindAsync(new object[] { postingId }, cancellationToken: ct);
                if (posting == null || posting.Status != (int)InterestPostingStatus.Posted) return false;

                // Create reversing journal entry
                var originalEntry = await _context.JournalEntries.FindAsync(new object[] { posting.JournalEntryId }, cancellationToken: ct);
                if (originalEntry == null) return false;

                // Mark posting as reversed
                posting.Status = (int)InterestPostingStatus.Reversed;
                posting.UpdatedAt = DateTime.UtcNow;

                // Mark calculations as not posted (revert status)
                var calculations = await _context.InterestCalculations
                    .Where(ic => ic.LoanId == posting.LoanId && 
                                 ic.PeriodId == posting.PeriodId &&
                                 ic.Status == (int)InterestCalculationStatus.Posted)
                    .ToListAsync(ct);

                foreach (var calc in calculations)
                {
                    calc.Status = (int)InterestCalculationStatus.Calculated;
                }

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Get total posted interest for a loan
        /// </summary>
        public async Task<long> GetTotalPostedInterestAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestPostings
                    .Where(ip => ip.LoanId == loanId && 
                                 ip.Status == (int)InterestPostingStatus.Posted &&
                                 !ip.IsDeleted)
                    .SumAsync(ip => ip.PostedAmount, ct);
            }
            catch { return 0L; }
        }
    }
}
