using System;

namespace Fintech.Core.Domain;

/// <summary>
/// Tracks business code sequences for all entities.
/// Each entity per branch maintains its own sequence counter.
/// Example: Customer codes in Branch-1 use sequence starting from 1, Branch-2 uses its own sequence.
/// </summary>
public class CodeSequence
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Entity name (e.g., "Customer", "LoanCase", "Partner")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// Branch ID for branch-specific sequence isolation
    /// </summary>
    public Guid BranchId { get; set; }
    
    /// <summary>
    /// Next sequence number to be used (1, 2, 3, etc.)
    /// </summary>
    public int NextSequenceNumber { get; set; } = 1;
    
    /// <summary>
    /// Code prefix for this entity (e.g., "CUS", "LN", "PAR")
    /// </summary>
    public string CodePrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
