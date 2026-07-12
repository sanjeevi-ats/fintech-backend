using System;

namespace Fintech.Core.Domain;

public class ExpenseLine
{
    public Guid Id { get; set; }
    public Guid ProfitLossStatementId { get; set; }
    public int Category { get; set; } // 1 = Provisions, 2 = Waivers, 3 = Operating, 4 = Administrative
    public long Amount { get; set; } // In Paise
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; } // GL Account code or reference
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ProfitLossStatement? ProfitLossStatement { get; set; }
}

public enum ExpenseCategory 
{ 
    Provisions = 1, 
    Waivers = 2, 
    Operating = 3, 
    Administrative = 4 
}
