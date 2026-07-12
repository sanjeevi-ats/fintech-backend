using System;

namespace Fintech.Core.Domain;

public class InterestCalculation
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid? PeriodId { get; set; }
    public decimal DailyRate { get; set; } // Interest rate per day (e.g., 0.000274 for 10% annual)
    public long AccrualAmount { get; set; } // Daily accrual in Paise
    public DateTime CalculationDate { get; set; }
    public int Status { get; set; } // 1 = Calculated, 2 = Posted, 3 = Waived
    public int InterestType { get; set; } // 1 = Fixed, 2 = Declining, 3 = Variable, 4 = Step-up
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual LoanCase? Loan { get; set; }
    public virtual AccountingPeriod? Period { get; set; }
}

public enum InterestCalculationStatus { Calculated = 1, Posted = 2, Waived = 3 }
public enum InterestType { Fixed = 1, Declining = 2, Variable = 3, StepUp = 4 }
