using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fintech.Core.Domain;

public class CollectionRequest
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    
    // Unique user-friendly ID (e.g. COLREQ0001)
    [Column("request_number")]
    public string RequestNumber { get; set; } = string.Empty;
    
    public Guid InstallmentId { get; set; }
    public Installment Installment { get; set; } = null!;
    
    public Guid LoanCaseId { get; set; }
    public LoanCase LoanCase { get; set; } = null!;
    
    // Amount in Paise
    public long Amount { get; set; }
    
    public PaymentMode PaymentMode { get; set; }
    public string UtrRef { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    
    public CollectionRequestStatus Status { get; set; } = CollectionRequestStatus.Pending;
    
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public long PreviousDueAmount { get; set; }
    public long NewDueAmount { get; set; }
    
    [Column("ip_address")]
    public string IpAddress { get; set; } = string.Empty;
}
