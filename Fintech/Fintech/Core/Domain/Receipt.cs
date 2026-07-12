using System;

namespace Fintech.Core.Domain;

public class Receipt
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string PublicId { get; set; } = string.Empty; // RCP- prefix
    
    public Guid InstallmentId { get; set; }
    public Installment Installment { get; set; } = null!;

    public Guid LoanCaseId { get; set; }
    public LoanCase LoanCase { get; set; } = null!;
    
    public long AmountPaid { get; set; }
    public PaymentMode Mode { get; set; } // Cash/UPI/Bank
    public string UTRRef { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., RCP0001, RCP0002)
    /// Note: PublicId is legacy; ReceiptCode is the new standardized code
    /// </summary>
    public string? ReceiptCode { get; set; }
}
