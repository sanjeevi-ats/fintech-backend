using System;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class LoanClosureService : ILoanClosureService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;

    public LoanClosureService(IUnitOfWork unitOfWork, FinVedaDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task CheckAndCloseLoanAsync(Guid loanId)
    {
        try
        {
            // Call the stored procedure
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"CALL proc_check_and_close_loan({loanId})");
        }
        catch (Exception ex)
        {
            throw new FinVedaException(500, "CLOSURE_ERROR", $"Failed to check loan closure: {ex.Message}");
        }
    }

    public async Task ManuallyCloseLoanAsync(Guid loanId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new FinVedaException(400, "INVALID_REASON", "Closure reason is required");
        }

        try
        {
            // Call the stored procedure
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"CALL proc_close_loan_manual({loanId}, {reason})");
        }
        catch (Exception ex)
        {
            throw new FinVedaException(500, "CLOSURE_ERROR", $"Failed to close loan: {ex.Message}");
        }
    }

    public async Task<LoanClosureStatus> VerifyLoanClosureStatusAsync(Guid loanId)
    {
        var loanRepo = _unitOfWork.Repository<LoanCase>();
        var loan = await loanRepo.GetByIdAsync(loanId);
        
        if (loan == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Loan not found");
        }

        // Get installment statistics
        var installments = loan.Installments ?? new List<Installment>();
        var totalInstallments = installments.Count;
        var paidInstallments = installments.Count(i => i.Status == InstallmentStatus.paid);
        var pendingInstallments = totalInstallments - paidInstallments;

        // Get payment statistics from receipts
        var receipts = await _dbContext.Receipts
            .Where(r => r.LoanCaseId == loanId)
            .ToListAsync();
        
        var totalPaid = receipts.Sum(r => r.AmountPaid);
        var collectionPercentage = loan.TotalReceivable > 0 
            ? (decimal)totalPaid / loan.TotalReceivable * 100 
            : 0;

        return new LoanClosureStatus
        {
            LoanId = loanId,
            CurrentStatus = loan.Status.ToString(),
            TotalInstallments = totalInstallments,
            PaidInstallments = paidInstallments,
            PendingInstallments = pendingInstallments,
            TotalReceivable = loan.TotalReceivable,
            TotalPaid = totalPaid,
            IsClosureEligible = pendingInstallments == 0 && loan.Status != LoanStatus.closed,
            CollectionPercentage = collectionPercentage
        };
    }
}
