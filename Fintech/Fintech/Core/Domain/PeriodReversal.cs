using System;

namespace Fintech.Core.Domain;

public class PeriodReversal
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public string ReversedBy { get; set; } = "";
    public string? Reason { get; set; }
    public int? DeletedAccruals { get; set; }
    public int? DeletedProvisions { get; set; }
    public int? DeletedJournalEntries { get; set; }
    public DateTime ReversedAt { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
}
