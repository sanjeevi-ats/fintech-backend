using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IJournalService
{
    Task<JournalEntry> CreateCapitalAccountJournalEntryAsync(
        Guid capitalTransactionId,
        Guid capitalAccountId,
        string transactionType,
        long amount,
        string description,
        string referenceNumber,
        Guid createdBy);

    Task<JournalEntry> CreateManualJournalEntryAsync(
        List<(string accountCode, string accountName, string type, long amount)> lines,
        string description,
        Guid createdBy,
        Guid? approverId = null);

    Task<JournalEntry> ReverseJournalEntryAsync(Guid journalEntryId, Guid createdBy);
    
    Task<bool> ValidateEntryBalanceAsync(JournalEntry entry);
    
    Task<string> GenerateJournalEntryCodeAsync(Guid branchId);
    
    Task<string> GenerateJournalLineCodeAsync(Guid branchId);
}

public class JournalService : IJournalService
{
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly IAccountMappingService _accountMappingService;
    private readonly IAuditService _auditService;
    private readonly ITenantService _tenantService;

    public JournalService(
        FinVedaDbContext dbContext,
        ICodeGenerationService codeGenerationService,
        IAccountMappingService accountMappingService,
        IAuditService auditService,
        ITenantService tenantService)
    {
        _dbContext = dbContext;
        _codeGenerationService = codeGenerationService;
        _accountMappingService = accountMappingService;
        _auditService = auditService;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Creates a journal entry for a capital transaction approval.
    /// Automatically determines debit/credit accounts based on transaction type.
    /// </summary>
    public async Task<JournalEntry> CreateCapitalAccountJournalEntryAsync(
        Guid capitalTransactionId,
        Guid capitalAccountId,
        string transactionType,
        long amount,
        string description,
        string referenceNumber,
        Guid createdBy)
    {
        // Validate inputs
        if (amount <= 0)
            throw new FinVedaException(400, "BAD_REQUEST", "Amount must be greater than zero");

        if (transactionType != "Contribution" && transactionType != "Withdrawal" && transactionType != "Distribution")
            throw new FinVedaException(400, "BAD_REQUEST", "Invalid transaction type");

        // Validate capital transaction exists and is "Pending"
        var transaction = await _dbContext.CapitalTransactions
            .FirstOrDefaultAsync(t => t.Id == capitalTransactionId);

        if (transaction == null)
            throw new FinVedaException(404, "NOT_FOUND", "Capital transaction not found");

        if (transaction.Status != "Pending")
            throw new FinVedaException(400, "BAD_REQUEST", "Only pending transactions can create journal entries");

        // Get capital account
        var capitalAccount = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(a => a.Id == capitalAccountId);

        if (capitalAccount == null)
            throw new FinVedaException(404, "NOT_FOUND", "Capital account not found");

        // Get GL account mapping for capital account
        var capitalGLMapping = await _accountMappingService
            .GetGLAccountForCapitalAsync(capitalAccount.CapitalAccountCode);

        if (string.IsNullOrEmpty(capitalGLMapping))
            throw new FinVedaException(404, "ACCOUNT_MAPPING_MISSING", 
                $"GL account mapping not found for capital account {capitalAccount.CapitalAccountCode}");

        // Validate GL accounts exist
        var bankAccount = await _dbContext.Accounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.AccountCode == "GL-1000");
        var capitalGLAccount = await _dbContext.Accounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.AccountCode == capitalGLMapping);

        if (bankAccount == null)
            throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", "Bank GL account (GL-1000) not found");

        if (capitalGLAccount == null)
            throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", $"Capital GL account ({capitalGLMapping}) not found");

        // Begin transaction to ensure atomicity
        using (var dbTransaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                // Create journal entry
                var journalEntry = new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    BranchId = _tenantService.BranchId,
                    JournalEntryCode = await GenerateJournalEntryCodeAsync(_tenantService.BranchId),
                    Date = DateTime.UtcNow,
                    Description = $"Capital {transactionType}: {description}",
                    Reference = referenceNumber,
                    IsManual = false,
                    IsPosted = true,  // Auto-post on creation
                    CapitalTransactionId = capitalTransactionId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    Lines = new List<JournalLine>()
                };

                // Determine debit and credit accounts based on transaction type
                JournalLine debitLine, creditLine;

                if (transactionType == "Contribution")
                {
                    // Debit Bank (asset increase), Credit Capital
                    debitLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = "GL-1000",
                        AccountName = bankAccount.Name,
                        Type = "debit",
                        Amount = amount
                    };

                    creditLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = capitalGLMapping,
                        AccountName = capitalGLAccount.Name,
                        Type = "credit",
                        Amount = amount
                    };
                }
                else if (transactionType == "Distribution")
                {
                    // Ensure Retained Earnings account exists
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

                    // Debit Retained Earnings, Credit Capital (increases capital balance)
                    debitLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = "GL-3100",
                        AccountName = retainedEarningsAccount.Name,
                        Type = "debit",
                        Amount = amount
                    };

                    creditLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = capitalGLMapping,
                        AccountName = capitalGLAccount.Name,
                        Type = "credit",
                        Amount = amount
                    };
                }
                else // Withdrawal
                {
                    // Debit Capital, Credit Bank (asset decrease)
                    debitLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = capitalGLMapping,
                        AccountName = capitalGLAccount.Name,
                        Type = "debit",
                        Amount = amount
                    };

                    creditLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = "GL-1000",
                        AccountName = bankAccount.Name,
                        Type = "credit",
                        Amount = amount
                    };
                }

                journalEntry.Lines.Add(debitLine);
                journalEntry.Lines.Add(creditLine);

                // Verify entry balances
                if (!await ValidateEntryBalanceAsync(journalEntry))
                    throw new FinVedaException(400, "UNBALANCED_ENTRY", 
                        "Journal entry debits do not equal credits");

                // Insert entry and lines atomically
                _dbContext.JournalEntries.Add(journalEntry);
                await _dbContext.SaveChangesAsync();

                // Audit logging is automatic via DbContext.SaveChangesAsync()

                await dbTransaction.CommitAsync();

                return journalEntry;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a manual journal entry with validation.
    /// Requires debits to equal credits.
    /// </summary>
    public async Task<JournalEntry> CreateManualJournalEntryAsync(
        List<(string accountCode, string accountName, string type, long amount)> lines,
        string description,
        Guid createdBy,
        Guid? approverIdNullable = null)
    {
        // Validate inputs
        if (lines == null || lines.Count < 2)
            throw new FinVedaException(400, "BAD_REQUEST", "Manual entries must have at least 2 lines");

        var approverIdValue = approverIdNullable ?? Guid.Empty;

        // Calculate totals
        long debits = 0, credits = 0;
        foreach (var (accountCode, _, type, amount) in lines)
        {
            if (amount <= 0)
                throw new FinVedaException(400, "BAD_REQUEST", "All amounts must be greater than zero");

            if (type.ToLower() == "debit")
                debits += amount;
            else if (type.ToLower() == "credit")
                credits += amount;
            else
                throw new FinVedaException(400, "BAD_REQUEST", "Invalid line type (must be debit or credit)");
        }

        // Validate balance
        if (debits != credits)
            throw new FinVedaException(400, "UNBALANCED_ENTRY", 
                $"Debits ({debits}) do not equal credits ({credits})");

        using (var dbTransaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                var journalEntry = new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    BranchId = _tenantService.BranchId,
                    JournalEntryCode = await GenerateJournalEntryCodeAsync(_tenantService.BranchId),
                    Date = DateTime.UtcNow,
                    Description = description,
                    Reference = "",
                    IsManual = true,
                    IsPosted = false,  // Manual entries require approval before posting
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    Lines = new List<JournalLine>()
                };

                // Create journal lines
                foreach (var (accountCode, accountName, type, amount) in lines)
                {
                    var line = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = accountCode,
                        AccountName = accountName,
                        Type = type.ToLower(),
                        Amount = amount
                    };
                    journalEntry.Lines.Add(line);
                }

                _dbContext.JournalEntries.Add(journalEntry);
                await _dbContext.SaveChangesAsync();

                // Audit logging is automatic via DbContext.SaveChangesAsync()

                await dbTransaction.CommitAsync();

                return journalEntry;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a reversing entry for an existing journal entry.
    /// </summary>
    public async Task<JournalEntry> ReverseJournalEntryAsync(Guid journalEntryId, Guid createdBy)
    {
        // Get original entry
        var originalEntry = await _dbContext.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId);

        if (originalEntry == null)
            throw new FinVedaException(404, "NOT_FOUND", "Journal entry not found");

        using (var dbTransaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                // Create reversing entry
                var reversingEntry = new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    BranchId = _tenantService.BranchId,
                    JournalEntryCode = await GenerateJournalEntryCodeAsync(_tenantService.BranchId),
                    Date = DateTime.UtcNow,
                    Description = $"Reversal of {originalEntry.JournalEntryCode}: {originalEntry.Description}",
                    Reference = originalEntry.Reference,
                    IsManual = originalEntry.IsManual,
                    IsPosted = true,
                    ReversalOfEntryId = journalEntryId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    Lines = new List<JournalLine>()
                };

                // Create reversing lines (opposite amounts)
                foreach (var originalLine in originalEntry.Lines)
                {
                    var reversingType = originalLine.Type == "debit" ? "credit" : "debit";
                    var reversingLine = new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        JournalLineCode = await GenerateJournalLineCodeAsync(_tenantService.BranchId),
                        AccountCode = originalLine.AccountCode,
                        AccountName = originalLine.AccountName,
                        Type = reversingType,
                        Amount = originalLine.Amount
                    };
                    reversingEntry.Lines.Add(reversingLine);
                }

                _dbContext.JournalEntries.Add(reversingEntry);
                await _dbContext.SaveChangesAsync();

                // Audit logging is automatic via DbContext.SaveChangesAsync()

                await dbTransaction.CommitAsync();

                return reversingEntry;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Validates that journal entry debits equal credits.
    /// </summary>
    public async Task<bool> ValidateEntryBalanceAsync(JournalEntry entry)
    {
        long debits = 0, credits = 0;

        foreach (var line in entry.Lines)
        {
            if (line.Type.ToLower() == "debit")
                debits += line.Amount;
            else if (line.Type.ToLower() == "credit")
                credits += line.Amount;
        }

        return debits == credits;
    }

    /// <summary>
    /// Generates a unique journal entry code.
    /// </summary>
    public async Task<string> GenerateJournalEntryCodeAsync(Guid branchId)
    {
        return await _codeGenerationService.GenerateCodeAsync("JournalEntry", branchId);
    }

    /// <summary>
    /// Generates a unique journal line code.
    /// </summary>
    public async Task<string> GenerateJournalLineCodeAsync(Guid branchId)
    {
        return await _codeGenerationService.GenerateCodeAsync("JournalLine", branchId);
    }
}
