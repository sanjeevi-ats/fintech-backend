using System;
using Fintech.Core.Domain;

namespace Fintech.Application.DTOs;

public class CollectionRequestDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid InstallmentId { get; set; }
    public string? InstallmentCode { get; set; }
    public int InstallmentNo { get; set; }
    public Guid LoanCaseId { get; set; }
    public string? LoanCode { get; set; }
    public string? CustomerName { get; set; }
    public long Amount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string UtrRef { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    public Guid RequestedById { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime RequestedAt { get; set; }
    
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public long PreviousDueAmount { get; set; }
    public long NewDueAmount { get; set; }
}
