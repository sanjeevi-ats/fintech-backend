using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IRecoveryService
{
    Task<IReadOnlyList<LoanCase>> GetOverdueLoansAsync();
    Task RecordFollowUpAsync(Guid loanId, string notes);
}

public class RecoveryService : IRecoveryService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecoveryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LoanCase>> GetOverdueLoansAsync()
    {
        var all = await _unitOfWork.Repository<LoanCase>().ListAllAsync();
        // Simplified overdue logic
        return all.Where(x => x.Status == LoanStatus.active).ToList(); 
    }

    public async Task RecordFollowUpAsync(Guid loanId, string notes)
    {
        // Placeholder for follow-up logic
        await _unitOfWork.CompleteAsync();
    }
}
