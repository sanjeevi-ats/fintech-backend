using System;

namespace Fintech.Core.Domain;

public class Customer
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string Aadhaar_Encrypted { get; set; } = string.Empty;
    public string PAN_Encrypted { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable business code (e.g., CUS0001, CUS0002)
    /// This will be populated programmatically since DB column doesn't exist yet
    /// </summary>
    public string? CustomerCode { get; set; }
}
