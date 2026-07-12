using System;
using MediatR;

namespace Fintech.Application.Features.Loans.Commands;

public class DisburseLoanCommand : IRequest<bool>
{
    public Guid LoanCaseId { get; set; }
}
