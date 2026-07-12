using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IReportService
{
    Task<JournalEntry?> GetJournalEntryByCodeAsync(string code);
    Task<JournalLine?> GetJournalLineByCodeAsync(string code);
    Task<object> GetPortfolioAtRiskAsync(DateTime? start, DateTime? end);
    Task<object> GetCollectionEfficiencyAsync(DateTime? start, DateTime? end);
    Task<object> GetTrialBalanceAsync(DateTime? start, DateTime? end);
    Task<object> GetDashboardStatsAsync();
}

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ITenantService _tenantService;

    public ReportService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _tenantService = tenantService;
    }

    public async Task<JournalEntry?> GetJournalEntryByCodeAsync(string code)
    {
        return await _dbContext.JournalEntries
            .FirstOrDefaultAsync(je => je.JournalEntryCode == code && je.BranchId == _tenantService.BranchId);
    }

    public async Task<JournalLine?> GetJournalLineByCodeAsync(string code)
    {
        return await _dbContext.JournalLines
            .FirstOrDefaultAsync(jl => jl.JournalLineCode == code && jl.BranchId == _tenantService.BranchId);
    }

    public async Task<object> GetPortfolioAtRiskAsync(DateTime? start, DateTime? end)
    {
        return new { message = "PAR Report Data" };
    }

    public async Task<object> GetCollectionEfficiencyAsync(DateTime? start, DateTime? end)
    {
        return new { message = "Efficiency Report Data" };
    }

    public async Task<object> GetTrialBalanceAsync(DateTime? start, DateTime? end)
    {
        return new { message = "Trial Balance Report Data" };
    }

    public async Task<object> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow;
        var startOfDay = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var aum = await _dbContext.LoanCases.Where(l => l.Status != LoanStatus.closed).SumAsync(l => l.TotalReceivable);
        var interestLines = await _dbContext.JournalLines.Where(jl => jl.AccountName == "Interest Income" && jl.JournalEntry.Date >= firstDayOfMonth).ToListAsync();
        var expenseLines = await _dbContext.JournalLines.Where(jl => jl.AccountName == "Salaries" && jl.JournalEntry.Date >= firstDayOfMonth).ToListAsync();

        var interestIncome = interestLines.Sum(l => l.Type == "credit" ? l.Amount : -l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Type == "debit" ? l.Amount : -l.Amount);

        var stats = new
        {
            aum = aum,
            totalLoans = await _dbContext.LoanCases.CountAsync(),
            activeLoans = await _dbContext.LoanCases.Where(l => l.Status != LoanStatus.closed).CountAsync(),
            par30 = 3.5, // Logic for PAR would go here
            interestIncomeMTD = interestIncome,
            expensesMTD = totalExpenses,
            netProfitMTD = interestIncome - totalExpenses,
            collectedToday = await _dbContext.Receipts.Where(r => r.CapturedAt >= startOfDay && r.CapturedAt < endOfDay).SumAsync(r => r.AmountPaid),
            
            // Mocking trend for frontend visualization
            aumTrend = new[] {
                new { month = "Jan", aum = aum * 0.9 },
                new { month = "Feb", aum = aum * 0.95 },
                new { month = "Mar", aum = (double)aum }
            },
            
            branchPerformance = await _dbContext.Branches.Select(b => new {
                branch = b.Name,
                aum = _dbContext.LoanCases.Where(l => l.BranchId == b.Id && l.Status != LoanStatus.closed).Sum(l => l.TotalReceivable),
                loans = _dbContext.LoanCases.Where(l => l.BranchId == b.Id).Count(),
                collection = 95.5,
                par = 2.1
            }).ToListAsync()
        };

        return stats;
    }
}
