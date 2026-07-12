using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fintech.Core.Domain;

public class JournalLine
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    
    [Column("account_name")]
    public string AccountName { get; set; } = string.Empty;
    
    // Phase 5 Enhancement: Track GL account code for posting
    [Column("account_code")]
    public string AccountCode { get; set; } = string.Empty;
    
    [Column("entry_type")]
    public string Type { get; set; } = string.Empty;
    public long Amount { get; set; } // Amount in Paise
    
    /// <summary>
    /// Human-readable business code (e.g., JL0001, JL0002)
    /// </summary>
    public string? JournalLineCode { get; set; }
}
