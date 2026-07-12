using System;

namespace Fintech.Core.Domain;

public class Branch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string SettingsJson { get; set; } = "{}";
    
    /// <summary>
    /// Human-readable business code (e.g., BR0001, BR0002)
    /// </summary>
    public string? BranchCode { get; set; }
}
