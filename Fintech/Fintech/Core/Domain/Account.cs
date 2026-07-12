using System;

namespace Fintech.Core.Domain;

public class Account
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable business code (e.g., ACC0001, ACC0002)
    /// </summary>
    public string? AccountCode { get; set; }
}
