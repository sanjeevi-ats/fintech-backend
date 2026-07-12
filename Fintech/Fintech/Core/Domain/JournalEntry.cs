using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fintech.Core.Domain;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string PublicId { get; set; } = string.Empty; // JE-XXXX (legacy)
    
    [Column("entry_date")]
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public bool IsManual { get; set; }
    public bool IsPosted { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., JE0001, JE0002)
    /// Note: PublicId is legacy; JournalEntryCode is the new standardized code
    /// </summary>
    public string? JournalEntryCode { get; set; }

    // Phase 5 Enhancement: Track source and reversals
    public Guid? CapitalTransactionId { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
    
    // Phase 5 Enhancement: Track creator and timestamps
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
