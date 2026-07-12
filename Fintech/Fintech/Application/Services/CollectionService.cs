using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly IRedisLockService _redisLock;
    private readonly ILoanClosureService _loanClosureService;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public CollectionService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        IRedisLockService redisLock,
        ILoanClosureService loanClosureService,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _redisLock = redisLock;
        _loanClosureService = loanClosureService;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<Receipt?> GetCollectionByCodeAsync(string code)
    {
        return await _dbContext.Receipts
            .FirstOrDefaultAsync(r => r.ReceiptCode == code && r.BranchId == _tenantService.BranchId);
    }

    public async Task<Receipt> CollectInstallmentAsync(Guid installmentId, long amountPaid, string mode, string utrRef)
    {
        string lockKey = $"lock:installment:{installmentId}";
        if (!await _redisLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
        {
            throw new FinVedaException(409, "LOCKED", "Collection is already in progress for this installment.");
        }

        try
        {
            var installmentRepo = _unitOfWork.Repository<Installment>();
            var installment = await installmentRepo.GetByIdAsync(installmentId);
            if (installment == null) throw new FinVedaException(404, "NOT_FOUND", "Installment not found");

            if (installment.Status == InstallmentStatus.paid)
            {
                throw new FinVedaException(409, "ALREADY_COLLECTED", "This installment is already fully paid.");
            }

            if (amountPaid < installment.Amount)
            {
                long remaining = installment.Amount - amountPaid;
                throw new FinVedaException(402, "PAYMENT_REQUIRED", $"Partial payment rejected. Remaining balance: {remaining}");
            }

            installment.Status = InstallmentStatus.paid;
            await installmentRepo.UpdateAsync(installment);

            var receipt = new Receipt
            {
                Id = Guid.NewGuid(),
                BranchId = installment.BranchId,
                ReceiptCode = await _codeService.GenerateCodeAsync("Receipt", installment.BranchId),
                PublicId = "RCP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                InstallmentId = installmentId,
                LoanCaseId = installment.LoanCaseId,
                AmountPaid = amountPaid,
                Mode = (PaymentMode)Enum.Parse(typeof(PaymentMode), mode, true),
                UTRRef = utrRef,
                CapturedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Receipt>().AddAsync(receipt);

            var loanCaseRepo = _unitOfWork.Repository<LoanCase>();
            var loanCase = await loanCaseRepo.GetByIdAsync(installment.LoanCaseId);

            long principalPortion = loanCase != null ? loanCase.Principal / 12 : amountPaid;
            long interestPortion = amountPaid - principalPortion;

            var je = new JournalEntry
            {
                Id = Guid.NewGuid(),
                BranchId = installment.BranchId,
                JournalEntryCode = await _codeService.GenerateCodeAsync("JournalEntry", installment.BranchId),
                PublicId = "JE-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Date = DateTime.UtcNow,
                Description = $"Collection for Installment {installment.No}",
                Reference = receipt.PublicId,
                IsManual = false,
                IsPosted = true
            };

            string cashAccount = "Cash in Hand";
            string portfolioAccount = "Loan Receivables";
            string interestAccount = "Interest Income";

            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, JournalLineCode = await _codeService.GenerateCodeAsync("JournalLine", installment.BranchId), AccountName = cashAccount, Type = "debit", Amount = amountPaid });
            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, JournalLineCode = await _codeService.GenerateCodeAsync("JournalLine", installment.BranchId), AccountName = portfolioAccount, Type = "credit", Amount = principalPortion });
            
            if (interestPortion > 0)
            {
                je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, JournalLineCode = await _codeService.GenerateCodeAsync("JournalLine", installment.BranchId), AccountName = interestAccount, Type = "credit", Amount = interestPortion });
            }

            await _unitOfWork.Repository<JournalEntry>().AddAsync(je);
            await _unitOfWork.CompleteAsync();

            // Check if loan should be automatically closed
            await _loanClosureService.CheckAndCloseLoanAsync(installment.LoanCaseId);

            return receipt;
        }
        finally
        {
            await _redisLock.ReleaseLockAsync(lockKey);
        }
    }

    public async Task<List<SyncResult>> SyncOfflineCollectionsAsync(List<OfflineCollection> offlineCollections)
    {
        var results = new List<SyncResult>();
        foreach (var req in offlineCollections)
        {
            try
            {
                var receipt = await CollectInstallmentAsync(req.InstallmentId, req.AmountPaid, req.Mode, req.UTRRef);
                results.Add(new SyncResult { LocalId = req.LocalId, Success = true, ReceiptId = receipt.Id.ToString() });
            }
            catch (FinVedaException ex)
            {
                results.Add(new SyncResult { LocalId = req.LocalId, Success = false, Error = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                results.Add(new SyncResult { LocalId = req.LocalId, Success = false, Error = ex.Message });
            }
        }
        return results;
    }
}
