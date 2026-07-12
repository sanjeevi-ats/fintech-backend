using System;

namespace Fintech.Core.Domain;

public class ProfitDistribution
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Period { get; set; } = string.Empty;
    public long PayoutAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;
    
    /// <summary>
    /// Human-readable business code (e.g., PFT0001, PFT0002)
    /// </summary>
    public string? ProfitDistributionCode { get; set; }
}
