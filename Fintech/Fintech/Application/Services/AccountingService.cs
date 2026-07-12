using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class TrialBalanceResult
{
    public string AccountName { get; set; } = string.Empty;
    public long Balance { get; set; }
}

public interface IAccountingService
{
    Task<Account?> GetByCodeAsync(string code);
    Task<List<TrialBalanceResult>> GetTrialBalanceAsync();
    Task CloseDayAsync(DateTime date, long cashInHandVerified);
    Task<long> GetProfitAndLossAsync(DateTime startDate, DateTime endDate);
}

public class AccountingService : IAccountingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public AccountingService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<Account?> GetByCodeAsync(string code)
    {
        return await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountCode == code && a.BranchId == _tenantService.BranchId);
    }

    public async Task<List<TrialBalanceResult>> GetTrialBalanceAsync()
    {
        var trialBalance = await _dbContext.JournalLines
            .GroupBy(jl => jl.AccountName)
            .Select(g => new TrialBalanceResult
            {
                AccountName = g.Key,
                Balance = g.Sum(x => x.Type == "debit" ? x.Amount : -x.Amount)
            })
            .ToListAsync();

        return trialBalance;
    }

    public async Task CloseDayAsync(DateTime date, long cashInHandVerified)
    {
        var entries = await _dbContext.JournalEntries
            .Include(je => je.Lines)
            .Where(je => je.Date.Date == date.Date)
            .ToListAsync();

        foreach (var je in entries)
        {
            long debits = je.Lines.Where(l => l.Type == "debit").Sum(l => l.Amount);
            long credits = je.Lines.Where(l => l.Type == "credit").Sum(l => l.Amount);

            if (debits != credits)
            {
                throw new FinVedaException(422, "UNBALANCED_ENTRY", $"Journal Entry {je.PublicId} is unbalanced. Debits: {debits}, Credits: {credits}");
            }
        }

        // Calculate EOD stats
        long totalCollected = await _dbContext.Receipts
            .Where(r => r.CapturedAt.Date == date.Date)
            .SumAsync(r => r.AmountPaid);

        long discrepancy = cashInHandVerified - totalCollected;

        var dayEnd = new DayEnd
        {
            Id = Guid.NewGuid(),
            BranchId = entries.FirstOrDefault()?.BranchId ?? Guid.Empty,
            Date = date,
            IsClosed = true,
            JournalsLocked = entries.Count,
            TotalCollected = totalCollected,
            Discrepancy = discrepancy,
            DiscrepancyResolved = (discrepancy == 0)
        };

        await _dbContext.DayEnds.AddAsync(dayEnd);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<long> GetProfitAndLossAsync(DateTime startDate, DateTime endDate)
    {
        var incomeAccounts = new[] { "Interest Income", "Fee Income" };
        var expenseAccounts = new[] { "Salaries" }; 

        var pnlLines = await _dbContext.JournalLines
            .Include(jl => jl.JournalEntry)
            .Where(jl => jl.JournalEntry.Date >= startDate && jl.JournalEntry.Date <= endDate)
            .ToListAsync();

        long totalIncome = pnlLines
            .Where(jl => incomeAccounts.Contains(jl.AccountName))
            .Sum(jl => jl.Type == "credit" ? jl.Amount : -jl.Amount); 

        long totalExpense = pnlLines
            .Where(jl => expenseAccounts.Contains(jl.AccountName))
            .Sum(jl => jl.Type == "debit" ? jl.Amount : -jl.Amount); 

        return totalIncome - totalExpense;
    }
}
