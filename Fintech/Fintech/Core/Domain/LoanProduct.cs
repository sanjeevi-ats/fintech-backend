using System;

namespace Fintech.Core.Domain;

public class LoanProduct
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public double InterestRate { get; set; }
    public int DefaultTenureMonths { get; set; }
    public Frequency RepaymentFrequency { get; set; }
    public bool IsActive { get; set; } = true;
}
