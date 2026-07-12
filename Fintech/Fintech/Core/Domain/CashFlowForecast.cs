using System;

namespace Fintech.Core.Domain;

public class CashFlowForecast
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime ForecastPeriod { get; set; }
    public long ProjectedCashFlow { get; set; } // In Paise
    public int ConfidenceLevel { get; set; } // 1-10 scale
    public string? Assumptions { get; set; }
    public int Status { get; set; } // 1 = Draft, 2 = Generated, 3 = Approved
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
    public virtual Branch? Branch { get; set; }
}

public enum CashFlowForecastStatus { Draft = 1, Generated = 2, Approved = 3 }
