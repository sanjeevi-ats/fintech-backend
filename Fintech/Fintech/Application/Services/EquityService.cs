using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Fintech.Application.Services;

public interface IEquityService
{
    Task<ProfitDistribution?> GetByCodeAsync(string code);
    Task DistributeProfitAsync(DateTime startDate, DateTime endDate, Dictionary<Guid, decimal> partnerEquityPct, Guid branchId);
}

public class EquityService : IEquityService
{
    private readonly IAccountingService _accountingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;

    public EquityService(
        IAccountingService accountingService,
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService)
    {
        _accountingService = accountingService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
    }

    public async Task<ProfitDistribution?> GetByCodeAsync(string code)
    {
        return await _dbContext.ProfitDistributions
            .FirstOrDefaultAsync(p => p.ProfitDistributionCode == code);
    }

    public async Task DistributeProfitAsync(DateTime startDate, DateTime endDate, Dictionary<Guid, decimal> partnerEquityPct, Guid branchId)
    {
        long netProfit = await _accountingService.GetProfitAndLossAsync(startDate, endDate);
        if (netProfit <= 0) return; 

        if (partnerEquityPct.Values.Sum() != 1.0m)
        {
            throw new FinVedaException(422, "INVALID_EQUITY", "Total equity distribution percentage must equal 100% (1.0).");
        }

        var je = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            PublicId = "JE-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            Date = DateTime.UtcNow,
            Description = $"Profit Distribution for period {startDate.ToShortDateString()} - {endDate.ToShortDateString()}",
            IsManual = false,
            IsPosted = true
        };

        string retainedEarningsName = "Retained Earnings";
        je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = branchId, JournalEntryId = je.Id, AccountName = retainedEarningsName, Type = "debit", Amount = netProfit });

        foreach (var partner in partnerEquityPct)
        {
            long share = (long)(netProfit * partner.Value);
            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = branchId, JournalEntryId = je.Id, AccountName = $"Partner_{partner.Key}", Type = "credit", Amount = share });
        }

        await _unitOfWork.Repository<JournalEntry>().AddAsync(je);
        await _unitOfWork.CompleteAsync();
    }
}
