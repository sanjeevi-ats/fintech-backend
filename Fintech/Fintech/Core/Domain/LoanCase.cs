using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fintech.Core.Domain;

public class LoanCase
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    // Loan code for user-friendly tracking (LN00001, LN00002, etc.)
    [Column("loan_code")]
    public string? LoanCode { get; set; }
    
    // Money fields in Paise
    [Column("finance_amount")]
    public long Principal { get; set; }
    public long InterestAmount { get; set; }
    public long TotalReceivable { get; set; }
    
    [Column("file_charges_amount")]
    public long ProcessingFees { get; set; }
    public LoanStatus Status { get; set; }
    
    [ConcurrencyCheck]
    public long Version { get; set; }

    // Workflow metadata
    public Guid? SubmittedById { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    [Column("document_urls")]
    public string? DocumentUrls { get; set; }

    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
