using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IReceiptService
{
    Task<Receipt?> GetByCodeAsync(string code);
    Task<Receipt> RecordPaymentAsync(Receipt receipt);
}

public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ILogger<ReceiptService> _logger;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public ReceiptService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ILogger<ReceiptService> logger,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<Receipt?> GetByCodeAsync(string code)
    {
        return await _dbContext.Receipts
            .FirstOrDefaultAsync(r => r.ReceiptCode == code && r.BranchId == _tenantService.BranchId);
    }

    public async Task<Receipt> RecordPaymentAsync(Receipt receipt)
    {
        var instRepo = _unitOfWork.Repository<Installment>();
        var installment = await instRepo.GetByIdAsync(receipt.InstallmentId);
        
        if (installment == null) throw new FinVedaException(404, "NOT_FOUND", "Installment not found");

        receipt.Id = Guid.NewGuid();
        receipt.ReceiptCode = await _codeService.GenerateCodeAsync("Receipt", _tenantService.BranchId);
        receipt.PublicId = "REC-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        receipt.LoanCaseId = installment.LoanCaseId;
        receipt.CapturedAt = DateTime.UtcNow;

        // Update installment status
        if (receipt.AmountPaid >= installment.Amount)
        {
            installment.Status = InstallmentStatus.paid;
        }

        await _unitOfWork.Repository<Receipt>().AddAsync(receipt);
        await instRepo.UpdateAsync(installment);
        await _unitOfWork.CompleteAsync();
        
        return receipt;
    }
}
