using System;

namespace Fintech.Core.Domain;

public class LedgerBalance
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    
    // GL Account reference
    public Guid GLAccountId { get; set; }
    public string GLAccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    
    // Balance tracking
    public long Balance { get; set; } = 0;          // Current balance in Paise (debits - credits)
    public long TotalDebits { get; set; } = 0;      // Cumulative debits in Paise
    public long TotalCredits { get; set; } = 0;     // Cumulative credits in Paise
    
    // Optimistic locking for concurrency control
    public int Version { get; set; } = 1;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
