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
    public class ProvisionService
    {
        private readonly FinVedaDbContext _context;

        public ProvisionService(FinVedaDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProvisionEntry>> CalculateDoubtfulDebtProvisionAsync(Guid periodId, DateTime periodEndDate, CancellationToken ct = default)
        {
            try
            {
                var period = await _context.AccountingPeriods.FindAsync(new object[] { periodId }, cancellationToken: ct);
                if (period == null)
                    return new List<ProvisionEntry>();

                var provisions = new List<ProvisionEntry>();
                var loans = await _context.LoanCases.ToListAsync(ct);

                foreach (var loan in loans)
                {
                    if (loan.InterestAmount <= 0) continue;

                    var provisionAmount = loan.InterestAmount / 10; // 10% provision
                    var provisionEntry = new ProvisionEntry
                    {
                        Id = Guid.NewGuid(),
                        PeriodId = periodId,
                        LoanId = loan.Id,
                        Type = 1,
                        Amount = provisionAmount,
                        PreviousAmount = 0,
                        JournalEntryId = Guid.NewGuid(),
                        Status = 1,
                        CalculationMethod = "Age-based",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.ProvisionEntries.Add(provisionEntry);
                    provisions.Add(provisionEntry);
                }

                await _context.SaveChangesAsync(ct);
                return provisions;
            }
            catch
            {
                return new List<ProvisionEntry>();
            }
        }

        public async Task<List<ProvisionEntry>> CalculateLoanLossProvisionAsync(Guid periodId, CancellationToken ct = default)
        {
            try
            {
                return await Task.FromResult(new List<ProvisionEntry>());
            }
            catch
            {
                return new List<ProvisionEntry>();
            }
        }

        public async Task<bool> ReleaseProvisionAsync(Guid loanId, CancellationToken ct = default)
        {
            try
            {
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProvisionEntry>> GetProvisionsAsync(Guid periodId, CancellationToken ct = default)
        {
            try
            {
                return await _context.ProvisionEntries
                    .Where(p => p.PeriodId == periodId && !p.IsDeleted)
                    .ToListAsync(ct);
            }
            catch
            {
                return new List<ProvisionEntry>();
            }
        }
    }

    public enum ProvisionType { DoubtfulDebt = 1, LoanLoss = 2 }
    public enum ProvisionStatus { Active = 1, Released = 2 }
}
