using System;
using System.Collections.Generic;

namespace Fintech.Core.Domain;

public class AccountingPeriod
{
    public Guid Id { get; set; }
    public string PeriodCode { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Status { get; set; } // 1 = Open, 2 = Closed, 3 = Locked
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public Guid BranchId { get; set; }
    public int Version { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ICollection<AccrualEntry> AccrualEntries { get; set; } = new List<AccrualEntry>();
    public virtual ICollection<ProvisionEntry> ProvisionEntries { get; set; } = new List<ProvisionEntry>();
    public virtual ICollection<PeriodReversal> Reversals { get; set; } = new List<PeriodReversal>();
}
