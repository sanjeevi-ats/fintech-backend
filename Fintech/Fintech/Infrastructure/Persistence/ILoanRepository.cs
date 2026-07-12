using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Core.Domain;

namespace Fintech.Infrastructure.Persistence;

public interface ILoanRepository : IBaseRepository<LoanCase>
{
    Task<IReadOnlyList<LoanCase>> GetActiveLoansByBranchAsync(Guid branchId);
}
