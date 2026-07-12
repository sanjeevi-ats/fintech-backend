using System;

namespace Fintech.Core.Domain;

public class InterestWaiver
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public long WaiverAmount { get; set; } // Amount waived in Paise
    public DateTime WaiverDate { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int Status { get; set; } // 1 = Pending, 2 = Approved, 3 = Rejected, 4 = Reversed
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual LoanCase? Loan { get; set; }
    public virtual JournalEntry? ReversalJournalEntry { get; set; }
}

public enum InterestWaiverStatus { Pending = 1, Approved = 2, Rejected = 3, Reversed = 4 }
