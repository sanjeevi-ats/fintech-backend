using System;
using System.Collections.Generic;

namespace Fintech.Core.Domain;

/// <summary>
/// Capital Account - Tracks investor/partner equity contributions and ownership
/// </summary>
public class CapitalAccount
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., CAP0001, CAP0002)
    /// </summary>
    public string CapitalAccountCode { get; set; } = string.Empty;
    
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;

    /// <summary>
    /// Initial capital balance (opening balance in Paise)
    /// </summary>
    public long OpeningBalance { get; set; }

    /// <summary>
    /// Current total capital balance (in Paise)
    /// </summary>
    public long CurrentBalance { get; set; }

    /// <summary>
    /// Ownership percentage (stored as decimal, e.g., 12.3456)
    /// </summary>
    public decimal OwnershipPercentage { get; set; }

    /// <summary>
    /// Account status: Active, Closed, Suspended
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Currency code (default: INR)
    /// </summary>
    public string Currency { get; set; } = "INR";

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    // Navigation properties
    public ICollection<CapitalTransaction> Transactions { get; set; } = new List<CapitalTransaction>();
    public ICollection<CapitalAccountHistory> History { get; set; } = new List<CapitalAccountHistory>();
}

/// <summary>
/// Capital Transaction - Records all capital account movements
/// </summary>
public class CapitalTransaction
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Human-readable transaction code (e.g., CAPTX0001, CAPTX0002)
    /// </summary>
    public string TransactionCode { get; set; } = string.Empty;
    
    public Guid CapitalAccountId { get; set; }
    public CapitalAccount CapitalAccount { get; set; } = null!;

    /// <summary>
    /// Transaction type: Contribution, Withdrawal, Distribution, Adjustment
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount (in Paise)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Date of transaction
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Description or reason for transaction
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference document number (e.g., cheque number, invoice number)
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Transaction status: Pending, Approved, Rejected
    /// </summary>
    public string Status { get; set; } = "Pending";

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    // Navigation properties
    public User? ApprovedByUser { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}

/// <summary>
/// Capital Account History - Historical snapshot of capital account for profit allocation
/// </summary>
public class CapitalAccountHistory
{
    public Guid Id { get; set; }
    
    public Guid CapitalAccountId { get; set; }
    public CapitalAccount CapitalAccount { get; set; } = null!;

    /// <summary>
    /// Date when this snapshot is effective
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Balance as of effective date (in Paise)
    /// </summary>
    public long Balance { get; set; }

    /// <summary>
    /// Ownership percentage as of effective date
    /// </summary>
    public decimal OwnershipPercentage { get; set; }

    /// <summary>
    /// Total capital across all partners on this effective date (in Paise)
    /// </summary>
    public long TotalCapitalAtDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
