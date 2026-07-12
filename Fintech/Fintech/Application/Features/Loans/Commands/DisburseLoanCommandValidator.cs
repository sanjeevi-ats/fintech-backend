using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fintech.Infrastructure.Persistence;
using Fintech.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Features.Loans.Commands;

public class DisburseLoanCommandValidator : AbstractValidator<DisburseLoanCommand>
{
    private readonly FinVedaDbContext _dbContext;

    public DisburseLoanCommandValidator(FinVedaDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.LoanCaseId)
            .NotEmpty().WithMessage("Loan case ID is required.")
            .MustAsync(NotBeActiveAsync).WithMessage("Loan is already active. Cannot disburse.");
    }

    private async Task<bool> NotBeActiveAsync(Guid loanCaseId, CancellationToken cancellationToken)
    {
        var loan = await _dbContext.LoanCases.FirstOrDefaultAsync(l => l.Id == loanCaseId, cancellationToken);
        if (loan == null) return true; 
        return loan.Status != LoanStatus.active;
    }
}
