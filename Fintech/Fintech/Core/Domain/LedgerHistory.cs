using System;

namespace Fintech.Core.Domain;

public class LedgerHistory
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    
    // GL Account reference
    public Guid GLAccountId { get; set; }
    public string GLAccountCode { get; set; } = string.Empty;
    
    // Balance history
    public DateTime Date { get; set; }              // Effective date of the transaction
    public long Balance { get; set; } = 0;          // Balance in Paise as of this date
    public long Debits { get; set; } = 0;           // Debits recorded on this date
    public long Credits { get; set; } = 0;          // Credits recorded on this date
    
    // Tracking
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
