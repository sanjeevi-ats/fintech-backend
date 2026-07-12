using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class CollectionRequestService : ICollectionRequestService
{
    private readonly FinVedaDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;
    private readonly ICollectionService _collectionService;

    public CollectionRequestService(
        FinVedaDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICodeGenerationService codeService,
        ITenantService tenantService,
        ICollectionService collectionService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _codeService = codeService ?? throw new ArgumentNullException(nameof(codeService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
    }

    public async Task<CollectionRequest> CreateRequestAsync(
        Guid installmentId, 
        long amount, 
        PaymentMode mode, 
        string utrRef, 
        string remarks, 
        string ipAddress)
    {
        var installment = await _dbContext.Installments
            .FirstOrDefaultAsync(i => i.Id == installmentId && i.BranchId == _tenantService.BranchId);

        if (installment == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Installment not found");
        }

        if (installment.Status == InstallmentStatus.paid)
        {
            throw new FinVedaException(409, "ALREADY_COLLECTED", "This installment is already fully paid.");
        }

        // We check if there's already a pending request for this installment to prevent double submission
        var existingPending = await _dbContext.CollectionRequests
            .AnyAsync(cr => cr.InstallmentId == installmentId && cr.Status == CollectionRequestStatus.Pending);
            
        if (existingPending)
        {
            throw new FinVedaException(409, "PENDING_REQUEST_EXISTS", "A pending approval request already exists for this installment.");
        }

        var requestNumber = await _codeService.GenerateCodeAsync("CollectionRequest", _tenantService.BranchId);
        var userId = _tenantService.UserId;

        // If UserId is empty, default to the seed collection officer user for fallback/testing
        if (userId == Guid.Empty)
        {
            userId = Guid.Parse("22222222-2222-2222-2222-222222222225"); // Collection Officer User
        }

        var previousDue = installment.Amount;
        var newDue = Math.Max(0, installment.Amount - amount);

        var request = new CollectionRequest
        {
            Id = Guid.NewGuid(),
            BranchId = _tenantService.BranchId,
            RequestNumber = requestNumber,
            InstallmentId = installmentId,
            LoanCaseId = installment.LoanCaseId,
            Amount = amount,
            PaymentMode = mode,
            UtrRef = utrRef,
            Remarks = remarks,
            Status = CollectionRequestStatus.Pending,
            RequestedById = userId,
            RequestedAt = DateTime.UtcNow,
            PreviousDueAmount = previousDue,
            NewDueAmount = newDue,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress
        };

        await _dbContext.CollectionRequests.AddAsync(request);
        await _dbContext.SaveChangesAsync();

        return request;
    }

    public async Task<CollectionRequest> ApproveRequestAsync(Guid id, Guid approvedById)
    {
        var request = await _dbContext.CollectionRequests
            .Include(cr => cr.Installment)
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.BranchId == _tenantService.BranchId);

        if (request == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Collection request not found");
        }

        if (request.Status != CollectionRequestStatus.Pending)
        {
            throw new FinVedaException(400, "INVALID_STATUS", $"Cannot approve request with status: {request.Status}");
        }

        // Apply database updates by calling the primary collection service
        await _collectionService.CollectInstallmentAsync(
            request.InstallmentId, 
            request.Amount, 
            request.PaymentMode.ToString(), 
            request.UtrRef
        );

        request.Status = CollectionRequestStatus.Approved;
        request.ApprovedById = approvedById == Guid.Empty ? _tenantService.UserId : approvedById;
        request.ApprovedAt = DateTime.UtcNow;

        _dbContext.CollectionRequests.Update(request);
        await _dbContext.SaveChangesAsync();

        return request;
    }

    public async Task<CollectionRequest> RejectRequestAsync(Guid id, Guid approvedById)
    {
        var request = await _dbContext.CollectionRequests
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.BranchId == _tenantService.BranchId);

        if (request == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Collection request not found");
        }

        if (request.Status != CollectionRequestStatus.Pending)
        {
            throw new FinVedaException(400, "INVALID_STATUS", $"Cannot reject request with status: {request.Status}");
        }

        request.Status = CollectionRequestStatus.Rejected;
        request.ApprovedById = approvedById == Guid.Empty ? _tenantService.UserId : approvedById;
        request.ApprovedAt = DateTime.UtcNow;

        _dbContext.CollectionRequests.Update(request);
        await _dbContext.SaveChangesAsync();

        return request;
    }

    public async Task<CollectionRequest> CancelRequestAsync(Guid id, Guid requestedById)
    {
        var request = await _dbContext.CollectionRequests
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.BranchId == _tenantService.BranchId);

        if (request == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Collection request not found");
        }

        if (request.Status != CollectionRequestStatus.Pending)
        {
            throw new FinVedaException(400, "INVALID_STATUS", $"Cannot cancel request with status: {request.Status}");
        }

        request.Status = CollectionRequestStatus.Cancelled;

        _dbContext.CollectionRequests.Update(request);
        await _dbContext.SaveChangesAsync();

        return request;
    }

    public async Task<IReadOnlyList<CollectionRequest>> GetAllRequestsAsync(CollectionRequestStatus? status)
    {
        var query = _dbContext.CollectionRequests
            .Include(cr => cr.Installment)
            .Include(cr => cr.LoanCase)
                .ThenInclude(lc => lc.Customer)
            .Include(cr => cr.RequestedBy)
            .Include(cr => cr.ApprovedBy)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(cr => cr.Status == status.Value);
        }

        return await query
            .OrderByDescending(cr => cr.RequestedAt)
            .ToListAsync();
    }

    public async Task<CollectionRequest?> GetByIdAsync(Guid id)
    {
        return await _dbContext.CollectionRequests
            .Include(cr => cr.Installment)
            .Include(cr => cr.LoanCase)
                .ThenInclude(lc => lc.Customer)
            .Include(cr => cr.RequestedBy)
            .Include(cr => cr.ApprovedBy)
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.BranchId == _tenantService.BranchId);
    }
}
