using System;

namespace Fintech.Core.Domain;

public class ProvisionEntry
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? LoanId { get; set; }
    public int Type { get; set; } // 1 = Doubtful Debt, 2 = Loan Loss, 3 = Other
    public long Amount { get; set; } // In Paise
    public long PreviousAmount { get; set; } // In Paise
    public Guid JournalEntryId { get; set; }
    public int Status { get; set; } // 1 = Created, 2 = Posted, 3 = Reversed
    public string? CalculationMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
    public virtual LoanCase? Loan { get; set; }
    public virtual JournalEntry? JournalEntry { get; set; }
}
