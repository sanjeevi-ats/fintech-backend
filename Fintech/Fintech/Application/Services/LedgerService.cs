using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface ILedgerService
{
    Task<LedgerPostResult> PostJournalEntryAsync(Guid journalEntryId, Guid postedBy);
    
    Task<long> GetAccountBalanceAsync(string glAccountCode);
    
    Task<Phase5TrialBalanceResult> GetTrialBalanceAsync(DateTime? asOfDate = null);
    
    Task UpdateLedgerBalanceAsync(Guid glAccountId, string glAccountCode, long debitAmount, long creditAmount);
}

/// <summary>
/// Result of posting a journal entry to the ledger.
/// </summary>
public class LedgerPostResult
{
    public bool Success { get; set; }
    public int EntriesPosted { get; set; }
    public int AccountsUpdated { get; set; }
    public Dictionary<string, AccountBalanceInfo> Balances { get; set; } = new();
}

/// <summary>
/// Account balance information.
/// </summary>
public class AccountBalanceInfo
{
    public long Balance { get; set; }
    public long Debits { get; set; }
    public long Credits { get; set; }
}

/// <summary>
/// Phase 5 Trial balance result showing all accounts and balance status.
/// </summary>
public class Phase5TrialBalanceResult
{
    public bool IsBalanced { get; set; }
    public long TotalDebits { get; set; }
    public long TotalCredits { get; set; }
    public List<Phase5TrialBalanceAccount> Accounts { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Single account in Phase 5 trial balance.
/// </summary>
public class Phase5TrialBalanceAccount
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long Debits { get; set; }
    public long Credits { get; set; }
}

public class LedgerService : ILedgerService
{
    private readonly FinVedaDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ITenantService _tenantService;
    private const int MAX_RETRY_ATTEMPTS = 3;

    public LedgerService(
        FinVedaDbContext dbContext,
        IAuditService auditService,
        ITenantService tenantService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Posts a journal entry to the ledger, updating GL account balances atomically.
    /// </summary>
    public async Task<LedgerPostResult> PostJournalEntryAsync(Guid journalEntryId, Guid postedBy)
    {
        // Get journal entry with lines
        var journalEntry = await _dbContext.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId);

        if (journalEntry == null)
            throw new FinVedaException(404, "NOT_FOUND", "Journal entry not found");

        if (journalEntry.IsPosted)
            throw new FinVedaException(400, "BAD_REQUEST", "Journal entry is already posted");

        using (var dbTransaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                var result = new LedgerPostResult { Success = false };
                
                // Post each journal line
                foreach (var line in journalEntry.Lines)
                {
                    // Get GL account
                    var glAccount = await _dbContext.Accounts
                        .FirstOrDefaultAsync(a => a.AccountCode == line.AccountCode);

                    if (glAccount == null)
                        throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", 
                            $"GL account {line.AccountCode} not found");

                    // Update ledger balance
                    long debitAmount = line.Type.ToLower() == "debit" ? line.Amount : 0;
                    long creditAmount = line.Type.ToLower() == "credit" ? line.Amount : 0;

                    await UpdateLedgerBalanceAsync(glAccount.Id, line.AccountCode, debitAmount, creditAmount);

                    // Create ledger history record
                    var history = new LedgerHistory
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        GLAccountId = glAccount.Id,
                        GLAccountCode = line.AccountCode,
                        Date = journalEntry.Date,
                        Balance = (await GetAccountBalanceAsync(line.AccountCode)),
                        Debits = debitAmount,
                        Credits = creditAmount,
                        RecordedAt = DateTime.UtcNow
                    };

                    _dbContext.LedgerHistories.Add(history);

                    result.EntriesPosted++;
                    if (!result.Balances.ContainsKey(line.AccountCode))
                    {
                        result.Balances[line.AccountCode] = new AccountBalanceInfo();
                    }
                }

                // Mark entry as posted
                journalEntry.IsPosted = true;
                await _dbContext.SaveChangesAsync();

                // Populate balance information
                foreach (var accountCode in result.Balances.Keys)
                {
                    var balance = await GetAccountBalanceAsync(accountCode);
                    result.Balances[accountCode].Balance = balance;
                }

                // Audit logging is automatic via DbContext.SaveChangesAsync()

                result.Success = true;
                result.AccountsUpdated = result.Balances.Count;

                await dbTransaction.CommitAsync();

                return result;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Gets the current balance for a GL account.
    /// Balance = Debits - Credits
    /// </summary>
    public async Task<long> GetAccountBalanceAsync(string glAccountCode)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountCode == glAccountCode);

        if (account == null)
            throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", $"GL account {glAccountCode} not found");

        var ledger = await _dbContext.LedgerBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.GLAccountCode == glAccountCode);

        if (ledger == null)
            return 0;  // New account has zero balance

        return ledger.Balance;
    }

    /// <summary>
    /// Calculates trial balance from all posted journal entries.
    /// Returns balanced status and account details.
    /// </summary>
    public async Task<Phase5TrialBalanceResult> GetTrialBalanceAsync(DateTime? asOfDate = null)
    {
        var result = new Phase5TrialBalanceResult
        {
            Timestamp = DateTime.UtcNow
        };

        // Query all posted journal entries
        var query = _dbContext.JournalLines
            .Where(jl => _dbContext.JournalEntries
                .Where(je => je.IsPosted && (asOfDate == null || je.Date <= asOfDate))
                .Select(je => je.Id)
                .Contains(jl.JournalEntryId));

        var journalLines = await query.ToListAsync();

        // Group by account code and sum debits/credits
        var accountsDict = new Dictionary<string, (long debits, long credits, string name)>();

        foreach (var line in journalLines)
        {
            if (!accountsDict.ContainsKey(line.AccountCode))
            {
                accountsDict[line.AccountCode] = (0, 0, line.AccountName);
            }

            var (debits, credits, name) = accountsDict[line.AccountCode];

            if (line.Type.ToLower() == "debit")
                debits += line.Amount;
            else if (line.Type.ToLower() == "credit")
                credits += line.Amount;

            accountsDict[line.AccountCode] = (debits, credits, name);
        }

        // Build result
        foreach (var kvp in accountsDict)
        {
            var (debits, credits, name) = kvp.Value;
            
            // Skip zero balance accounts
            if (debits == 0 && credits == 0)
                continue;

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

        // Verify balance
        result.IsBalanced = result.TotalDebits == result.TotalCredits;

        return result;
    }

    /// <summary>
    /// Updates a GL account balance with optimistic locking.
    /// Implements retry logic for concurrent updates.
    /// </summary>
    public async Task UpdateLedgerBalanceAsync(Guid glAccountId, string glAccountCode, long debitAmount, long creditAmount)
    {
        int retryCount = 0;

        while (retryCount < MAX_RETRY_ATTEMPTS)
        {
            try
            {
                // Get current balance
                var ledgerBalance = await _dbContext.LedgerBalances
                    .FirstOrDefaultAsync(l => l.GLAccountId == glAccountId);

                if (ledgerBalance == null)
                {
                    // Create new ledger balance entry
                    var glAccount = await _dbContext.Accounts
                        .FirstOrDefaultAsync(a => a.Id == glAccountId);

                    if (glAccount == null)
                        throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", "GL account not found");

                    ledgerBalance = new LedgerBalance
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        GLAccountId = glAccountId,
                        GLAccountCode = glAccountCode,
                        AccountName = glAccount.Name,
                        Balance = debitAmount - creditAmount,
                        TotalDebits = debitAmount,
                        TotalCredits = creditAmount,
                        Version = 1,
                        LastUpdated = DateTime.UtcNow
                    };

                    _dbContext.LedgerBalances.Add(ledgerBalance);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Update existing balance with optimistic locking using FormattableString for PostgreSQL
                    var oldVersion = ledgerBalance.Version;
                    var newBalance = (ledgerBalance.Balance + debitAmount) - creditAmount;
                    var now = DateTime.UtcNow;

                    // Use FormattableString interpolation for proper SQL parameter binding (PostgreSQL compatible)
                    var result = await _dbContext.Database.ExecuteSqlAsync(
                        $@"UPDATE ledger_balances 
                        SET balance = {newBalance},
                            total_debits = total_debits + {debitAmount},
                            total_credits = total_credits + {creditAmount},
                            version = version + 1,
                            last_updated = {now}
                        WHERE id = {ledgerBalance.Id} AND version = {oldVersion}");

                    if (result == 0)
                    {
                        // Version mismatch - retry
                        retryCount++;
                        if (retryCount >= MAX_RETRY_ATTEMPTS)
                            throw new FinVedaException(409, "OPTIMISTIC_LOCK_FAILED", 
                                "Failed to update ledger balance after maximum retries due to concurrent updates");

                        // Clear change tracker and retry
                        _dbContext.ChangeTracker.Clear();
                        continue;
                    }
                }

                return;  // Success
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                if (retryCount >= MAX_RETRY_ATTEMPTS)
                    throw new FinVedaException(409, "OPTIMISTIC_LOCK_FAILED", 
                        "Failed to update ledger balance due to concurrent updates");
            }
        }
    }
}
