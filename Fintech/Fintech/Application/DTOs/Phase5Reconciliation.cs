using System;
using System.Collections.Generic;

namespace Fintech.Application.DTOs;

/// <summary>
/// Result of reconciliation verification
/// </summary>
public class ReconciliationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Account discrepancy found during reconciliation
/// </summary>
public class AccountDiscrepancy
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long JournalBalance { get; set; }  // Balance calculated from journal entries
    public long LedgerBalance { get; set; }   // Balance stored in ledger
    public long Difference { get; set; }      // LedgerBalance - JournalBalance
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Comprehensive reconciliation report
/// </summary>
public class ComprehensiveReconciliationReport
{
    public Guid BranchId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    public ReconciliationResult TrialBalanceCheck { get; set; } = new();
    public ReconciliationResult JournalVsLedgerCheck { get; set; } = new();
    public ReconciliationResult OrphanedEntriesCheck { get; set; } = new();
    
    public List<AccountDiscrepancy> Discrepancies { get; set; } = new();
    public List<OrphanedEntry> OrphanedEntries { get; set; } = new();
    
    public bool IsFullyReconciled { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Orphaned journal or ledger entry
/// </summary>
public class OrphanedEntry
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // JournalEntry, LedgerEntry
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public long Amount { get; set; }
}

/// <summary>
/// Journal vs Ledger comparison line item
/// </summary>
public class JournalVsLedgerComparison
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long JournalDebits { get; set; }
    public long JournalCredits { get; set; }
    public long LedgerDebits { get; set; }
    public long LedgerCredits { get; set; }
    
    public bool DebitsMatch { get; set; }
    public bool CreditsMatch { get; set; }
    public long DebitDifference { get; set; }
    public long CreditDifference { get; set; }
}

/// <summary>
/// Reconciliation summary for dashboard
/// </summary>
public class ReconciliationSummaryDto
{
    public bool LastCheckPassed { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public int DiscrepanciesFound { get; set; }
    public int OrphanedEntriesFound { get; set; }
    public List<AccountDiscrepancy> TopDiscrepancies { get; set; } = new();
}

/// <summary>
/// Request to reset account balance (supervisor only)
/// </summary>
public class ResetAccountBalanceRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public long CorrectBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of account balance reset
/// </summary>
public class ResetAccountBalanceResult
{
    public bool Success { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public long PreviousBalance { get; set; }
    public long NewBalance { get; set; }
    public string AdjustmentEntryCode { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }
}

/// <summary>
/// Discrepancy log entry
/// </summary>
public class DiscrepancyLog
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string DiscrepancyType { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Details { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public DateTime LoggedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
