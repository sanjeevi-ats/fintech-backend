using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IPartnerService
{
    Task<Partner?> GetByIdAsync(Guid id);
    Task<Partner?> GetByCodeAsync(string code);
    Task<IReadOnlyList<Partner>> GetAllAsync();
    Task<Partner> CreateAsync(Partner partner);
    Task UpdateAsync(Partner partner);
}

public class PartnerService : IPartnerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly ICodeGenerationService _codeService;

    public PartnerService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ITenantService tenantService,
        ICodeGenerationService codeService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _tenantService = tenantService;
        _codeService = codeService;
    }

    public async Task<Partner?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Partners
            .IgnoreQueryFilters()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Partner?> GetByCodeAsync(string code)
    {
        return await _dbContext.Partners
            .FirstOrDefaultAsync(p => p.PartnerCode == code && p.BranchId == _tenantService.BranchId);
    }

    public async Task<IReadOnlyList<Partner>> GetAllAsync()
    {
        return await _dbContext.Partners
            .Include(p => p.User)
            .ToListAsync();
    }

    public async Task<Partner> CreateAsync(Partner partner)
    {
        partner.Id = Guid.NewGuid();
        partner.BranchId = _tenantService.BranchId;
        partner.PartnerCode = await _codeService.GenerateCodeAsync("Partner", _tenantService.BranchId);
        await _unitOfWork.Repository<Partner>().AddAsync(partner);
        await _unitOfWork.CompleteAsync();
        
        // Reload with User navigation
        return await GetByIdAsync(partner.Id) ?? partner;
    }

    public async Task UpdateAsync(Partner partner)
    {
        await _unitOfWork.Repository<Partner>().UpdateAsync(partner);
        await _unitOfWork.CompleteAsync();
    }
}
