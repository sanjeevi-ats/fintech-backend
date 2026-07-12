using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services
{
    public class InterestReportingService
    {
        private readonly FinVedaDbContext _context;
        private readonly InterestCalculationService _calculationService;
        private readonly InterestPostingService _postingService;
        private readonly InterestWaiverService _waiverService;

        public InterestReportingService(
            FinVedaDbContext context,
            InterestCalculationService calculationService,
            InterestPostingService postingService,
            InterestWaiverService waiverService)
        {
            _context = context;
            _calculationService = calculationService;
            _postingService = postingService;
            _waiverService = waiverService;
        }

        /// <summary>
        /// Generate comprehensive interest statement for a customer
        /// Shows all interest transactions across all their loans
        /// </summary>
        public async Task<InterestStatementDto> GenerateInterestStatementAsync(
            Guid customerId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(new object[] { customerId }, cancellationToken: ct);
                if (customer == null) return new InterestStatementDto();

                fromDate ??= DateTime.UtcNow.AddMonths(-12); // Default 1 year
                toDate ??= DateTime.UtcNow;

                // Get all loans for this customer
                var loans = await _context.LoanCases
                    .Where(l => l.CustomerId == customerId)
                    .ToListAsync(ct);

                var statement = new InterestStatementDto
                {
                    CustomerId = customerId,
                    CustomerName = customer.Name,
                    FromDate = fromDate.Value,
                    ToDate = toDate.Value,
                    StatementDate = DateTime.UtcNow,
                    LoanStatements = new List<LoanInterestStatementDto>()
                };

                long totalCalculated = 0;
                long totalPosted = 0;
                long totalWaived = 0;
                long totalPending = 0;

                // Generate statement for each loan
                foreach (var loan in loans)
                {
                    var loanSummary = await _calculationService.GetInterestSummaryAsync(loan.Id, ct);
                    
                    var loanStatement = new LoanInterestStatementDto
                    {
                        LoanId = loan.Id,
                        LoanCode = loan.LoanCode ?? "",
                        Principal = loan.Principal,
                        TotalCalculated = loanSummary.TotalCalculated,
                        TotalPosted = loanSummary.TotalPosted,
                        TotalWaived = loanSummary.TotalWaived,
                        PendingInterest = loanSummary.PendingInterest,
                        CollectionStatus = GetCollectionStatus(loanSummary)
                    };

                    statement.LoanStatements.Add(loanStatement);

                    totalCalculated += loanSummary.TotalCalculated;
                    totalPosted += loanSummary.TotalPosted;
                    totalWaived += loanSummary.TotalWaived;
                    totalPending += loanSummary.PendingInterest;
                }

                statement.TotalCalculated = totalCalculated;
                statement.TotalPosted = totalPosted;
                statement.TotalWaived = totalWaived;
                statement.TotalPending = totalPending;

                return statement;
            }
            catch { return new InterestStatementDto(); }
        }

        /// <summary>
        /// Get interest aging analysis - shows overdue interest not collected
        /// </summary>
        public async Task<InterestAgingDto> GetInterestAgingAsync(
            Guid? customerId = null,
            CancellationToken ct = default)
        {
            try
            {
                var aging = new InterestAgingDto
                {
                    AsOfDate = DateTime.UtcNow,
                    AgingBuckets = new Dictionary<string, long>
                    {
                        { "0-30 days", 0L },
                        { "31-60 days", 0L },
                        { "61-90 days", 0L },
                        { "91-180 days", 0L },
                        { "180+ days", 0L }
                    }
                };

                // Get all loans
                IQueryable<LoanCase> loansQuery = _context.LoanCases;
                if (customerId.HasValue)
                {
                    loansQuery = loansQuery.Where(l => l.CustomerId == customerId.Value);
                }

                var loans = await loansQuery.ToListAsync(ct);

                foreach (var loan in loans)
                {
                    // Get pending/posted interest
                    var calculations = await _calculationService.GetInterestByLoanAsync(loan.Id, ct);
                    var postings = await _postingService.GetPostingHistoryAsync(loan.Id, ct);

                    // Calculate age of oldest unposted interest
                    var oldestCalculation = calculations
                        .Where(c => c.Status != (int)InterestCalculationStatus.Posted)
                        .OrderBy(c => c.CalculationDate)
                        .FirstOrDefault();

                    if (oldestCalculation != null)
                    {
                        var daysOld = (int)(DateTime.UtcNow - oldestCalculation.CalculationDate).TotalDays;
                        long amount = calculations
                            .Where(c => c.Status != (int)InterestCalculationStatus.Posted)
                            .Sum(c => c.AccrualAmount);

                        if (daysOld <= 30)
                            aging.AgingBuckets["0-30 days"] += amount;
                        else if (daysOld <= 60)
                            aging.AgingBuckets["31-60 days"] += amount;
                        else if (daysOld <= 90)
                            aging.AgingBuckets["61-90 days"] += amount;
                        else if (daysOld <= 180)
                            aging.AgingBuckets["91-180 days"] += amount;
                        else
                            aging.AgingBuckets["180+ days"] += amount;
                    }
                }

                aging.TotalOutstanding = aging.AgingBuckets.Values.Sum();
                return aging;
            }
            catch { return new InterestAgingDto { AsOfDate = DateTime.UtcNow }; }
        }

        /// <summary>
        /// Get interest summary across all customers/loans
        /// </summary>
        public async Task<InterestSummaryReportDto> GetSystemInterestSummaryAsync(CancellationToken ct = default)
        {
            try
            {
                var report = new InterestSummaryReportDto
                {
                    AsOfDate = DateTime.UtcNow,
                    TotalLoans = await _context.LoanCases.CountAsync(ct),
                    TotalCalculated = 0,
                    TotalPosted = 0,
                    TotalWaived = 0,
                    TotalPending = 0,
                    AverageDailyRate = 0m
                };

                var calculations = await _context.InterestCalculations
                    .Where(ic => !ic.IsDeleted)
                    .ToListAsync(ct);

                var postings = await _context.InterestPostings
                    .Where(ip => !ip.IsDeleted)
                    .ToListAsync(ct);

                var waivers = await _context.InterestWaivers
                    .Where(iw => iw.Status == (int)InterestWaiverStatus.Approved && !iw.IsDeleted)
                    .ToListAsync(ct);

                report.TotalCalculated = calculations.Sum(c => c.AccrualAmount);
                report.TotalPosted = postings.Sum(p => p.PostedAmount);
                report.TotalWaived = waivers.Sum(w => w.WaiverAmount);
                report.TotalPending = report.TotalCalculated - report.TotalPosted - report.TotalWaived;

                if (calculations.Any())
                {
                    var avgDailyRate = calculations.Average(c => (double)c.DailyRate);
                    report.AverageDailyRate = (decimal)avgDailyRate;
                }

                report.PostingPercentage = report.TotalCalculated > 0 
                    ? (decimal)(report.TotalPosted * 100.0 / report.TotalCalculated) 
                    : 0m;

                report.WaiverPercentage = report.TotalCalculated > 0 
                    ? (decimal)(report.TotalWaived * 100.0 / report.TotalCalculated) 
                    : 0m;

                return report;
            }
            catch { return new InterestSummaryReportDto { AsOfDate = DateTime.UtcNow }; }
        }

        /// <summary>
        /// Get interest summary by branch - useful for comparing interest across branches
        /// </summary>
        public async Task<List<InterestByProductDto>> GetInterestByBranchAsync(CancellationToken ct = default)
        {
            try
            {
                var branches = await _context.Branches.ToListAsync(ct);
                var result = new List<InterestByProductDto>();

                foreach (var branch in branches)
                {
                    var loansInBranch = await _context.LoanCases
                        .Where(l => l.BranchId == branch.Id)
                        .ToListAsync(ct);

                    long totalCalculated = 0;
                    long totalPosted = 0;
                    long totalWaived = 0;
                    int loanCount = 0;

                    foreach (var loan in loansInBranch)
                    {
                        var summary = await _calculationService.GetInterestSummaryAsync(loan.Id, ct);
                        totalCalculated += summary.TotalCalculated;
                        totalPosted += summary.TotalPosted;
                        totalWaived += summary.TotalWaived;
                        loanCount++;
                    }

                    var branchSummary = new InterestByProductDto
                    {
                        BranchId = branch.Id,
                        BranchName = branch.Name,
                        LoanCount = loanCount,
                        TotalCalculated = totalCalculated,
                        TotalPosted = totalPosted,
                        TotalWaived = totalWaived,
                        AveragePerLoan = loanCount > 0 ? totalCalculated / loanCount : 0
                    };

                    result.Add(branchSummary);
                }

                return result.OrderByDescending(x => x.TotalCalculated).ToList();
            }
            catch { return new List<InterestByProductDto>(); }
        }

        private string GetCollectionStatus(InterestSummaryDto summary)
        {
            if (summary.TotalCalculated == 0) return "No Interest";
            if (summary.TotalPosted == 0) return "Pending Posting";
            
            decimal collectionRate = summary.TotalPosted > 0 
                ? (decimal)(summary.TotalPosted * 100.0 / summary.TotalCalculated) 
                : 0m;

            return collectionRate switch
            {
                >= 100 => "Fully Collected",
                >= 75 => "Mostly Collected",
                >= 50 => "Partially Collected",
                >= 25 => "Minimal Collection",
                _ => "Not Collected"
            };
        }
    }

    // DTO Classes
    public class InterestStatementDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime StatementDate { get; set; }
        public long TotalCalculated { get; set; }
        public long TotalPosted { get; set; }
        public long TotalWaived { get; set; }
        public long TotalPending { get; set; }
        public List<LoanInterestStatementDto> LoanStatements { get; set; } = new();
    }

    public class LoanInterestStatementDto
    {
        public Guid LoanId { get; set; }
        public string LoanCode { get; set; } = "";
        public long Principal { get; set; }
        public long TotalCalculated { get; set; }
        public long TotalPosted { get; set; }
        public long TotalWaived { get; set; }
        public long PendingInterest { get; set; }
        public string CollectionStatus { get; set; } = "";
    }

    public class InterestAgingDto
    {
        public DateTime AsOfDate { get; set; }
        public Dictionary<string, long> AgingBuckets { get; set; } = new();
        public long TotalOutstanding { get; set; }
    }

    public class InterestSummaryReportDto
    {
        public DateTime AsOfDate { get; set; }
        public int TotalLoans { get; set; }
        public long TotalCalculated { get; set; }
        public long TotalPosted { get; set; }
        public long TotalWaived { get; set; }
        public long TotalPending { get; set; }
        public decimal AverageDailyRate { get; set; }
        public decimal PostingPercentage { get; set; }
        public decimal WaiverPercentage { get; set; }
    }

    public class InterestByProductDto
    {
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = "";
        public int LoanCount { get; set; }
        public long TotalCalculated { get; set; }
        public long TotalPosted { get; set; }
        public long TotalWaived { get; set; }
        public long AveragePerLoan { get; set; }
    }
}
