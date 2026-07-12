using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using System.Linq;

namespace Fintech.Application.Features.Loans.Commands;

public class DisburseLoanCommandHandler : IRequestHandler<DisburseLoanCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DisburseLoanCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DisburseLoanCommand request, CancellationToken cancellationToken)
    {
        var loanRepo = _unitOfWork.Repository<LoanCase>();
        var loan = await loanRepo.GetByIdAsync(request.LoanCaseId);
        
        if (loan == null) throw new FinVedaException(404, "NOT_FOUND", "Loan case not found.");
        
        if (loan.Status == LoanStatus.active)
            throw new FinVedaException(409, "ALREADY_ACTIVE", "Loan is already disbursed.");

        loan.Status = LoanStatus.active;
        await loanRepo.UpdateAsync(loan);

        var je = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BranchId = loan.BranchId,
            PublicId = "JE-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            Date = DateTime.UtcNow,
            Description = $"Disbursement for Loan {loan.Id}",
            IsManual = false,
            IsPosted = true
        };
        
        long netDisbursement = loan.Principal - loan.ProcessingFees;

        string portfolioAccount = "Loan Receivables";
        string cashAccount = "Cash in Hand";
        string feeIncomeAccount = "Fee Income";

        je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = loan.BranchId, JournalEntryId = je.Id, AccountName = portfolioAccount, Type = "debit", Amount = loan.Principal });
        je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = loan.BranchId, JournalEntryId = je.Id, AccountName = cashAccount, Type = "credit", Amount = netDisbursement });
        
        if (loan.ProcessingFees > 0)
        {
            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = loan.BranchId, JournalEntryId = je.Id, AccountName = feeIncomeAccount, Type = "credit", Amount = loan.ProcessingFees });
        }

        // Validate Golden Rule (Double-Entry Balance)
        long debits = je.Lines.Where(l => l.Type == "debit").Sum(l => l.Amount);
        long credits = je.Lines.Where(l => l.Type == "credit").Sum(l => l.Amount);

        if (debits != credits)
        {
            throw new FinVedaException(422, "UNBALANCED_JOURNAL", $"Journal Entry must balance. Debits: {debits}, Credits: {credits}");
        }

        var journalRepo = _unitOfWork.Repository<JournalEntry>();
        await journalRepo.AddAsync(je);

        await _unitOfWork.CompleteAsync();

        return true;
    }
}
