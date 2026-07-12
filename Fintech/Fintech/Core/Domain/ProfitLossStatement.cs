using System;

namespace Fintech.Core.Domain;

public class ProfitLossStatement
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public long TotalRevenue { get; set; } // In Paise
    public long TotalExpenses { get; set; } // In Paise
    public long GrossProfit { get; set; } // In Paise (Revenue - Expenses)
    public long NetProfit { get; set; } // In Paise (for future use: after tax)
    public decimal ProfitMargin { get; set; } // Percentage (0-100)
    public DateTime StatementDate { get; set; }
    public int Status { get; set; } // 1 = Draft, 2 = Generated, 3 = Finalized, 4 = Archived
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<RevenueLine>? RevenueLines { get; set; }
    public virtual ICollection<ExpenseLine>? ExpenseLines { get; set; }
}

public enum ProfitLossStatus { Draft = 1, Generated = 2, Finalized = 3, Archived = 4 }
