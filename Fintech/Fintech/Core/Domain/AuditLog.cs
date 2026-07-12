using System;

namespace Fintech.Core.Domain;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string BeforeVal { get; set; } = string.Empty;
    public string AfterVal { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., AUD0001, AUD0002)
    /// </summary>
    public string? AuditLogCode { get; set; }
}
