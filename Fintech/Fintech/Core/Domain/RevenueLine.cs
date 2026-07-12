using System;

namespace Fintech.Core.Domain;

public class RevenueLine
{
    public Guid Id { get; set; }
    public Guid ProfitLossStatementId { get; set; }
    public int Category { get; set; } // 1 = Interest Income, 2 = Fees, 3 = Penalties, 4 = Other
    public long Amount { get; set; } // In Paise
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; } // GL Account code or reference
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ProfitLossStatement? ProfitLossStatement { get; set; }
}

public enum RevenueCategory 
{ 
    InterestIncome = 1, 
    Fees = 2, 
    Penalties = 3, 
    Other = 4 
}
