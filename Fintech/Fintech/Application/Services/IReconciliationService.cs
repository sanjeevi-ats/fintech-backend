using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fintech.Application.Services
{
    /// <summary>
    /// Reconciliation service for ledger and financial reconciliation
    /// Phase 5: Ledger reconciliation support
    /// Phase 6: Month-end close reconciliation support
    /// </summary>
    public interface IReconciliationService
    {
        Task<ReconciliationReportDto> GenerateReconciliationReportAsync(Guid branchId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<List<ReconciliationDiscrepancyDto>> GetDiscrepanciesAsync(Guid branchId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }

    public class ReconciliationReportDto
    {
        public Guid BranchId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long TotalDebits { get; set; } // In Paise
        public long TotalCredits { get; set; } // In Paise
        public bool IsBalanced { get; set; }
        public List<ReconciliationDiscrepancyDto> Discrepancies { get; set; } = new();
    }

    public class ReconciliationDiscrepancyDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public long SystemAmount { get; set; } // In Paise
        public long ActualAmount { get; set; } // In Paise
        public DateTime DiscrepancyDate { get; set; }
        public string Category { get; set; } // Bank, Loan, Collection, etc.
    }

    // Phase 6: Bank Reconciliation Result
    public class ReconciliationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long MatchedAmount { get; set; } // In Paise
        public long UnmatchedAmount { get; set; } // In Paise
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
    }

    public class DiscrepancyDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public long SystemAmount { get; set; } // In Paise
        public long ActualAmount { get; set; } // In Paise
        public DateTime DiscrepancyDate { get; set; }
    }
}

