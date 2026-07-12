using System;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface ILoanClosureService
{
    /// <summary>
    /// Automatically checks if a loan should be closed (all installments paid)
    /// and closes it if eligible. Called after each payment.
    /// </summary>
    Task CheckAndCloseLoanAsync(Guid loanId);
    
    /// <summary>
    /// Manually closes a loan with a reason (e.g., NPA, customer request)
    /// </summary>
    Task ManuallyCloseLoanAsync(Guid loanId, string reason);
    
    /// <summary>
    /// Verifies the closure status of a loan
    /// </summary>
    Task<LoanClosureStatus> VerifyLoanClosureStatusAsync(Guid loanId);
}

public class LoanClosureStatus
{
    public Guid LoanId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int PendingInstallments { get; set; }
    public long TotalReceivable { get; set; }
    public long TotalPaid { get; set; }
    public bool IsClosureEligible { get; set; }
    public decimal CollectionPercentage { get; set; }
}
