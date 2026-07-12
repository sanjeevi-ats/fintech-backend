using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IReportPdfService
{
    /// <summary>
    /// Generate individual customer loan report PDF
    /// </summary>
    Task<byte[]> GenerateCustomerLoanReportPdfAsync(Guid customerId);
    
    /// <summary>
    /// Generate company turnover report PDF
    /// </summary>
    Task<byte[]> GenerateTurnoverReportPdfAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Generate Profit & Loss report PDF
    /// </summary>
    Task<byte[]> GeneratePnLReportPdfAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Generate partner investment report PDF
    /// </summary>
    Task<byte[]> GeneratePartnerReportPdfAsync(Guid partnerId);
    
    /// <summary>
    /// Generate portfolio at risk (PAR) report PDF
    /// </summary>
    Task<byte[]> GenerateParReportPdfAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Generate collection efficiency report PDF
    /// </summary>
    Task<byte[]> GenerateCollectionEfficiencyReportPdfAsync(DateTime startDate, DateTime endDate);
}

public class CustomerLoanReportDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public List<LoanReportDetailDto> Loans { get; set; } = new();
    public long TotalLoans { get; set; }
    public long TotalDisbursed { get; set; }
    public long TotalCollected { get; set; }
    public long TotalOutstanding { get; set; }
}

public class LoanReportDetailDto
{
    public string LoanId { get; set; } = string.Empty;
    public long LoanAmount { get; set; }
    public DateTime DisbursedDate { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public long CollectedAmount { get; set; }
    public long OutstandingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TurnoverReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int ClosedLoans { get; set; }
    public long TotalDisbursed { get; set; }
    public long TotalCollected { get; set; }
    public long AverageLoanSize { get; set; }
    public int TotalCustomers { get; set; }
    public decimal CollectionEfficiency { get; set; }
    public decimal PortfolioAtRisk { get; set; }
}

public class PnLReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long InterestIncome { get; set; }
    public long ProcessingFees { get; set; }
    public long TotalRevenue { get; set; }
    public long OperatingExpenses { get; set; }
    public long ProvisionForLosses { get; set; }
    public long TotalExpenses { get; set; }
    public long NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<PnLLineItemDto> LineItems { get; set; } = new();
}

public class PnLLineItemDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Type { get; set; } = string.Empty; // Income or Expense
}

public class PartnerReportDto
{
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerEmail { get; set; } = string.Empty;
    public string PartnerPhone { get; set; } = string.Empty;
    public long TotalInvestment { get; set; }
    public decimal EquityPercentage { get; set; }
    public long TotalProfitEarned { get; set; }
    public long TotalWithdrawals { get; set; }
    public long CurrentBalance { get; set; }
    public List<PartnerTransactionDto> Transactions { get; set; } = new();
}

public class PartnerTransactionDto
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // Investment, Withdrawal, Profit
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ParReportDto
{
    public DateTime ReportDate { get; set; }
    public long TotalPortfolio { get; set; }
    public long OverdueAmount { get; set; }
    public decimal ParPercentage { get; set; }
    public long Par30 { get; set; }
    public long Par60 { get; set; }
    public long Par90 { get; set; }
    public long ParAbove90 { get; set; }
    public List<ParBucketDto> Buckets { get; set; } = new();
}

public class ParBucketDto
{
    public string BucketName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public int LoanCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CollectionEfficiencyReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long TotalDue { get; set; }
    public long TotalCollected { get; set; }
    public decimal EfficiencyPercentage { get; set; }
    public long OnTimeCollection { get; set; }
    public long LateCollection { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public List<CollectionTrendDto> Trends { get; set; } = new();
}

public class CollectionTrendDto
{
    public DateTime Date { get; set; }
    public long DueAmount { get; set; }
    public long CollectedAmount { get; set; }
    public decimal EfficiencyPercentage { get; set; }
}
