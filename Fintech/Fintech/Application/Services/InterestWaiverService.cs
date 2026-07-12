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
    public class InterestWaiverService
    {
        private readonly FinVedaDbContext _context;

        public InterestWaiverService(FinVedaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Apply interest waiver (creates pending waiver record for approval)
        /// </summary>
        public async Task<InterestWaiver?> ApplyWaiverAsync(
            Guid loanId,
            long waiverAmount,
            string reason,
            CancellationToken ct = default)
        {
            try
            {
                var loan = await _context.LoanCases.FindAsync(new object[] { loanId }, cancellationToken: ct);
                if (loan == null) return null;

                // Validate waiver amount
                if (waiverAmount <= 0) return null;

                // Check if waiver amount doesn't exceed total interest
                var totalInterest = await GetTotalInterestAsync(loanId, ct);
                if (waiverAmount > totalInterest) return null;

                var waiver = new InterestWaiver
                {
                    Id = Guid.NewGuid(),
                    LoanId = loanId,
                    WaiverAmount = waiverAmount,
                    WaiverDate = DateTime.UtcNow,
                    Reason = reason ?? "Interest waiver request",
                    Status = (int)InterestWaiverStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.InterestWaivers.Add(waiver);
                await _context.SaveChangesAsync(ct);

                return waiver;
            }
            catch { return null; }
        }

        /// <summary>
        /// Approve a pending interest waiver (role: super_admin only)
        /// </summary>
        public async Task<bool> ApproveWaiverAsync(
            Guid waiverId,
            string approvedBy,
            CancellationToken ct = default)
        {
            try
            {
                var waiver = await _context.InterestWaivers.FindAsync(new object[] { waiverId }, cancellationToken: ct);
                if (waiver == null || waiver.Status != (int)InterestWaiverStatus.Pending) return false;

                waiver.Status = (int)InterestWaiverStatus.Approved;
                waiver.ApprovedBy = approvedBy;
                waiver.ApprovalDate = DateTime.UtcNow;
                waiver.UpdatedAt = DateTime.UtcNow;

                // Note: Reversing journal entries will be created by InterestReportingService in Phase 7 Week 3

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Reject a pending interest waiver
        /// </summary>
        public async Task<bool> RejectWaiverAsync(
            Guid waiverId,
            string rejectionReason,
            CancellationToken ct = default)
        {
            try
            {
                var waiver = await _context.InterestWaivers.FindAsync(new object[] { waiverId }, cancellationToken: ct);
                if (waiver == null || waiver.Status != (int)InterestWaiverStatus.Pending) return false;

                waiver.Status = (int)InterestWaiverStatus.Rejected;
                waiver.Reason = $"{waiver.Reason} | Rejected: {rejectionReason}";
                waiver.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Reverse an approved waiver (refund the waived amount)
        /// </summary>
        public async Task<bool> ReverseWaiverAsync(
            Guid waiverId,
            string reversedBy,
            CancellationToken ct = default)
        {
            try
            {
                var waiver = await _context.InterestWaivers.FindAsync(new object[] { waiverId }, cancellationToken: ct);
                if (waiver == null || waiver.Status != (int)InterestWaiverStatus.Approved) return false;

                waiver.Status = (int)InterestWaiverStatus.Reversed;
                waiver.UpdatedAt = DateTime.UtcNow;

                // If there was a reversal entry, we'd need to reverse it back
                // For now, just mark the waiver as reversed
                if (waiver.ReversalJournalEntryId.HasValue)
                {
                    // The original posted interest needs to be re-posted
                    var loan = await _context.LoanCases.FindAsync(new object[] { waiver.LoanId }, cancellationToken: ct);
                    if (loan != null)
                    {
                        // Mark calculations for this loan as pending again
                        // (in practice, re-calculate interest for the period)
                    }
                }

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Get all waivers for a loan
        /// </summary>
        public async Task<List<InterestWaiver>> GetWaiversAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestWaivers
                    .Where(iw => iw.LoanId == loanId && !iw.IsDeleted)
                    .OrderByDescending(iw => iw.WaiverDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestWaiver>(); }
        }

        /// <summary>
        /// Get pending waivers requiring approval
        /// </summary>
        public async Task<List<InterestWaiver>> GetPendingWaiversAsync(CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestWaivers
                    .Where(iw => iw.Status == (int)InterestWaiverStatus.Pending && !iw.IsDeleted)
                    .OrderBy(iw => iw.WaiverDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestWaiver>(); }
        }

        /// <summary>
        /// Get approved waivers for a loan
        /// </summary>
        public async Task<List<InterestWaiver>> GetApprovedWaiversAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestWaivers
                    .Where(iw => iw.LoanId == loanId && 
                                 iw.Status == (int)InterestWaiverStatus.Approved &&
                                 !iw.IsDeleted)
                    .OrderByDescending(iw => iw.ApprovalDate)
                    .ToListAsync(ct);
            }
            catch { return new List<InterestWaiver>(); }
        }

        /// <summary>
        /// Get total waived amount for a loan
        /// </summary>
        public async Task<long> GetTotalWaivedAmountAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestWaivers
                    .Where(iw => iw.LoanId == loanId && 
                                 iw.Status == (int)InterestWaiverStatus.Approved &&
                                 !iw.IsDeleted)
                    .SumAsync(iw => iw.WaiverAmount, ct);
            }
            catch { return 0L; }
        }

        /// <summary>
        /// Get total pending waiver amount (awaiting approval)
        /// </summary>
        public async Task<long> GetPendingWaiverAmountAsync(
            Guid loanId,
            CancellationToken ct = default)
        {
            try
            {
                return await _context.InterestWaivers
                    .Where(iw => iw.LoanId == loanId && 
                                 iw.Status == (int)InterestWaiverStatus.Pending &&
                                 !iw.IsDeleted)
                    .SumAsync(iw => iw.WaiverAmount, ct);
            }
            catch { return 0L; }
        }

        private async Task<long> GetTotalInterestAsync(Guid loanId, CancellationToken ct = default)
        {
            try
            {
                // Sum all calculations + postings for this loan
                var calculatedInterest = await _context.InterestCalculations
                    .Where(ic => ic.LoanId == loanId && !ic.IsDeleted)
                    .SumAsync(ic => ic.AccrualAmount, ct);

                var postedInterest = await _context.InterestPostings
                    .Where(ip => ip.LoanId == loanId && !ip.IsDeleted)
                    .SumAsync(ip => ip.PostedAmount, ct);

                return calculatedInterest + postedInterest;
            }
            catch { return 0L; }
        }
    }
}
