using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IComplianceReportingService
{
    Task<RBIReportMonthly?> GenerateRBIMonthlyReportAsync(Guid branchId, int month, int year, CancellationToken ct = default);
    Task<GSTComplianceReport?> GenerateGSTReportAsync(Guid branchId, string quarter, int year, CancellationToken ct = default);
    Task<LoanPortfolioCompliance?> AnalyzeLoanPortfolioComplianceAsync(Guid branchId, CancellationToken ct = default);
    Task<NPAReport?> GenerateNPAReportAsync(Guid branchId, CancellationToken ct = default);
    Task<ProvisioningReport?> GenerateProvisioningReportAsync(Guid branchId, int month, int year, CancellationToken ct = default);
}

public class RBIReportMonthly
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime ReportDate { get; set; }
    
    // Portfolio metrics
    public int TotalLoansOutstanding { get; set; }
    public long TotalOutstandingAmount { get; set; } // In Paise
    public long TotalDisbursedAmount { get; set; }
    
    // NPA metrics
    public int NPALoans { get; set; }
    public long NPAAmount { get; set; }
    public decimal NPARatio { get; set; } // Percentage
    
    // Collections
    public long TotalCollected { get; set; }
    public int LoansRepaid { get; set; }
    
    // Risk metrics
    public decimal DiversificationIndex { get; set; }
    public decimal ConcentrationRatio { get; set; }
    public List<string> ComplianceIssues { get; set; } = new();
}

public class GSTComplianceReport
{
    public Guid BranchId { get; set; }
    public string Quarter { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateTime ReportDate { get; set; }
    
    // GST liability
    public long TotalGSTLiability { get; set; }
    public long InputTaxCredit { get; set; }
    public long NetGSTPayable { get; set; }
    
    // Transaction breakdown
    public int InvoicesIssued { get; set; }
    public long TaxableValue { get; set; }
    public long GSTCollected { get; set; }
    
    // Compliance status
    public bool GSTReturnFiled { get; set; }
    public DateTime? GSTReturnFilingDate { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty; // Compliant, Overdue, Pending
}

public class LoanPortfolioCompliance
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    
    // Portfolio composition
    public int TotalLoans { get; set; }
    public long TotalOutstanding { get; set; }
    public decimal AverageLoanSize { get; set; }
    public decimal MedianLoanSize { get; set; }
    
    // Regulatory requirements
    public bool MeetsMinimumCapitalRatio { get; set; }
    public decimal CurrentCapitalRatio { get; set; }
    public decimal RequiredCapitalRatio { get; set; }
    
    // Concentration limits
    public bool MeetsConcentrationLimits { get; set; }
    public decimal LargestBorrowerExposure { get; set; }
    public decimal GroupExposureLimit { get; set; }
    
    // Documentation compliance
    public int LoansWithProperDocumentation { get; set; }
    public decimal DocumentationCompliance { get; set; } // Percentage
    
    // Issues found
    public List<string> ComplianceViolations { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class NPAReport
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    
    // NPA classification
    public int SubStandardLoans { get; set; }
    public int DoubtfulLoans { get; set; }
    public int LossLoans { get; set; }
    public int PerformingLoans { get; set; }
    
    // NPA amounts
    public long SubStandardAmount { get; set; }
    public long DoubtfulAmount { get; set; }
    public long LossAmount { get; set; }
    public long TotalNPA { get; set; }
    
    // NPA ratio
    public decimal NPARatio { get; set; }
    public decimal GrossNPARatio { get; set; }
    
    // Trend analysis
    public decimal NPAChange { get; set; } // % change from previous period
    public string NPATrend { get; set; } = string.Empty; // Improving, Stable, Deteriorating
    
    // Provisioning
    public long ProvisioningRequired { get; set; }
    public long ProvisioningDone { get; set; }
    public decimal ProvisioningCoverage { get; set; }
}

public class ProvisioningReport
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime ReportDate { get; set; }
    
    // Provision categories
    public long StandardAssetProvision { get; set; }
    public long SubStandardProvision { get; set; }
    public long DoubtfulProvision { get; set; }
    public long LossAssetProvision { get; set; }
    public long TotalProvisionRequired { get; set; }
    
    // Actual provisions
    public long StandardAssetProvided { get; set; }
    public long SubStandardProvided { get; set; }
    public long DoubtfulProvided { get; set; }
    public long LossAssetProvided { get; set; }
    public long TotalProvisionMade { get; set; }
    
    // Provisions ratios
    public decimal ProvisioningCoverageRatio { get; set; }
    public decimal OverProvision { get; set; }
    public decimal UnderProvision { get; set; }
    
    // Compliance check
    public bool IsCompliant { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class ComplianceReportingService : IComplianceReportingService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ComplianceReportingService> _logger;

    public ComplianceReportingService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<ComplianceReportingService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<RBIReportMonthly?> GenerateRBIMonthlyReportAsync(
        Guid branchId,
        int month,
        int year,
        CancellationToken ct = default)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
            if (branch == null) return null;

            var loans = await _context.LoanCases
                .Include(l => l.Installments)
                .Where(l => l.BranchId == branchId)
                .ToListAsync(ct);

            var report = new RBIReportMonthly
            {
                BranchId = branchId,
                BranchCode = branch.BranchCode ?? "UNKNOWN",
                Month = month,
                Year = year,
                ReportDate = DateTime.UtcNow
            };

            // Calculate portfolio metrics
            report.TotalLoansOutstanding = loans.Count(l => l.Status != LoanStatus.closed);
            report.TotalOutstandingAmount = loans
                .Where(l => l.Status != LoanStatus.closed)
                .Sum(l => (long)l.Principal);
            report.TotalDisbursedAmount = loans.Sum(l => (long)l.Principal);

            // Calculate NPA metrics
            var npaLoans = loans.Where(l => 
                l.Installments.Any(i => i.Status == InstallmentStatus.overdue)).ToList();
            report.NPALoans = npaLoans.Count;
            report.NPAAmount = npaLoans.Sum(l => (long)l.Principal);
            report.NPARatio = report.TotalOutstandingAmount > 0
                ? (report.NPAAmount * 100m) / report.TotalOutstandingAmount
                : 0;

            // Collections this month
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            report.TotalCollected = 0; // Would aggregate receipts for the month
            report.LoansRepaid = loans.Count(l => l.Status == LoanStatus.paid);

            // Calculate diversification
            report.DiversificationIndex = CalculateDiversificationIndex(loans);
            report.ConcentrationRatio = CalculateConcentrationRatio(loans);

            // Check compliance
            CheckRBICompliance(report);

            _logger.LogInformation(
                "RBI monthly report generated: Branch={Code}, Month={Month}/{Year}, NPA={NPA}%",
                report.BranchCode, month, year, report.NPARatio.ToString("F2"));

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RBI report for branch {BranchId}", branchId);
            return null;
        }
    }

    public async Task<GSTComplianceReport?> GenerateGSTReportAsync(
        Guid branchId,
        string quarter,
        int year,
        CancellationToken ct = default)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
            if (branch == null) return null;

            var report = new GSTComplianceReport
            {
                BranchId = branchId,
                Quarter = quarter,
                Year = year,
                ReportDate = DateTime.UtcNow
            };

            // Get month range for quarter
            var (startMonth, endMonth) = GetQuarterMonths(quarter);

            report.InvoicesIssued = 0; // Simplified
            report.TaxableValue = 0; // Simplified - would aggregate from journals
            report.GSTCollected = (long)(report.TaxableValue * 0.09m); // Assuming 9% GST

            report.TotalGSTLiability = report.GSTCollected;
            report.InputTaxCredit = (long)(report.TaxableValue * 0.05m); // Simplified
            report.NetGSTPayable = Math.Max(report.TotalGSTLiability - report.InputTaxCredit, 0);

            // Default compliance status
            report.ComplianceStatus = "Pending";
            report.GSTReturnFiled = false;

            _logger.LogInformation(
                "GST report generated: Quarter={Quarter}, Year={Year}, Liability={Liability}",
                quarter, year, report.TotalGSTLiability);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GST report for branch {BranchId}", branchId);
            return null;
        }
    }

    public async Task<LoanPortfolioCompliance?> AnalyzeLoanPortfolioComplianceAsync(
        Guid branchId,
        CancellationToken ct = default)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
            if (branch == null) return null;

            var loans = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .ToListAsync(ct);

            var compliance = new LoanPortfolioCompliance
            {
                BranchId = branchId,
                BranchCode = branch.BranchCode ?? "UNKNOWN",
                AnalysisDate = DateTime.UtcNow,
                TotalLoans = loans.Count,
                TotalOutstanding = loans
                    .Where(l => l.Status != LoanStatus.closed)
                    .Sum(l => (long)l.Principal)
            };

            if (loans.Count > 0)
            {
                var amounts = loans.Select(l => (decimal)l.Principal).OrderBy(a => a).ToList();
                compliance.AverageLoanSize = amounts.Average();
                compliance.MedianLoanSize = amounts.Count % 2 == 0
                    ? (amounts[amounts.Count / 2 - 1] + amounts[amounts.Count / 2]) / 2
                    : amounts[amounts.Count / 2];
            }

            // Capital ratio check (minimum 15% for MFIs)
            compliance.RequiredCapitalRatio = 15m;
            compliance.CurrentCapitalRatio = 18m; // Simplified
            compliance.MeetsMinimumCapitalRatio = compliance.CurrentCapitalRatio >= compliance.RequiredCapitalRatio;

            // Concentration limits
            var largestLoan = loans.Max(l => (decimal?)l.Principal) ?? 0;
            compliance.LargestBorrowerExposure = (largestLoan / (compliance.TotalOutstanding + 1)) * 100;
            compliance.GroupExposureLimit = 10m; // 10% limit for single borrower
            compliance.MeetsConcentrationLimits = compliance.LargestBorrowerExposure <= compliance.GroupExposureLimit;

            // Documentation compliance
            compliance.LoansWithProperDocumentation = loans.Count; // Assume all have docs
            compliance.DocumentationCompliance = (compliance.LoansWithProperDocumentation * 100m) / (loans.Count + 1);

            // Check violations
            CheckLoanPortfolioCompliance(compliance, loans);

            _logger.LogInformation(
                "Loan portfolio compliance analyzed: Branch={Code}, Concentration={Conc}%, CapitalRatio={Cap}%",
                compliance.BranchCode, compliance.LargestBorrowerExposure.ToString("F2"), 
                compliance.CurrentCapitalRatio.ToString("F2"));

            return compliance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing loan portfolio compliance");
            return null;
        }
    }

    public async Task<NPAReport?> GenerateNPAReportAsync(
        Guid branchId,
        CancellationToken ct = default)
    {
        try
        {
            var loans = await _context.LoanCases
                .Include(l => l.Installments)
                .Where(l => l.BranchId == branchId)
                .ToListAsync(ct);

            var report = new NPAReport
            {
                BranchId = branchId,
                ReportDate = DateTime.UtcNow
            };

            // Classify loans based on overdue days
            foreach (var loan in loans)
            {
                if (loan.Status == LoanStatus.closed || loan.Status == LoanStatus.paid)
                {
                    report.PerformingLoans++;
                    continue;
                }

                var overdueInstallments = loan.Installments
                    .Where(i => i.Status == InstallmentStatus.overdue)
                    .ToList();

                if (overdueInstallments.Count == 0)
                {
                    report.PerformingLoans++;
                }
                else
                {
                    var maxOverdueDays = overdueInstallments
                        .Max(i => (DateTime.UtcNow - i.DueDate).Days);

                    if (maxOverdueDays < 90)
                    {
                        report.SubStandardLoans++;
                        report.SubStandardAmount += loan.Principal;
                    }
                    else if (maxOverdueDays < 180)
                    {
                        report.DoubtfulLoans++;
                        report.DoubtfulAmount += loan.Principal;
                    }
                    else
                    {
                        report.LossLoans++;
                        report.LossAmount += loan.Principal;
                    }
                }
            }

            report.TotalNPA = report.SubStandardAmount + report.DoubtfulAmount + report.LossAmount;
            var totalAssets = loans.Sum(l => (long)l.Principal);
            report.NPARatio = totalAssets > 0 ? (report.TotalNPA * 100m) / totalAssets : 0;
            report.GrossNPARatio = report.NPARatio;

            // Provisioning
            report.ProvisioningRequired = (long)(report.SubStandardAmount * 0.10m +
                                                 report.DoubtfulAmount * 0.50m +
                                                 report.LossAmount * 1.00m);
            report.ProvisioningDone = report.ProvisioningRequired; // Assume fully provided
            report.ProvisioningCoverage = report.ProvisioningRequired > 0
                ? (report.ProvisioningDone * 100m) / report.ProvisioningRequired
                : 100m;

            // Trend
            report.NPATrend = "Stable";
            if (report.NPARatio < 5m)
                report.NPATrend = "Improving";
            else if (report.NPARatio > 10m)
                report.NPATrend = "Deteriorating";

            _logger.LogInformation(
                "NPA report generated: NPA Ratio={Ratio}%, Classification=SubStd:{SubStd}/Doubtful:{Doubtful}/Loss:{Loss}",
                report.NPARatio.ToString("F2"), report.SubStandardLoans, report.DoubtfulLoans, report.LossLoans);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NPA report");
            return null;
        }
    }

    public async Task<ProvisioningReport?> GenerateProvisioningReportAsync(
        Guid branchId,
        int month,
        int year,
        CancellationToken ct = default)
    {
        try
        {
            var npaReport = await GenerateNPAReportAsync(branchId, ct);
            if (npaReport == null) return null;

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
            
            var report = new ProvisioningReport
            {
                BranchId = branchId,
                BranchCode = branch?.BranchCode ?? "UNKNOWN",
                Month = month,
                Year = year,
                ReportDate = DateTime.UtcNow
            };

            // Calculate provisions based on NPA classification
            report.StandardAssetProvision = 0; // 0% for standard assets
            report.SubStandardProvision = (long)(npaReport.SubStandardAmount * 0.10m);
            report.DoubtfulProvision = (long)(npaReport.DoubtfulAmount * 0.50m);
            report.LossAssetProvision = (long)(npaReport.LossAmount * 1.00m);
            report.TotalProvisionRequired = report.SubStandardProvision + 
                                           report.DoubtfulProvision + 
                                           report.LossAssetProvision;

            // Assume all provisions are made
            report.StandardAssetProvided = report.StandardAssetProvision;
            report.SubStandardProvided = report.SubStandardProvision;
            report.DoubtfulProvided = report.DoubtfulProvision;
            report.LossAssetProvided = report.LossAssetProvision;
            report.TotalProvisionMade = report.TotalProvisionRequired;

            // Calculate ratios
            report.ProvisioningCoverageRatio = report.TotalProvisionRequired > 0
                ? (report.TotalProvisionMade * 100m) / report.TotalProvisionRequired
                : 100m;
            report.OverProvision = Math.Max(report.TotalProvisionMade - report.TotalProvisionRequired, 0);
            report.UnderProvision = Math.Max(report.TotalProvisionRequired - report.TotalProvisionMade, 0);

            report.IsCompliant = report.UnderProvision == 0;
            if (report.UnderProvision > 0)
                report.Issues.Add($"Under-provision of {report.UnderProvision}");

            _logger.LogInformation(
                "Provisioning report generated: Month={Month}/{Year}, Total Required={Total}, Coverage={Coverage}%",
                month, year, report.TotalProvisionRequired, report.ProvisioningCoverageRatio.ToString("F2"));

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provisioning report");
            return null;
        }
    }

    private decimal CalculateDiversificationIndex(List<LoanCase> loans)
    {
        // Herfindahl-Hirschman Index (HHI) calculation
        var totalAmount = loans.Sum(l => (decimal)l.Principal);
        if (totalAmount == 0) return 100;

        var sumSquares = loans.Sum(l => (decimal)Math.Pow((double)((decimal)l.Principal / totalAmount), 2));
        return sumSquares * 100;
    }

    private decimal CalculateConcentrationRatio(List<LoanCase> loans)
    {
        var totalAmount = loans.Sum(l => (decimal)l.Principal);
        if (totalAmount == 0) return 0;

        var largestLoan = loans.Max(l => (decimal)l.Principal);
        return (largestLoan / totalAmount) * 100;
    }

    private void CheckRBICompliance(RBIReportMonthly report)
    {
        report.ComplianceIssues.Clear();

        if (report.NPARatio > 8)
            report.ComplianceIssues.Add($"NPA ratio {report.NPARatio}% exceeds threshold");

        if (report.ConcentrationRatio > 30)
            report.ComplianceIssues.Add("Single borrower concentration exceeds limits");

        if (report.DiversificationIndex < 30)
            report.ComplianceIssues.Add("Low portfolio diversification");
    }

    private void CheckLoanPortfolioCompliance(LoanPortfolioCompliance compliance, List<LoanCase> loans)
    {
        compliance.ComplianceViolations.Clear();
        compliance.Recommendations.Clear();

        if (!compliance.MeetsMinimumCapitalRatio)
        {
            compliance.ComplianceViolations.Add(
                $"Capital ratio {compliance.CurrentCapitalRatio}% below required {compliance.RequiredCapitalRatio}%");
            compliance.Recommendations.Add("Increase capital base");
        }

        if (!compliance.MeetsConcentrationLimits)
        {
            compliance.ComplianceViolations.Add(
                $"Largest borrower exposure {compliance.LargestBorrowerExposure}% exceeds {compliance.GroupExposureLimit}%");
            compliance.Recommendations.Add("Reduce concentration with largest borrower");
        }

        if (compliance.DocumentationCompliance < 90)
        {
            compliance.ComplianceViolations.Add("Incomplete documentation for some loans");
            compliance.Recommendations.Add("Update missing documentation");
        }
    }

    private (int startMonth, int endMonth) GetQuarterMonths(string quarter)
    {
        return quarter.ToUpper() switch
        {
            "Q1" => (1, 3),
            "Q2" => (4, 6),
            "Q3" => (7, 9),
            "Q4" => (10, 12),
            _ => (1, 3)
        };
    }
}

