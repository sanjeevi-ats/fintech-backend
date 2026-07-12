using System;

namespace Fintech.Core.Domain;

public class InterestPosting
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid? PeriodId { get; set; }
    public long PostedAmount { get; set; } // Amount posted in Paise
    public DateTime PostedDate { get; set; }
    public Guid JournalEntryId { get; set; }
    public int Status { get; set; } // 1 = Posted, 2 = Reversed
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual LoanCase? Loan { get; set; }
    public virtual AccountingPeriod? Period { get; set; }
    public virtual JournalEntry? JournalEntry { get; set; }
}

public enum InterestPostingStatus { Posted = 1, Reversed = 2 }
