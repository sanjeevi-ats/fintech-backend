using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Core.Domain;

namespace Fintech.Application.Services;

public interface ICollectionService
{
    Task<Receipt?> GetCollectionByCodeAsync(string code);
    Task<Receipt> CollectInstallmentAsync(Guid installmentId, long amountPaid, string mode, string utrRef);
    Task<List<SyncResult>> SyncOfflineCollectionsAsync(List<OfflineCollection> offlineCollections);
}

public class SyncResult
{
    public string LocalId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? ReceiptId { get; set; }
}

public class OfflineCollection
{
    public string LocalId { get; set; } = string.Empty;
    public Guid InstallmentId { get; set; }
    public long AmountPaid { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string UTRRef { get; set; } = string.Empty;
}
