using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;

namespace Fintech.Infrastructure.Persistence;

public class LoanRepository : BaseRepository<LoanCase>, ILoanRepository
{
    public LoanRepository(FinVedaDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<LoanCase>> GetActiveLoansByBranchAsync(Guid branchId)
    {
        return await _dbContext.LoanCases
            .Where(l => l.BranchId == branchId && l.Status == LoanStatus.active)
            .ToListAsync();
    }
}
