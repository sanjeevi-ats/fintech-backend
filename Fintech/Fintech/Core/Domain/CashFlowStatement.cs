using System;

namespace Fintech.Core.Domain;

public class CashFlowStatement
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public long OperatingCashFlow { get; set; } // In Paise
    public long InvestingCashFlow { get; set; } // In Paise
    public long FinancingCashFlow { get; set; } // In Paise
    public long NetCashFlow { get; set; } // In Paise
    public long BeginningBalance { get; set; } // In Paise
    public long EndingBalance { get; set; } // In Paise
    public DateTime StatementDate { get; set; }
    public int Status { get; set; } // 1 = Draft, 2 = Generated, 3 = Finalized, 4 = Archived
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? Period { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<CashFlowItem>? CashFlowItems { get; set; }
}

public enum CashFlowStatementStatus { Draft = 1, Generated = 2, Finalized = 3, Archived = 4 }
