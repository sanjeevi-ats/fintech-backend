using System;

namespace Fintech.Core.Domain;

public class AccountMapping
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    
    // Capital account code (e.g., CAP0001)
    public string CapitalAccountCode { get; set; } = string.Empty;
    public Guid CapitalAccountId { get; set; }
    
    // GL account code (e.g., GL-3000 for Capital/Equity)
    public string GLAccountCode { get; set; } = string.Empty;
    public Guid GLAccountId { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
