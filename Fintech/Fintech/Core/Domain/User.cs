using System;

namespace Fintech.Core.Domain;

public class User
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool TotpEnabled { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable business code (e.g., USR0001, USR0002)
    /// </summary>
    public string? UserCode { get; set; }
}
