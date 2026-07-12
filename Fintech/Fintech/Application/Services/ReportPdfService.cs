using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class ReportPdfService : IReportPdfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly IAccountingService _accountingService;

    public ReportPdfService(IUnitOfWork unitOfWork, FinVedaDbContext dbContext, IAccountingService accountingService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _accountingService = accountingService;
    }

    public async Task<byte[]> GenerateCustomerLoanReportPdfAsync(Guid customerId)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Customer not found");
        }

        var loans = await _dbContext.LoanCases
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == customerId)
            .ToListAsync();

        var reportDto = new CustomerLoanReportDto
        {
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerAddress = "Address not available",
            Loans = new List<LoanReportDetailDto>()
        };

        foreach (var loan in loans)
        {
            var receipts = await _dbContext.Receipts
                .Where(r => r.LoanCaseId == loan.Id)
                .ToListAsync();

            var collectedAmount = receipts.Sum(r => r.AmountPaid);
            var outstandingAmount = loan.TotalReceivable - collectedAmount;

            reportDto.Loans.Add(new LoanReportDetailDto
            {
                LoanId = loan.Id.ToString(),
                LoanAmount = loan.Principal,
                DisbursedDate = DateTime.UtcNow,
                TotalInstallments = loan.Installments.Count,
                PaidInstallments = loan.Installments.Count(i => i.Status == InstallmentStatus.paid),
                CollectedAmount = collectedAmount,
                OutstandingAmount = outstandingAmount,
                Status = loan.Status.ToString()
            });

            reportDto.TotalLoans += 1;
            reportDto.TotalDisbursed += loan.Principal;
            reportDto.TotalCollected += collectedAmount;
            reportDto.TotalOutstanding += outstandingAmount;
        }

        var html = GenerateCustomerLoanReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GenerateTurnoverReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var loans = await _dbContext.LoanCases.ToListAsync();
        var receipts = await _dbContext.Receipts.ToListAsync();

        var reportDto = new TurnoverReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalLoans = loans.Count,
            ActiveLoans = loans.Count(l => l.Status == LoanStatus.active),
            ClosedLoans = loans.Count(l => l.Status == LoanStatus.closed),
            TotalDisbursed = loans.Sum(l => l.Principal),
            TotalCollected = receipts.Sum(r => r.AmountPaid),
            TotalCustomers = await _dbContext.Customers.CountAsync(),
            CollectionEfficiency = loans.Sum(l => l.Principal) > 0 
                ? (decimal)receipts.Sum(r => r.AmountPaid) / loans.Sum(l => l.Principal) * 100 
                : 0
        };

        reportDto.AverageLoanSize = reportDto.TotalLoans > 0 ? reportDto.TotalDisbursed / reportDto.TotalLoans : 0;

        var html = GenerateTurnoverReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GeneratePnLReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var receipts = await _dbContext.Receipts
            .Where(r => r.CapturedAt >= startDate && r.CapturedAt <= endDate)
            .ToListAsync();

        var loans = await _dbContext.LoanCases.ToListAsync();

        var interestIncome = receipts.Sum(r => r.AmountPaid) - loans.Sum(l => l.Principal);
        var processingFees = loans.Sum(l => l.ProcessingFees);
        var totalRevenue = interestIncome + processingFees;

        var reportDto = new PnLReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            InterestIncome = Math.Max(0, interestIncome),
            ProcessingFees = processingFees,
            TotalRevenue = totalRevenue,
            OperatingExpenses = 100000, // Placeholder
            ProvisionForLosses = 50000,  // Placeholder
            TotalExpenses = 150000,      // Placeholder
            NetProfit = totalRevenue - 150000,
            LineItems = new List<PnLLineItemDto>
            {
                new() { Category = "Revenue", Description = "Interest Income", Amount = Math.Max(0, interestIncome), Type = "Income" },
                new() { Category = "Revenue", Description = "Processing Fees", Amount = processingFees, Type = "Income" },
                new() { Category = "Expenses", Description = "Operating Expenses", Amount = 100000, Type = "Expense" },
                new() { Category = "Expenses", Description = "Provision for Losses", Amount = 50000, Type = "Expense" }
            }
        };

        reportDto.ProfitMargin = reportDto.TotalRevenue > 0 
            ? (decimal)reportDto.NetProfit / reportDto.TotalRevenue * 100 
            : 0;

        var html = GeneratePnLReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GeneratePartnerReportPdfAsync(Guid partnerId)
    {
        var partner = await _dbContext.Partners
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == partnerId);

        if (partner == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Partner not found");
        }

        // Get capital account for partner
        var capitalAccount = await _dbContext.CapitalAccounts
            .FirstOrDefaultAsync(ca => ca.PartnerId == partnerId);

        if (capitalAccount == null)
        {
            // No capital account - return empty report
            var emptyReportDto = new PartnerReportDto
            {
                PartnerName = partner.Name,
                PartnerEmail = partner.Email,
                PartnerPhone = partner.Phone,
                TotalInvestment = 0,
                EquityPercentage = (decimal)partner.EquityPct,
                TotalProfitEarned = 0,
                TotalWithdrawals = 0,
                CurrentBalance = 0,
                Transactions = new()
            };
            var emptyHtml = GeneratePartnerReportHtml(emptyReportDto);
            return Encoding.UTF8.GetBytes(emptyHtml);
        }

        // Get transactions for the account (Phase 4+)
        var transactions = await _dbContext.CapitalTransactions
            .Where(t => t.CapitalAccountId == capitalAccount.Id && t.Status == "Approved")
            .ToListAsync();

        var totalInvestment = transactions
            .Where(t => t.TransactionType == "Contribution")
            .Sum(t => t.Amount);

        var totalWithdrawals = transactions
            .Where(t => t.TransactionType == "Withdrawal")
            .Sum(t => t.Amount);

        var reportDto = new PartnerReportDto
        {
            PartnerName = partner.Name,
            PartnerEmail = partner.Email,
            PartnerPhone = partner.Phone,
            TotalInvestment = capitalAccount.OpeningBalance,
            EquityPercentage = capitalAccount.OwnershipPercentage,
            TotalProfitEarned = 0, // Placeholder - Phase 5+
            TotalWithdrawals = totalWithdrawals,
            CurrentBalance = capitalAccount.CurrentBalance,
            Transactions = transactions.Select(t => new PartnerTransactionDto
            {
                Date = t.TransactionDate,
                Type = t.TransactionType,
                Amount = t.Amount,
                Description = t.Description
            }).ToList()
        };

        var html = GeneratePartnerReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GenerateParReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var loans = await _dbContext.LoanCases.ToListAsync();
        var installments = await _dbContext.Installments.ToListAsync();
        var receipts = await _dbContext.Receipts.ToListAsync();

        var now = DateTime.UtcNow;
        var overdueInstallments = installments
            .Where(i => i.Status != InstallmentStatus.paid && new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day) < now.Date)
            .ToList();

        var totalPortfolio = loans.Sum(l => l.TotalReceivable);
        var overdueAmount = overdueInstallments.Sum(i => i.Amount);

        var reportDto = new ParReportDto
        {
            ReportDate = DateTime.UtcNow,
            TotalPortfolio = totalPortfolio,
            OverdueAmount = overdueAmount,
            ParPercentage = totalPortfolio > 0 ? (decimal)overdueAmount / totalPortfolio * 100 : 0,
            Par30 = overdueInstallments.Where(i => (now.Date - new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day)).Days <= 30).Sum(i => i.Amount),
            Par60 = overdueInstallments.Where(i => (now.Date - new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day)).Days <= 60).Sum(i => i.Amount),
            Par90 = overdueInstallments.Where(i => (now.Date - new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day)).Days <= 90).Sum(i => i.Amount),
            ParAbove90 = overdueInstallments.Where(i => (now.Date - new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day)).Days > 90).Sum(i => i.Amount)
        };

        var html = GenerateParReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GenerateCollectionEfficiencyReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var receipts = await _dbContext.Receipts
            .Where(r => r.CapturedAt >= startDate && r.CapturedAt <= endDate)
            .ToListAsync();

        var installments = await _dbContext.Installments
            .Where(i => i.DueDate >= startDate && i.DueDate <= endDate)
            .ToListAsync();

        var totalDue = installments.Sum(i => i.Amount);
        var totalCollected = receipts.Sum(r => r.AmountPaid);

        var reportDto = new CollectionEfficiencyReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalDue = totalDue,
            TotalCollected = totalCollected,
            EfficiencyPercentage = totalDue > 0 ? (decimal)totalCollected / totalDue * 100 : 0,
            OnTimeCollection = totalCollected,
            LateCollection = 0,
            OnTimeCount = receipts.Count,
            LateCount = 0
        };

        var html = GenerateCollectionEfficiencyReportHtml(reportDto);
        return Encoding.UTF8.GetBytes(html);
    }

    // HTML Generation Methods
    private string GenerateCustomerLoanReportHtml(CustomerLoanReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-size: 14px; font-weight: bold; background-color: #f0f0f0; padding: 10px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #f0f0f0; padding: 10px; text-align: left; border-bottom: 2px solid #333; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
        .total {{ font-weight: bold; background-color: #f9f9f9; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>CUSTOMER LOAN REPORT</div>
        <div>Generated on: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}</div>
    </div>

    <div class='section'>
        <div class='section-title'>CUSTOMER INFORMATION</div>
        <p><strong>Name:</strong> {report.CustomerName}</p>
        <p><strong>Phone:</strong> {report.CustomerPhone}</p>
        <p><strong>Address:</strong> {report.CustomerAddress}</p>
    </div>

    <div class='section'>
        <div class='section-title'>LOAN DETAILS</div>
        <table>
            <thead>
                <tr>
                    <th>Loan ID</th>
                    <th>Amount</th>
                    <th>Disbursed</th>
                    <th>Installments</th>
                    <th>Collected</th>
                    <th>Outstanding</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", report.Loans.Select(l => $@"
                <tr>
                    <td>{l.LoanId.Substring(0, 12)}...</td>
                    <td>₹{(l.LoanAmount / 100.0):N2}</td>
                    <td>{l.DisbursedDate:dd/MM/yyyy}</td>
                    <td>{l.PaidInstallments}/{l.TotalInstallments}</td>
                    <td>₹{(l.CollectedAmount / 100.0):N2}</td>
                    <td>₹{(l.OutstandingAmount / 100.0):N2}</td>
                    <td>{l.Status}</td>
                </tr>"))}
                <tr class='total'>
                    <td>TOTAL</td>
                    <td>₹{(report.TotalDisbursed / 100.0):N2}</td>
                    <td></td>
                    <td></td>
                    <td>₹{(report.TotalCollected / 100.0):N2}</td>
                    <td>₹{(report.TotalOutstanding / 100.0):N2}</td>
                    <td></td>
                </tr>
            </tbody>
        </table>
    </div>
</body>
</html>";
    }

    private string GenerateTurnoverReportHtml(TurnoverReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        .metric {{ display: inline-block; width: 45%; margin: 10px 2.5%; padding: 15px; background-color: #f9f9f9; border: 1px solid #eee; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .metric-value {{ font-size: 18px; font-weight: bold; color: #333; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>COMPANY TURNOVER REPORT</div>
        <div>Period: {report.StartDate:dd/MM/yyyy} to {report.EndDate:dd/MM/yyyy}</div>
    </div>

    <div>
        <div class='metric'>
            <div class='metric-label'>Total Loans</div>
            <div class='metric-value'>{report.TotalLoans}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Active Loans</div>
            <div class='metric-value'>{report.ActiveLoans}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Closed Loans</div>
            <div class='metric-value'>{report.ClosedLoans}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Total Customers</div>
            <div class='metric-value'>{report.TotalCustomers}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Total Disbursed</div>
            <div class='metric-value'>₹{(report.TotalDisbursed / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Total Collected</div>
            <div class='metric-value'>₹{(report.TotalCollected / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Average Loan Size</div>
            <div class='metric-value'>₹{(report.AverageLoanSize / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Collection Efficiency</div>
            <div class='metric-value'>{report.CollectionEfficiency:F2}%</div>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePnLReportHtml(PnLReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #f0f0f0; padding: 10px; text-align: left; border-bottom: 2px solid #333; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
        .total {{ font-weight: bold; background-color: #f9f9f9; }}
        .profit {{ color: #2ecc71; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>PROFIT & LOSS STATEMENT</div>
        <div>Period: {report.StartDate:dd/MM/yyyy} to {report.EndDate:dd/MM/yyyy}</div>
    </div>

    <table>
        <thead>
            <tr>
                <th>Category</th>
                <th>Description</th>
                <th>Amount</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", report.LineItems.Select(item => $@"
            <tr>
                <td>{item.Category}</td>
                <td>{item.Description}</td>
                <td>₹{(item.Amount / 100.0):N2}</td>
            </tr>"))}
            <tr class='total'>
                <td colspan='2'>NET PROFIT</td>
                <td class='profit'>₹{(report.NetProfit / 100.0):N2}</td>
            </tr>
            <tr>
                <td colspan='2'>Profit Margin</td>
                <td>{report.ProfitMargin:F2}%</td>
            </tr>
        </tbody>
    </table>
</body>
</html>";
    }

    private string GeneratePartnerReportHtml(PartnerReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-size: 14px; font-weight: bold; background-color: #f0f0f0; padding: 10px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #f0f0f0; padding: 10px; text-align: left; border-bottom: 2px solid #333; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>PARTNER INVESTMENT REPORT</div>
        <div>Generated on: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}</div>
    </div>

    <div class='section'>
        <div class='section-title'>PARTNER INFORMATION</div>
        <p><strong>Name:</strong> {report.PartnerName}</p>
        <p><strong>Email:</strong> {report.PartnerEmail}</p>
        <p><strong>Phone:</strong> {report.PartnerPhone}</p>
        <p><strong>Equity %:</strong> {report.EquityPercentage:F2}%</p>
    </div>

    <div class='section'>
        <div class='section-title'>INVESTMENT SUMMARY</div>
        <p><strong>Total Investment:</strong> ₹{(report.TotalInvestment / 100.0):N2}</p>
        <p><strong>Total Profit Earned:</strong> ₹{(report.TotalProfitEarned / 100.0):N2}</p>
        <p><strong>Total Withdrawals:</strong> ₹{(report.TotalWithdrawals / 100.0):N2}</p>
        <p><strong>Current Balance:</strong> ₹{(report.CurrentBalance / 100.0):N2}</p>
    </div>

    <div class='section'>
        <div class='section-title'>TRANSACTION HISTORY</div>
        <table>
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Type</th>
                    <th>Amount</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", report.Transactions.Select(t => $@"
                <tr>
                    <td>{t.Date:dd/MM/yyyy}</td>
                    <td>{t.Type}</td>
                    <td>₹{(t.Amount / 100.0):N2}</td>
                    <td>{t.Description}</td>
                </tr>"))}
            </tbody>
        </table>
    </div>
</body>
</html>";
    }

    private string GenerateParReportHtml(ParReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        .metric {{ display: inline-block; width: 45%; margin: 10px 2.5%; padding: 15px; background-color: #f9f9f9; border: 1px solid #eee; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .metric-value {{ font-size: 18px; font-weight: bold; color: #333; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>PORTFOLIO AT RISK (PAR) REPORT</div>
        <div>As of: {report.ReportDate:dd/MM/yyyy}</div>
    </div>

    <div>
        <div class='metric'>
            <div class='metric-label'>Total Portfolio</div>
            <div class='metric-value'>₹{(report.TotalPortfolio / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Overdue Amount</div>
            <div class='metric-value'>₹{(report.OverdueAmount / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>PAR %</div>
            <div class='metric-value'>{report.ParPercentage:F2}%</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>PAR 30</div>
            <div class='metric-value'>₹{(report.Par30 / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>PAR 60</div>
            <div class='metric-value'>₹{(report.Par60 / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>PAR 90</div>
            <div class='metric-value'>₹{(report.Par90 / 100.0):N2}</div>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateCollectionEfficiencyReportHtml(CollectionEfficiencyReportDto report)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; }}
        .metric {{ display: inline-block; width: 45%; margin: 10px 2.5%; padding: 15px; background-color: #f9f9f9; border: 1px solid #eee; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .metric-value {{ font-size: 18px; font-weight: bold; color: #333; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>COLLECTION EFFICIENCY REPORT</div>
        <div>Period: {report.StartDate:dd/MM/yyyy} to {report.EndDate:dd/MM/yyyy}</div>
    </div>

    <div>
        <div class='metric'>
            <div class='metric-label'>Total Due</div>
            <div class='metric-value'>₹{(report.TotalDue / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Total Collected</div>
            <div class='metric-value'>₹{(report.TotalCollected / 100.0):N2}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Efficiency %</div>
            <div class='metric-value'>{report.EfficiencyPercentage:F2}%</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>On-Time Collections</div>
            <div class='metric-value'>{report.OnTimeCount}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Late Collections</div>
            <div class='metric-value'>{report.LateCount}</div>
        </div>
    </div>
</body>
</html>";
    }
}
