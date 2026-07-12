using System;

namespace Fintech.Core.Domain;

public class DayEnd
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
    public bool IsClosed { get; set; }
    public int JournalsLocked { get; set; }
    public long TotalCollected { get; set; }
    public long Discrepancy { get; set; }
    public bool DiscrepancyResolved { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., DE0001, DE0002)
    /// </summary>
    public string? DayEndCode { get; set; }
}
