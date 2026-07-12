using System;
using System.Collections.Generic;

namespace Fintech.Core.Domain;

public class Partner
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public double EquityPct { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User? User { get; set; }
    public ICollection<CapitalAccount> CapitalAccounts { get; set; } = new List<CapitalAccount>();
    
    /// <summary>
    /// Human-readable business code (e.g., PAR0001, PAR0002)
    /// </summary>
    public string? PartnerCode { get; set; }
    
    // Computed properties from User
    public string Name => User?.Name ?? string.Empty;
    public string Email => User?.Email ?? string.Empty;
    public string Phone => string.Empty; // Not in User entity, can be added later
}
