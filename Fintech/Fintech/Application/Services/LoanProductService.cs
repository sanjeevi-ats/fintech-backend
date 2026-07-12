using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface ILoanProductService
{
    Task<LoanProduct?> GetByCodeAsync(string code);
    Task<IReadOnlyList<LoanProduct>> GetActiveProductsAsync();
    Task<LoanProduct> CreateProductAsync(LoanProduct product);
    Task DeactivateProductAsync(Guid id);
    Task UpdateProductAsync(LoanProduct product);
}

public class LoanProductService : ILoanProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public LoanProductService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<LoanProduct?> GetByCodeAsync(string code)
    {
        return await _dbContext.LoanProducts
            .FirstOrDefaultAsync(p => p.Code == code && p.BranchId == _tenantService.BranchId);
    }

    public async Task<IReadOnlyList<LoanProduct>> GetActiveProductsAsync()
    {
        var all = await _unitOfWork.Repository<LoanProduct>().ListAllAsync();
        return all.Where(x => x.IsActive).ToList();
    }

    public async Task<LoanProduct> CreateProductAsync(LoanProduct product)
    {
        product.Id = Guid.NewGuid();
        product.BranchId = _tenantService.BranchId;
        product.Code = await _codeService.GenerateCodeAsync("LoanProduct", _tenantService.BranchId);
        product.IsActive = true;
        await _unitOfWork.Repository<LoanProduct>().AddAsync(product);
        await _unitOfWork.CompleteAsync();
        return product;
    }

    public async Task DeactivateProductAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<LoanProduct>();
        var product = await repo.GetByIdAsync(id);
        if (product != null)
        {
            product.IsActive = false;
            await repo.UpdateAsync(product);
            await _unitOfWork.CompleteAsync();
        }
    }

    public async Task UpdateProductAsync(LoanProduct product)
    {
        var repo = _unitOfWork.Repository<LoanProduct>();
        var existing = await repo.GetByIdAsync(product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Code = product.Code;
            existing.InterestRate = product.InterestRate;
            existing.DefaultTenureMonths = product.DefaultTenureMonths;
            existing.RepaymentFrequency = product.RepaymentFrequency;
            existing.IsActive = product.IsActive;
            
            await repo.UpdateAsync(existing);
            await _unitOfWork.CompleteAsync();
        }
    }
}
