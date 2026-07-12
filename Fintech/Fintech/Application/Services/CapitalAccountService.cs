using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface ICapitalAccountService
{
    // Existing methods
    Task<CapitalAccount?> GetByCodeAsync(string code);
    Task<CapitalTransaction> AddInvestmentAsync(Guid partnerId, long amount, string paymentMode, string remarks, Guid createdBy);
    Task<CapitalTransaction> WithdrawAsync(Guid partnerId, long amount, string paymentMode, string remarks, Guid createdBy, bool isImmediate);
    Task<PartnerCapitalSummaryDto?> GetSummaryAsync(Guid partnerId);
    
    // Phase 4 New Methods
    Task<List<CapitalAccount>> GetAllAccountsAsync();
    Task<CapitalAccount?> GetAccountByIdAsync(Guid accountId);
    Task<CapitalAccount> CreateAccountAsync(Guid partnerId, long openingBalance, Guid createdBy);
    Task<CapitalTransaction> RecordTransactionAsync(Guid accountId, string type, long amount, DateTime transactionDate, string description, string? referenceNumber, Guid createdBy);
    Task<CapitalTransaction?> ApproveTransactionAsync(Guid transactionId, Guid approvedBy);
    Task<CapitalTransaction?> RejectTransactionAsync(Guid transactionId, Guid rejectedBy);
    Task<List<CapitalTransaction>> GetTransactionsAsync(Guid accountId);
    Task RecalculateOwnershipPercentagesAsync();
    Task<decimal> GetOwnershipPercentageAsync(Guid accountId);
    Task<long> GetTotalCapitalAsync();
    Task DistributeProfitDirectAsync(long totalProfit, string period, Guid createdBy);
}

public class CapitalAccountService : ICapitalAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;
    private readonly IJournalService _journalService;
    private readonly IAccountMappingService _mappingService;

    public CapitalAccountService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,
        ITenantService tenantService,
        IJournalService journalService,
        IAccountMappingService mappingService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
        _tenantService = tenantService;
        _journalService = journalService;
        _mappingService = mappingService;
    }

    public async Task<CapitalAccount?> GetByCodeAsync(string code)
    {
        return await _dbContext.CapitalAccounts
            .Include(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(ca => ca.CapitalAccountCode == code);
    }

    public async Task<CapitalTransaction> AddInvestmentAsync(Guid partnerId, long amount, string paymentMode, string remarks, Guid createdBy)
    {
        var partner = await _dbContext.Partners.IgnoreQueryFilters().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner == null) throw new FinVedaException(404, "NOT_FOUND", "Partner not found");

        var branchId = partner.BranchId;

        // 1. Get or create CapitalAccount
        var account = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(ca => ca.PartnerId == partnerId);

        if (account == null)
        {
            account = new CapitalAccount
            {
                Id = Guid.NewGuid(),
                CapitalAccountCode = await _codeService.GenerateCodeAsync("CapitalAccount", branchId),
                PartnerId = partnerId,
                OpeningBalance = amount,
                CurrentBalance = amount,
                Status = "Active",
                Currency = "INR",
                OwnershipPercentage = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            _dbContext.CapitalAccounts.Add(account);
            await _dbContext.SaveChangesAsync();

            // Ensure GL account mapping for capital account exists
            var exists = await _mappingService.ValidateAccountMappingAsync(account.CapitalAccountCode);
            if (!exists)
            {
                var glAccount = await _dbContext.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AccountCode == "GL-3000");
                if (glAccount != null)
                {
                    await _mappingService.CreateAccountMappingAsync(account.CapitalAccountCode, account.Id, "GL-3000", glAccount.Id, branchId);
                }
            }
        }
        else
        {
            account.CurrentBalance += amount;
            _dbContext.CapitalAccounts.Update(account);
            await _dbContext.SaveChangesAsync();
        }

        // 2. Record CapitalTransaction as Pending temporarily so we can create a journal entry, then mark Approved
        var transaction = new CapitalTransaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = await _codeService.GenerateCodeAsync("CapitalTransaction", branchId),
            CapitalAccountId = account.Id,
            TransactionType = "Contribution",
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(remarks) ? "Capital Contribution (Investment)" : remarks,
            ReferenceNumber = paymentMode,
            Status = "Pending",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CapitalTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // 3. Create Journal Entry using the injected journalService
        await _journalService.CreateCapitalAccountJournalEntryAsync(
            transaction.Id,
            account.Id,
            "Contribution",
            amount,
            transaction.Description,
            transaction.ReferenceNumber ?? "",
            createdBy);

        // 4. Mark transaction as Approved
        transaction.Status = "Approved";
        transaction.ApprovedBy = createdBy;
        transaction.ApprovedAt = DateTime.UtcNow;
        _dbContext.CapitalTransactions.Update(transaction);
        await _dbContext.SaveChangesAsync();

        // 5. Recalculate ownership percentages
        await RecalculateOwnershipPercentagesAsync();

        return transaction;
    }

    public async Task<CapitalTransaction> WithdrawAsync(Guid partnerId, long amount, string paymentMode, string remarks, Guid createdBy, bool isImmediate)
    {
        var partner = await _dbContext.Partners.IgnoreQueryFilters().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner == null) throw new FinVedaException(404, "NOT_FOUND", "Partner not found");

        var account = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(ca => ca.PartnerId == partnerId);
        
        if (account == null) throw new FinVedaException(404, "NOT_FOUND", "Capital account not found");
        if (account.CurrentBalance < amount) 
            throw new FinVedaException(400, "BAD_REQUEST", "Withdrawal amount exceeds available capital balance.");

        var branchId = partner.BranchId;

        // 1. Record CapitalTransaction as Pending first so we can use CreateCapitalAccountJournalEntryAsync
        var transaction = new CapitalTransaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = await _codeService.GenerateCodeAsync("CapitalTransaction", branchId),
            CapitalAccountId = account.Id,
            TransactionType = "Withdrawal",
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(remarks) ? "Capital Withdrawal" : remarks,
            ReferenceNumber = paymentMode,
            Status = "Pending",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CapitalTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        if (isImmediate)
        {
            // Immediate flow (e.g. Admin)
            account.CurrentBalance -= amount;
            _dbContext.CapitalAccounts.Update(account);
            await _dbContext.SaveChangesAsync();

            // Create Journal Entry using the injected journalService
            await _journalService.CreateCapitalAccountJournalEntryAsync(
                transaction.Id,
                account.Id,
                "Withdrawal",
                amount,
                transaction.Description,
                transaction.ReferenceNumber ?? "",
                createdBy);

            // Mark transaction as Approved
            transaction.Status = "Approved";
            transaction.ApprovedBy = createdBy;
            transaction.ApprovedAt = DateTime.UtcNow;
            _dbContext.CapitalTransactions.Update(transaction);
            await _dbContext.SaveChangesAsync();

            // Recalculate ownership percentages
            await RecalculateOwnershipPercentagesAsync();
        }

        return transaction;
    }

    public async Task<PartnerCapitalSummaryDto?> GetSummaryAsync(Guid partnerId)
    {
        var partner = await _dbContext.Partners.IgnoreQueryFilters().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner == null) throw new FinVedaException(404, "NOT_FOUND", "Partner not found");

        var account = await _dbContext.CapitalAccounts
            .Include(ca => ca.Transactions)
            .FirstOrDefaultAsync(ca => ca.PartnerId == partnerId);

        if (account == null) return null;

        var approvedTransactions = account.Transactions != null 
            ? account.Transactions.Where(t => t.Status == "Approved").ToList()
            : new List<CapitalTransaction>();

        var totalContributions = approvedTransactions
            .Where(t => t.TransactionType == "Contribution")
            .Sum(t => t.Amount);

        var totalWithdrawals = approvedTransactions
            .Where(t => t.TransactionType == "Withdrawal")
            .Sum(t => t.Amount);

        var totalProfit = approvedTransactions
            .Where(t => t.TransactionType == "Distribution")
            .Sum(t => t.Amount);

        return new PartnerCapitalSummaryDto
        {
            PartnerId = partnerId,
            PartnerCode = partner.PartnerCode ?? string.Empty,
            PartnerName = partner.Name,
            TotalInvestment = account.OpeningBalance + totalContributions,
            TotalProfit = totalProfit,
            TotalWithdrawal = totalWithdrawals,
            CurrentBalance = account.CurrentBalance
        };
    }

    public async Task<List<CapitalAccount>> GetAllAccountsAsync()
    {
        return await _dbContext.CapitalAccounts
            .IgnoreQueryFilters()
            .Include(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .Include(ca => ca.Transactions)
            .Where(ca => ca.Status == "Active")
            .OrderBy(ca => ca.CapitalAccountCode)
            .ToListAsync();
    }

    public async Task<CapitalAccount?> GetAccountByIdAsync(Guid accountId)
    {
        return await _dbContext.CapitalAccounts
            .IgnoreQueryFilters()
            .Include(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .Include(ca => ca.Transactions)
            .FirstOrDefaultAsync(ca => ca.Id == accountId);
    }

    public async Task<CapitalAccount> CreateAccountAsync(Guid partnerId, long openingBalance, Guid createdBy)
    {
        var partner = await _dbContext.Partners.IgnoreQueryFilters().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner == null) throw new FinVedaException(404, "NOT_FOUND", "Partner not found");

        var existing = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(ca => ca.PartnerId == partnerId);
        
        if (existing != null) 
            throw new FinVedaException(400, "BAD_REQUEST", "Capital account already exists for this partner");

        var account = new CapitalAccount
        {
            Id = Guid.NewGuid(),
            CapitalAccountCode = await _codeService.GenerateCodeAsync("CapitalAccount", partner.BranchId),
            PartnerId = partnerId,
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            Status = "Active",
            Currency = "INR",
            OwnershipPercentage = 0,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CapitalAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        // Ensure GL account mapping for capital account exists
        var exists = await _mappingService.ValidateAccountMappingAsync(account.CapitalAccountCode);
        if (!exists)
        {
            var glAccount = await _dbContext.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AccountCode == "GL-3000");
            if (glAccount != null)
            {
                await _mappingService.CreateAccountMappingAsync(account.CapitalAccountCode, account.Id, "GL-3000", glAccount.Id, partner.BranchId);
            }
        }

        // Create initial contribution transaction if opening balance > 0
        if (openingBalance > 0)
        {
            var transaction = new CapitalTransaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = await _codeService.GenerateCodeAsync("CapitalTransaction", partner.BranchId),
                CapitalAccountId = account.Id,
                TransactionType = "Contribution",
                Amount = openingBalance,
                TransactionDate = DateTime.UtcNow,
                Description = "Initial Capital Contribution",
                ReferenceNumber = "Opening Balance",
                Status = "Pending",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.CapitalTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            await _journalService.CreateCapitalAccountJournalEntryAsync(
                transaction.Id,
                account.Id,
                "Contribution",
                openingBalance,
                transaction.Description,
                transaction.ReferenceNumber,
                createdBy);

            transaction.Status = "Approved";
            transaction.ApprovedBy = createdBy;
            transaction.ApprovedAt = DateTime.UtcNow;
            _dbContext.CapitalTransactions.Update(transaction);
            await _dbContext.SaveChangesAsync();
        }

        await RecalculateOwnershipPercentagesAsync();

        return account;
    }

    public async Task<CapitalTransaction> RecordTransactionAsync(
        Guid accountId, 
        string type, 
        long amount, 
        DateTime transactionDate, 
        string description, 
        string? referenceNumber, 
        Guid createdBy)
    {
        var account = await GetAccountByIdAsync(accountId);
        if (account == null) throw new FinVedaException(404, "NOT_FOUND", "Capital account not found");

        var transaction = new CapitalTransaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = await _codeService.GenerateCodeAsync("CapitalTransaction", _tenantService.BranchId),
            CapitalAccountId = accountId,
            TransactionType = type, // Contribution, Withdrawal, Distribution, Adjustment
            Amount = amount,
            TransactionDate = transactionDate,
            Description = description,
            ReferenceNumber = referenceNumber,
            Status = "Pending",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CapitalTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return transaction;
    }

    public async Task<CapitalTransaction?> ApproveTransactionAsync(Guid transactionId, Guid approvedBy)
    {
        var transaction = await _dbContext.CapitalTransactions
            .IgnoreQueryFilters()
            .Include(t => t.CapitalAccount)
            .ThenInclude(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null) 
            throw new FinVedaException(404, "NOT_FOUND", "Transaction not found");

        if (transaction.Status != "Pending") 
            throw new FinVedaException(400, "BAD_REQUEST", "Only pending transactions can be approved");

        var account = transaction.CapitalAccount;
        if (transaction.TransactionType == "Withdrawal")
        {
            if (account.CurrentBalance < transaction.Amount)
                throw new FinVedaException(400, "BAD_REQUEST", "Withdrawal amount exceeds available capital balance.");
            
            account.CurrentBalance -= transaction.Amount;
        }
        else if (transaction.TransactionType == "Contribution")
        {
            account.CurrentBalance += transaction.Amount;
        }
        else if (transaction.TransactionType == "Distribution")
        {
            account.CurrentBalance += transaction.Amount;
        }

        // Create Journal Entry first (since it expects "Pending" status)
        await _journalService.CreateCapitalAccountJournalEntryAsync(
            transaction.Id,
            account.Id,
            transaction.TransactionType,
            transaction.Amount,
            transaction.Description,
            transaction.ReferenceNumber ?? "",
            approvedBy);

        // Update transaction status to Approved
        transaction.Status = "Approved";
        transaction.ApprovedBy = approvedBy;
        transaction.ApprovedAt = DateTime.UtcNow;

        _dbContext.CapitalAccounts.Update(account);
        _dbContext.CapitalTransactions.Update(transaction);
        await _dbContext.SaveChangesAsync();

        // Recalculate ownership percentages
        await RecalculateOwnershipPercentagesAsync();

        return transaction;
    }

    public async Task<CapitalTransaction?> RejectTransactionAsync(Guid transactionId, Guid rejectedBy)
    {
        var transaction = await _dbContext.CapitalTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null) 
            throw new FinVedaException(404, "NOT_FOUND", "Transaction not found");

        if (transaction.Status != "Pending") 
            throw new FinVedaException(400, "BAD_REQUEST", "Only pending transactions can be rejected");

        transaction.Status = "Rejected";
        transaction.ApprovedBy = rejectedBy;
        transaction.ApprovedAt = DateTime.UtcNow;

        _dbContext.CapitalTransactions.Update(transaction);
        await _dbContext.SaveChangesAsync();

        return transaction;
    }

    public async Task<List<CapitalTransaction>> GetTransactionsAsync(Guid accountId)
    {
        var query = _dbContext.CapitalTransactions
            .IgnoreQueryFilters()
            .Include(t => t.CapitalAccount)
            .ThenInclude(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .AsQueryable();

        if (accountId != Guid.Empty)
        {
            query = query.Where(t => t.CapitalAccountId == accountId);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task RecalculateOwnershipPercentagesAsync()
    {
        var accounts = await _dbContext.CapitalAccounts
            .Where(ca => ca.Status == "Active")
            .ToListAsync();

        var totalCapital = accounts.Sum(a => a.CurrentBalance);

        foreach (var account in accounts)
        {
            if (totalCapital > 0)
            {
                account.OwnershipPercentage = (decimal)account.CurrentBalance / (decimal)totalCapital * 100;
            }
            else
            {
                account.OwnershipPercentage = 0;
            }

            var history = new CapitalAccountHistory
            {
                Id = Guid.NewGuid(),
                CapitalAccountId = account.Id,
                EffectiveDate = DateTime.UtcNow.Date,
                Balance = account.CurrentBalance,
                OwnershipPercentage = account.OwnershipPercentage,
                TotalCapitalAtDate = totalCapital,
                CreatedAt = DateTime.UtcNow
            };

            var existing = await _dbContext.CapitalAccountHistories
                .FirstOrDefaultAsync(h => h.CapitalAccountId == account.Id && 
                    h.EffectiveDate == DateTime.UtcNow.Date);

            if (existing == null)
            {
                _dbContext.CapitalAccountHistories.Add(history);
            }
            else
            {
                existing.Balance = account.CurrentBalance;
                existing.OwnershipPercentage = account.OwnershipPercentage;
                existing.TotalCapitalAtDate = totalCapital;
                _dbContext.CapitalAccountHistories.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<decimal> GetOwnershipPercentageAsync(Guid accountId)
    {
        var account = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(ca => ca.Id == accountId);

        return account?.OwnershipPercentage ?? 0;
    }

    public async Task<long> GetTotalCapitalAsync()
    {
        return await _dbContext.CapitalAccounts
            .Where(ca => ca.Status == "Active")
            .SumAsync(ca => ca.CurrentBalance);
    }

    public async Task DistributeProfitDirectAsync(long totalProfit, string period, Guid createdBy)
    {
        if (totalProfit <= 0)
            throw new FinVedaException(400, "BAD_REQUEST", "Profit amount must be greater than zero");

        var accounts = await _dbContext.CapitalAccounts
            .IgnoreQueryFilters()
            .Include(ca => ca.Partner)
            .ThenInclude(p => p.User)
            .Where(ca => ca.Status == "Active")
            .ToListAsync();

        long totalCapital = accounts.Sum(a => a.CurrentBalance);
        if (totalCapital <= 0)
            throw new FinVedaException(400, "BAD_REQUEST", "Total partner capital must be greater than zero to distribute profit");

        // Seed Retained Earnings account if not exists
        var retainedEarningsAccount = await _dbContext.Accounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.AccountCode == "GL-3100");
        if (retainedEarningsAccount == null)
        {
            retainedEarningsAccount = new Account
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
                BranchId = _tenantService.BranchId,
                Name = "Retained Earnings",
                AccountCode = "GL-3100"
            };
            _dbContext.Accounts.Add(retainedEarningsAccount);
            await _dbContext.SaveChangesAsync();
        }

        foreach (var account in accounts)
        {
            if (account.CurrentBalance <= 0) continue;

            // Partner Profit = (Partner Investment / Total Investment) * Total Profit
            decimal shareRatio = (decimal)account.CurrentBalance / (decimal)totalCapital;
            long partnerShare = (long)(totalProfit * shareRatio);

            if (partnerShare <= 0) continue;

            // Create ProfitDistribution record
            var dist = new ProfitDistribution
            {
                Id = Guid.NewGuid(),
                BranchId = _tenantService.BranchId,
                Period = period,
                PayoutAmount = partnerShare,
                Status = "Approved",
                ProcessedAt = DateTime.UtcNow,
                PartnerId = account.PartnerId,
                ProfitDistributionCode = await _codeService.GenerateCodeAsync("ProfitDistribution", _tenantService.BranchId)
            };
            _dbContext.ProfitDistributions.Add(dist);

            // Record transaction (Pending first, for journal entry)
            var transaction = new CapitalTransaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = await _codeService.GenerateCodeAsync("CapitalTransaction", _tenantService.BranchId),
                CapitalAccountId = account.Id,
                TransactionType = "Distribution",
                Amount = partnerShare,
                TransactionDate = DateTime.UtcNow,
                Description = $"Profit Distribution for {period}",
                ReferenceNumber = dist.ProfitDistributionCode,
                Status = "Pending",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.CapitalTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            // Create Journal Entry using injected service
            await _journalService.CreateCapitalAccountJournalEntryAsync(
                transaction.Id,
                account.Id,
                "Distribution",
                partnerShare,
                transaction.Description,
                transaction.ReferenceNumber ?? "",
                createdBy);

            // Mark transaction as Approved
            transaction.Status = "Approved";
            transaction.ApprovedBy = createdBy;
            transaction.ApprovedAt = DateTime.UtcNow;

            // Update partner capital balance
            account.CurrentBalance += partnerShare;

            _dbContext.CapitalAccounts.Update(account);
            _dbContext.CapitalTransactions.Update(transaction);
            await _dbContext.SaveChangesAsync();
        }

        // Recalculate ownership percentages
        await RecalculateOwnershipPercentagesAsync();
    }
}
