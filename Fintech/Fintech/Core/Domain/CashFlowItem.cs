using System;

namespace Fintech.Core.Domain;

public class CashFlowItem
{
    public Guid Id { get; set; }
    public Guid CashFlowStatementId { get; set; }
    public int Category { get; set; } // 1 = Operating, 2 = Investing, 3 = Financing
    public int ItemType { get; set; } // 1 = Inflow, 2 = Outflow
    public long Amount { get; set; } // In Paise
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; } // Transaction reference
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual CashFlowStatement? CashFlowStatement { get; set; }
}

public enum CashFlowCategory
{
    Operating = 1,
    Investing = 2,
    Financing = 3
}

public enum CashFlowItemType
{
    Inflow = 1,
    Outflow = 2
}
