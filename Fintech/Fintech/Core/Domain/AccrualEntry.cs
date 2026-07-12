using System;

namespace Fintech.Core.Domain;

public class AccrualEntry
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? LoanId { get; set; }
    public int Type { get; set; } // 1 = Interest, 2 = Penalty, 3 = Other
    public long Amount { get; set; } // In Paise
    public Guid JournalEntryId { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime AccrualDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
    public virtual LoanCase? Loan { get; set; }
    public virtual JournalEntry? JournalEntry { get; set; }
    public virtual JournalEntry? ReversalJournalEntry { get; set; }
}
