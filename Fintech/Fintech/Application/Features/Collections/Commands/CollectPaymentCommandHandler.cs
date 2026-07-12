using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Fintech.Application.Services;

namespace Fintech.Application.Features.Collections.Commands;

public class CollectPaymentCommandHandler : IRequestHandler<CollectPaymentCommand, Receipt>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisLockService _redisLock;

    public CollectPaymentCommandHandler(IUnitOfWork unitOfWork, IRedisLockService redisLock)
    {
        _unitOfWork = unitOfWork;
        _redisLock = redisLock;
    }

    public async Task<Receipt> Handle(CollectPaymentCommand request, CancellationToken cancellationToken)
    {
        string lockKey = $"lock:installment:{request.InstallmentId}";
        if (!await _redisLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
        {
            throw new FinVedaException(409, "LOCKED", "Collection is already in progress for this installment.");
        }

        try
        {
            var installmentRepo = _unitOfWork.Repository<Installment>();
            var installment = await installmentRepo.GetByIdAsync(request.InstallmentId);
            if (installment == null) throw new FinVedaException(404, "NOT_FOUND", "Installment not found");

            if (installment.Status == InstallmentStatus.paid)
            {
                throw new FinVedaException(409, "ALREADY_COLLECTED", "This installment is already fully paid.");
            }

            if (request.AmountPaid < installment.Amount)
            {
                long remaining = installment.Amount - request.AmountPaid;
                throw new FinVedaException(402, "PAYMENT_REQUIRED", $"Partial payment rejected. Remaining balance: {remaining}");
            }

            installment.Status = InstallmentStatus.paid;
            await installmentRepo.UpdateAsync(installment);

            var receiptCounter = new Random().Next(100, 999); // Mock sequence
            var strDate = DateTime.UtcNow.ToString("yyyyMMdd");

            var receipt = new Receipt
            {
                Id = Guid.NewGuid(),
                BranchId = installment.BranchId,
                PublicId = $"RCP-{strDate}-{receiptCounter}",
                InstallmentId = request.InstallmentId,
                AmountPaid = request.AmountPaid,
                Mode = request.Mode, // Now uses PaymentMode enum (bound from request)
                UTRRef = request.UTRRef,
                CapturedAt = DateTime.UtcNow,
                LoanCaseId = installment.LoanCaseId
            };
            await _unitOfWork.Repository<Receipt>().AddAsync(receipt);

            var je = new JournalEntry
            {
                Id = Guid.NewGuid(),
                BranchId = installment.BranchId,
                PublicId = "JE-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Date = DateTime.UtcNow,
                Description = $"Collection for Installment {installment.No}. GPS: {request.GPS_Lat},{request.GPS_Lng}",
                Reference = receipt.PublicId,
                IsManual = false,
                IsPosted = true
            };

            var loanCaseRepo = _unitOfWork.Repository<LoanCase>();
            var loanCase = await loanCaseRepo.GetByIdAsync(installment.LoanCaseId);

            long principalPortion = loanCase != null ? loanCase.Principal / 12 : request.AmountPaid;
            long interestPortion = request.AmountPaid - principalPortion;

            string cashAccount = "Cash in Hand";
            string portfolioAccount = "Loan Receivables";
            string interestAccount = "Interest Income";

            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, AccountName = cashAccount, Type = "debit", Amount = request.AmountPaid });
            je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, AccountName = portfolioAccount, Type = "credit", Amount = principalPortion });
            
            if (interestPortion > 0)
            {
                je.Lines.Add(new JournalLine { Id = Guid.NewGuid(), BranchId = installment.BranchId, JournalEntryId = je.Id, AccountName = interestAccount, Type = "credit", Amount = interestPortion });
            }

            await _unitOfWork.Repository<JournalEntry>().AddAsync(je);
            await _unitOfWork.CompleteAsync();

            return receipt;
        }
        finally
        {
            await _redisLock.ReleaseLockAsync(lockKey);
        }
    }
}
