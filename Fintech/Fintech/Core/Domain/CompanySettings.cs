using System;

namespace Fintech.Core.Domain;

public class CompanySettings
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? GstNumber { get; set; }
    public string? LicenseNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Logo { get; set; }
    public string? Tagline { get; set; }
    public string? BusinessType { get; set; }
    
    // Receipt settings
    public string ReceiptPrefix { get; set; } = "RCP";
    public string? ReceiptFooterText { get; set; }
    public bool ShowBranchDetails { get; set; } = true;
    public bool ShowTerminalDetails { get; set; } = true;
    public bool AutoEmailReceipts { get; set; } = false;
    public string ReceiptLanguage { get; set; } = "en";
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
