using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fintech.Core.Domain;

public class Installment
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid LoanCaseId { get; set; }
    public LoanCase LoanCase { get; set; } = null!;
    
    [System.ComponentModel.DataAnnotations.Schema.Column("installment_no")]
    public int No { get; set; }
    public DateTime DueDate { get; set; }
    public long Amount { get; set; }
    public InstallmentStatus Status { get; set; }
    
    /// <summary>
    /// Human-readable business code (e.g., INST0001, INST0002)
    /// </summary>
    public string? InstallmentCode { get; set; }
}
