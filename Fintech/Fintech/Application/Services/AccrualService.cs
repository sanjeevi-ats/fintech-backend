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
    public class AccrualService
    {
        private readonly FinVedaDbContext _context;

        public AccrualService(FinVedaDbContext context)
        {
            _context = context;
        }

        public async Task<List<AccrualEntry>> AccrueInterestAsync(Guid periodId, DateTime periodEndDate, CancellationToken ct = default)
        {
            try
            {
                var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId }, cancellationToken: ct);
                if (period == null) return new List<AccrualEntry>();

                var accruals = new List<AccrualEntry>();
                var loans = await _context.LoanCases.ToListAsync(ct);

                foreach (var loan in loans)
                {
                    if (loan.InterestAmount <= 0) continue;
                    
                    var accrualAmount = loan.InterestAmount / 12; // Monthly accrual
                    if (accrualAmount <= 0) continue;

                    var accrualEntry = new AccrualEntry
                    {
                        Id = Guid.NewGuid(),
                        PeriodId = periodId,
                        LoanId = loan.Id,
                        Type = 1,
                        Amount = accrualAmount,
                        JournalEntryId = Guid.NewGuid(),
                        AccrualDate = periodEndDate,
                        Description = $"Interest accrual for loan {loan.LoanCode}",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _context.AccrualEntries.Add(accrualEntry);
                    accruals.Add(accrualEntry);
                }

                await _context.SaveChangesAsync(ct);
                return accruals;
            }
            catch { return new List<AccrualEntry>(); }
        }

        public async Task<long> CalculateDailyAccrualAsync(Guid loanId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            return await Task.FromResult(0L);
        }

        public async Task<List<AccrualEntry>> GetAccrualsAsync(Guid periodId, CancellationToken ct = default)
        {
            try
            {
                return await _context.AccrualEntries
                    .Where(a => a.PeriodId == periodId && !a.IsDeleted)
                    .ToListAsync(ct);
            }
            catch
            {
                return new List<AccrualEntry>();
            }
        }
    }

    public enum AccrualType { InterestAccrual = 1, BankInterest = 2, ProvisionAccrual = 3 }
}

