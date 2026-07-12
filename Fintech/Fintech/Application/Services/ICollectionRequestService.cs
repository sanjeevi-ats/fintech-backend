using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Core.Domain;

namespace Fintech.Application.Services;

public interface ICollectionRequestService
{
    Task<CollectionRequest> CreateRequestAsync(Guid installmentId, long amount, PaymentMode mode, string utrRef, string remarks, string ipAddress);
    Task<CollectionRequest> ApproveRequestAsync(Guid id, Guid approvedById);
    Task<CollectionRequest> RejectRequestAsync(Guid id, Guid approvedById);
    Task<CollectionRequest> CancelRequestAsync(Guid id, Guid requestedById);
    Task<IReadOnlyList<CollectionRequest>> GetAllRequestsAsync(CollectionRequestStatus? status);
    Task<CollectionRequest?> GetByIdAsync(Guid id);
}
