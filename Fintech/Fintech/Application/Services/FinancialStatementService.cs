using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Fintech.Application.DTOs;

namespace Fintech.Application.Services;

public interface IFinancialStatementService
{
    Task<Phase5TrialBalanceResult> GenerateTrialBalanceAsync(Guid branchId, DateTime? asOfDate = null);
    
    Task<Phase5BalanceSheetResult> GenerateBalanceSheetAsync(Guid branchId, DateTime fromDate, DateTime toDate);
    
    Task<Phase5AccountStatementResult> GenerateAccountStatementAsync(
        Guid branchId, 
        string glAccountCode, 
        DateTime fromDate, 
        DateTime toDate);
    
    Task<byte[]> GeneratePdfReportAsync(string reportType, object reportData);
}

public class FinancialStatementService : IFinancialStatementService
{
    private readonly FinVedaDbContext _dbContext;
    private readonly ILedgerService _ledgerService;
    private readonly IReportPdfService _reportPdfService;
    private readonly ITenantService _tenantService;

    public FinancialStatementService(
        FinVedaDbContext dbContext,
        ILedgerService ledgerService,
        IReportPdfService reportPdfService,
        ITenantService tenantService)
    {
        _dbContext = dbContext;
        _ledgerService = ledgerService;
        _reportPdfService = reportPdfService;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Generates a Trial Balance report as of a given date.
    /// Trial balance ALWAYS balances (total debits = total credits).
    /// </summary>
    public async Task<Phase5TrialBalanceResult> GenerateTrialBalanceAsync(Guid branchId, DateTime? asOfDate = null)
    {
        var result = new Phase5TrialBalanceResult
        {
            Timestamp = DateTime.UtcNow
        };

        // Get all GL accounts
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.BranchId == branchId)
            .ToListAsync();

        // Get all posted journal entries as of the date
        var journalQuery = _dbContext.JournalLines
            .Where(jl => _dbContext.JournalEntries
                .Where(je => je.BranchId == branchId && je.IsPosted && 
                       (asOfDate == null || je.Date <= asOfDate))
                .Select(je => je.Id)
                .Contains(jl.JournalEntryId));

        var journalLines = await journalQuery.ToListAsync();

        // Group by account code and sum debits/credits
        var accountsDict = new Dictionary<string, (long debits, long credits, string name)>();

        foreach (var line in journalLines)
        {
            if (!accountsDict.ContainsKey(line.AccountCode))
            {
                var account = accounts.FirstOrDefault(a => a.AccountCode == line.AccountCode);
                accountsDict[line.AccountCode] = (0, 0, account?.Name ?? line.AccountName);
            }

            var (debits, credits, name) = accountsDict[line.AccountCode];

            if (line.Type.ToLower() == "debit")
                debits += line.Amount;
            else if (line.Type.ToLower() == "credit")
                credits += line.Amount;

            accountsDict[line.AccountCode] = (debits, credits, name);
        }

        // Build result accounts
        foreach (var kvp in accountsDict.OrderBy(x => x.Key))
        {
            var (debits, credits, name) = kvp.Value;
            var balance = debits - credits;

            // Include all accounts, even with zero balances
            result.Accounts.Add(new Phase5TrialBalanceAccount
            {
                AccountCode = kvp.Key,
                AccountName = name,
                Debits = debits,
                Credits = credits
            });

            result.TotalDebits += debits;
            result.TotalCredits += credits;
        }

        // Verify balance - trial balance ALWAYS balances
        result.IsBalanced = result.TotalDebits == result.TotalCredits;

        return result;
    }

    /// <summary>
    /// Generates a Balance Sheet showing Assets, Liabilities, and Equity.
    /// Verifies accounting equation: Assets = Liabilities + Equity
    /// </summary>
    public async Task<Phase5BalanceSheetResult> GenerateBalanceSheetAsync(Guid branchId, DateTime fromDate, DateTime toDate)
    {
        var result = new Phase5BalanceSheetResult
        {
            AsOfDate = toDate
        };

        // Get all GL accounts with account type classification
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.BranchId == branchId)
            .ToListAsync();

        // Get opening balances (as of fromDate - 1 day)
        var openingDate = fromDate.AddDays(-1);
        var openingBalances = await GetAccountBalancesAsOfDateAsync(branchId, openingDate);

        // Get closing balances (as of toDate)
        var closingBalances = await GetAccountBalancesAsOfDateAsync(branchId, toDate);

        // Classify accounts by type (simple logic - would be enhanced based on account code patterns)
        var assetAccounts = new List<Phase5BalanceSheetAccount>();
        var liabilityAccounts = new List<Phase5BalanceSheetAccount>();
        var equityAccounts = new List<Phase5BalanceSheetAccount>();

        foreach (var account in accounts)
        {
            var closingBalance = closingBalances.ContainsKey(account.AccountCode ?? "") 
                ? closingBalances[account.AccountCode ?? ""] 
                : 0;
            var openingBalance = openingBalances.ContainsKey(account.AccountCode ?? "") 
                ? openingBalances[account.AccountCode ?? ""] 
                : 0;

            var accountType = ClassifyAccountType(account.AccountCode ?? "");

            var bsAccount = new Phase5BalanceSheetAccount
            {
                AccountCode = account.AccountCode ?? "",
                AccountName = account.Name,
                AccountType = accountType,
                Balance = closingBalance,
                OpeningBalance = openingBalance
            };

            // Only include accounts with non-zero balances
            if (closingBalance != 0 || openingBalance != 0)
            {
                if (accountType == "Asset")
                    assetAccounts.Add(bsAccount);
                else if (accountType == "Liability")
                    liabilityAccounts.Add(bsAccount);
                else if (accountType == "Equity")
                    equityAccounts.Add(bsAccount);
            }
        }

        // Build balance sheet sections
        result.Assets = new Phase5BalanceSheetSection
        {
            SectionName = "Assets",
            Accounts = assetAccounts.OrderBy(a => a.AccountCode).ToList(),
            Subtotal = assetAccounts.Sum(a => a.Balance)
        };
        result.TotalAssets = result.Assets.Subtotal;

        result.Liabilities = new Phase5BalanceSheetSection
        {
            SectionName = "Liabilities",
            Accounts = liabilityAccounts.OrderBy(a => a.AccountCode).ToList(),
            Subtotal = liabilityAccounts.Sum(a => a.Balance)
        };
        result.TotalLiabilities = result.Liabilities.Subtotal;

        result.Equity = new Phase5BalanceSheetSection
        {
            SectionName = "Equity",
            Accounts = equityAccounts.OrderBy(a => a.AccountCode).ToList(),
            Subtotal = equityAccounts.Sum(a => a.Balance)
        };
        result.TotalEquity = result.Equity.Subtotal;

        // Verify accounting equation: Assets = Liabilities + Equity
        result.IsBalanced = result.TotalAssets == (result.TotalLiabilities + result.TotalEquity);
        result.ValidationMessage = result.IsBalanced 
            ? "Balance sheet is balanced" 
            : $"Balance sheet is NOT balanced. Assets: {result.TotalAssets}, Liabilities + Equity: {result.TotalLiabilities + result.TotalEquity}";

        return result;
    }

    /// <summary>
    /// Generates an Account Statement showing transaction history with running balance.
    /// </summary>
    public async Task<Phase5AccountStatementResult> GenerateAccountStatementAsync(
        Guid branchId,
        string glAccountCode,
        DateTime fromDate,
        DateTime toDate)
    {
        // Validate account exists
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.BranchId == branchId && a.AccountCode == glAccountCode);

        if (account == null)
            throw new FinVedaException(404, "NOT_FOUND", $"GL account {glAccountCode} not found");

        var result = new Phase5AccountStatementResult
        {
            AccountCode = glAccountCode,
            AccountName = account.Name,
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get opening balance as of fromDate - 1 day
        var openingDate = fromDate.AddDays(-1);
        var openingBalances = await GetAccountBalancesAsOfDateAsync(branchId, openingDate);
        result.OpeningBalance = openingBalances.ContainsKey(glAccountCode) 
            ? openingBalances[glAccountCode] 
            : 0;

        var runningBalance = result.OpeningBalance;

        // Get all journal lines for this account in the date range
        var journalLines = await _dbContext.JournalLines
            .Where(jl => jl.AccountCode == glAccountCode && jl.BranchId == branchId &&
                   _dbContext.JournalEntries
                       .Where(je => je.IsPosted && je.Date >= fromDate && je.Date <= toDate)
                       .Select(je => je.Id)
                       .Contains(jl.JournalEntryId))
            .OrderBy(jl => _dbContext.JournalEntries
                .Where(je => je.Id == jl.JournalEntryId)
                .Select(je => je.Date)
                .FirstOrDefault())
            .ThenBy(jl => jl.JournalEntryId)
            .ToListAsync();

        // Build transaction lines with running balance
        foreach (var line in journalLines)
        {
            var journalEntry = await _dbContext.JournalEntries
                .FirstOrDefaultAsync(je => je.Id == line.JournalEntryId);

            if (journalEntry == null)
                continue;

            var debit = line.Type.ToLower() == "debit" ? line.Amount : 0;
            var credit = line.Type.ToLower() == "credit" ? line.Amount : 0;

            runningBalance += debit;
            runningBalance -= credit;

            result.Transactions.Add(new Phase5AccountStatementLine
            {
                Date = journalEntry.Date,
                EntryCode = journalEntry.JournalEntryCode ?? journalEntry.PublicId,
                Description = journalEntry.Description,
                Debit = debit,
                Credit = credit,
                RunningBalance = runningBalance
            });

            result.TotalDebits += debit;
            result.TotalCredits += credit;
        }

        // Set closing balance
        result.ClosingBalance = runningBalance;

        return result;
    }

    /// <summary>
    /// Generates a PDF report from financial statement data.
    /// </summary>
    public async Task<byte[]> GeneratePdfReportAsync(string reportType, object reportData)
    {
        try
        {
            // TODO: Implement PDF generation using IReportPdfService
            // For now, return empty byte array as placeholder
            // This would delegate to the existing PDF service once method is available
            
            // Create a simple PDF representation
            var pdfContent = $"Report Type: {reportType}\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            var pdfBytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);
            
            return await Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            throw new FinVedaException(500, "PDF_GENERATION_ERROR", $"Failed to generate PDF: {ex.Message}");
        }
    }

    // ===== PRIVATE HELPER METHODS =====

    /// <summary>
    /// Gets the balance for each account as of a given date.
    /// </summary>
    private async Task<Dictionary<string, long>> GetAccountBalancesAsOfDateAsync(Guid branchId, DateTime asOfDate)
    {
        var balances = new Dictionary<string, long>();

        var journalLines = await _dbContext.JournalLines
            .Where(jl => jl.BranchId == branchId &&
                   _dbContext.JournalEntries
                       .Where(je => je.BranchId == branchId && je.IsPosted && je.Date <= asOfDate)
                       .Select(je => je.Id)
                       .Contains(jl.JournalEntryId))
            .ToListAsync();

        foreach (var line in journalLines)
        {
            if (!balances.ContainsKey(line.AccountCode))
                balances[line.AccountCode] = 0;

            if (line.Type.ToLower() == "debit")
                balances[line.AccountCode] += line.Amount;
            else if (line.Type.ToLower() == "credit")
                balances[line.AccountCode] -= line.Amount;
        }

        return balances;
    }

    /// <summary>
    /// Classifies account type based on account code patterns.
    /// This is a simplified implementation and should be enhanced based on actual account code structure.
    /// </summary>
    private string ClassifyAccountType(string accountCode)
    {
        if (string.IsNullOrEmpty(accountCode))
            return "Other";

        // Simple classification based on GL account code patterns
        // GL-1000 to GL-1999: Assets
        // GL-2000 to GL-2999: Liabilities
        // GL-3000 to GL-3999: Equity
        // GL-4000 to GL-4999: Revenue
        // GL-5000 to GL-5999: Expenses

        if (accountCode.StartsWith("GL-1"))
            return "Asset";
        if (accountCode.StartsWith("GL-2"))
            return "Liability";
        if (accountCode.StartsWith("GL-3"))
            return "Equity";
        if (accountCode.StartsWith("GL-4"))
            return "Revenue";
        if (accountCode.StartsWith("GL-5"))
            return "Expense";

        return "Other";
    }
}
