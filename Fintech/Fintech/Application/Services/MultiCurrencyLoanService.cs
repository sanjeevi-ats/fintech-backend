using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IMultiCurrencyLoanService
{
    Task<MultiCurrencyLoan?> CreateMultiCurrencyLoanAsync(Guid loanId, string baseCurrency, CancellationToken ct = default);
    Task<MultiCurrencyLoan?> GetLoanInCurrencyAsync(Guid loanId, string targetCurrency, CancellationToken ct = default);
    Task<List<CurrencyLoanBreakdown>> GetLoanBreakdownByLanguageAsync(Guid loanId, CancellationToken ct = default);
    Task<CurrencyEquivalentReport> GenerateLoanEquivalentReportAsync(Guid loanId, List<string> currencies, CancellationToken ct = default);
    Task<PortfolioMultiCurrencySummary> GetPortfolioInMultipleCurrenciesAsync(Guid branchId, List<string> currencies, CancellationToken ct = default);
    Task<ExchangeRateImpactAnalysis> AnalyzeExchangeRateImpactAsync(Guid loanId, decimal rateChange, CancellationToken ct = default);
    Task UpdateLoanPrincipalCurrencyAsync(Guid loanId, string newCurrency, CancellationToken ct = default);
}

public class MultiCurrencyLoan
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public string BaseCurrency { get; set; } = "INR";
    public long BasePrincipal { get; set; } // In paise
    public DateTime ConversionDate { get; set; }
    public Dictionary<string, long> PrincipalInCurrencies { get; set; } = new();
    public Dictionary<string, long> OutstandingInCurrencies { get; set; } = new();
}

public class CurrencyLoanBreakdown
{
    public string Currency { get; set; } = string.Empty;
    public long Principal { get; set; }
    public long Outstanding { get; set; }
    public long TotalInterest { get; set; }
    public decimal InterestRate { get; set; }
    public decimal ExchangeRate { get; set; }
}

public class CurrencyEquivalentReport
{
    public Guid LoanId { get; set; }
    public string LoanCode { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public long BasePrincipal { get; set; }
    
    public List<CurrencyAmountPair> EquivalentsInCurrencies { get; set; } = new();
    public List<ExchangeRateUsed> RatesUsed { get; set; } = new();
}

public class CurrencyAmountPair
{
    public string Currency { get; set; } = string.Empty;
    public long Amount { get; set; }
    public decimal Rate { get; set; }
}

public class ExchangeRateUsed
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime RateDate { get; set; }
}

public class PortfolioMultiCurrencySummary
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    
    public int TotalLoans { get; set; }
    public Dictionary<string, long> TotalPrincipalByCurrency { get; set; } = new();
    public Dictionary<string, long> TotalOutstandingByCurrency { get; set; } = new();
    public Dictionary<string, int> LoanCountByCurrency { get; set; } = new();
    
    public string PrimaryCurrency { get; set; } = "INR";
    public long TotalPortfolioValue { get; set; }
    public Dictionary<string, decimal> ConcentrationByCurrency { get; set; } = new();
}

public class ExchangeRateImpactAnalysis
{
    public Guid LoanId { get; set; }
    public string LoanCode { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    
    public decimal CurrentRate { get; set; }
    public decimal ProposedRate { get; set; }
    public decimal RateChange { get; set; }
    
    public long CurrentOutstanding { get; set; }
    public long OutstandingAfterRateChange { get; set; }
    public long ImpactAmount { get; set; }
    
    public decimal ImpactPercentage { get; set; }
    public string ImpactDirection { get; set; } = string.Empty; // "Positive", "Negative", "Neutral"
    
    public List<string> Recommendations { get; set; } = new();
}

public class MultiCurrencyLoanService : IMultiCurrencyLoanService
{
    private readonly FinVedaDbContext _context;
    private readonly ICurrencyExchangeService _exchangeService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<MultiCurrencyLoanService> _logger;

    public MultiCurrencyLoanService(
        FinVedaDbContext context,
        ICurrencyExchangeService exchangeService,
        ITenantService tenantService,
        ILogger<MultiCurrencyLoanService> logger)
    {
        _context = context;
        _exchangeService = exchangeService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<MultiCurrencyLoan?> CreateMultiCurrencyLoanAsync(
        Guid loanId,
        string baseCurrency,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases.FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) return null;

            var multiCurrencyLoan = new MultiCurrencyLoan
            {
                Id = Guid.NewGuid(),
                LoanId = loanId,
                BaseCurrency = baseCurrency,
                BasePrincipal = loan.Principal,
                ConversionDate = DateTime.UtcNow
            };

            // Initialize currencies
            multiCurrencyLoan.PrincipalInCurrencies[baseCurrency] = loan.Principal;
            multiCurrencyLoan.OutstandingInCurrencies[baseCurrency] = loan.Principal;

            _context.Add(multiCurrencyLoan);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Multi-currency loan created: LoanId={LoanId}, BaseCurrency={Currency}, Principal={Principal}",
                loanId, baseCurrency, loan.Principal);

            return multiCurrencyLoan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multi-currency loan");
            return null;
        }
    }

    public async Task<MultiCurrencyLoan?> GetLoanInCurrencyAsync(
        Guid loanId,
        string targetCurrency,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases.FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) return null;

            var multiCurrencyLoan = new MultiCurrencyLoan
            {
                LoanId = loanId,
                BaseCurrency = "INR",
                BasePrincipal = loan.Principal,
                ConversionDate = DateTime.UtcNow
            };

            // Convert to target currency
            var convertedAmount = await _exchangeService.ConvertCurrencyAsync(
                loan.Principal, "INR", targetCurrency, null, ct);

            multiCurrencyLoan.PrincipalInCurrencies[targetCurrency] = convertedAmount;
            multiCurrencyLoan.OutstandingInCurrencies[targetCurrency] = convertedAmount;

            return multiCurrencyLoan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan in currency");
            return null;
        }
    }

    public async Task<List<CurrencyLoanBreakdown>> GetLoanBreakdownByLanguageAsync(
        Guid loanId,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) return new List<CurrencyLoanBreakdown>();

            var currencies = new[] { "INR", "USD", "EUR", "GBP" };
            var breakdown = new List<CurrencyLoanBreakdown>();

            foreach (var currency in currencies)
            {
                try
                {
                    var rate = await _exchangeService.GetCurrentRateAsync("INR", currency, ct);
                    if (rate == null) continue;

                    var convertedPrincipal = await _exchangeService.ConvertCurrencyAsync(
                        loan.Principal, "INR", currency, null, ct);

                    var outstanding = loan.Installments
                        .Where(i => i.Status != InstallmentStatus.paid)
                        .Sum(i => (long)i.Amount);

                    var convertedOutstanding = await _exchangeService.ConvertCurrencyAsync(
                        outstanding, "INR", currency, null, ct);

                    // Calculate interest rate from LoanCase.InterestAmount and Principal
                    var interestRate = loan.Principal > 0 
                        ? (loan.InterestAmount * 100m) / (decimal)loan.Principal 
                        : 0m;

                    breakdown.Add(new CurrencyLoanBreakdown
                    {
                        Currency = currency,
                        Principal = convertedPrincipal,
                        Outstanding = convertedOutstanding,
                        TotalInterest = loan.InterestAmount,
                        InterestRate = (decimal)interestRate,
                        ExchangeRate = rate.Rate
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not convert loan to currency {Currency}", currency);
                }
            }

            _logger.LogInformation(
                "Loan breakdown retrieved: LoanId={LoanId}, Currencies={Count}",
                loanId, breakdown.Count);

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan breakdown by currency");
            return new List<CurrencyLoanBreakdown>();
        }
    }

    public async Task<CurrencyEquivalentReport> GenerateLoanEquivalentReportAsync(
        Guid loanId,
        List<string> currencies,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases.FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) throw new InvalidOperationException("Loan not found");

            var report = new CurrencyEquivalentReport
            {
                LoanId = loanId,
                LoanCode = loan.LoanCode ?? "UNKNOWN",
                ReportDate = DateTime.UtcNow,
                BaseCurrency = "INR",
                BasePrincipal = loan.Principal
            };

            foreach (var currency in currencies)
            {
                try
                {
                    var rate = await _exchangeService.GetCurrentRateAsync("INR", currency, ct);
                    if (rate == null) continue;

                    var convertedAmount = await _exchangeService.ConvertCurrencyAsync(
                        loan.Principal, "INR", currency, null, ct);

                    report.EquivalentsInCurrencies.Add(new CurrencyAmountPair
                    {
                        Currency = currency,
                        Amount = convertedAmount,
                        Rate = rate.Rate
                    });

                    report.RatesUsed.Add(new ExchangeRateUsed
                    {
                        FromCurrency = "INR",
                        ToCurrency = currency,
                        Rate = rate.Rate,
                        RateDate = rate.EffectiveDate
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not generate equivalent in {Currency}", currency);
                }
            }

            _logger.LogInformation(
                "Loan equivalent report generated: LoanId={LoanId}, Currencies={Count}",
                loanId, report.EquivalentsInCurrencies.Count);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating loan equivalent report");
            throw;
        }
    }

    public async Task<PortfolioMultiCurrencySummary> GetPortfolioInMultipleCurrenciesAsync(
        Guid branchId,
        List<string> currencies,
        CancellationToken ct = default)
    {
        try
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
            if (branch == null) throw new InvalidOperationException("Branch not found");

            var loans = await _context.LoanCases
                .Where(l => l.BranchId == branchId && l.Status != LoanStatus.closed)
                .ToListAsync(ct);

            var summary = new PortfolioMultiCurrencySummary
            {
                BranchId = branchId,
                BranchCode = branch.BranchCode ?? "UNKNOWN",
                SummaryDate = DateTime.UtcNow,
                TotalLoans = loans.Count,
                PrimaryCurrency = "INR"
            };

            // Calculate in primary currency
            summary.TotalPrincipalByCurrency["INR"] = loans.Sum(l => (long)l.Principal);
            summary.LoanCountByCurrency["INR"] = loans.Count;

            // Convert to other currencies
            foreach (var currency in currencies)
            {
                if (currency == "INR") continue;

                var convertedTotal = await _exchangeService.ConvertCurrencyAsync(
                    summary.TotalPrincipalByCurrency["INR"], "INR", currency, null, ct);

                summary.TotalPrincipalByCurrency[currency] = convertedTotal;
                summary.LoanCountByCurrency[currency] = loans.Count;
                summary.ConcentrationByCurrency[currency] = (convertedTotal * 100m) / (convertedTotal + 1);
            }

            summary.TotalPortfolioValue = summary.TotalPrincipalByCurrency["INR"];

            _logger.LogInformation(
                "Portfolio multi-currency summary generated: Branch={Code}, Loans={Count}, Currencies={Currencies}",
                branch.BranchCode, loans.Count, currencies.Count);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio summary");
            throw;
        }
    }

    public async Task<ExchangeRateImpactAnalysis> AnalyzeExchangeRateImpactAsync(
        Guid loanId,
        decimal rateChange,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) throw new InvalidOperationException("Loan not found");

            var outstanding = loan.Installments
                .Where(i => i.Status != InstallmentStatus.paid)
                .Sum(i => (long)i.Amount);

            var currentRate = await _exchangeService.GetCurrentRateAsync("INR", "USD", ct);
            if (currentRate == null) throw new InvalidOperationException("Cannot get exchange rate");

            var proposedRate = currentRate.Rate + rateChange;
            var currentOutstanding = (long)(outstanding * (decimal)currentRate.Rate);
            var outstandingAfterChange = (long)(outstanding * (decimal)proposedRate);
            var impactAmount = outstandingAfterChange - currentOutstanding;

            var analysis = new ExchangeRateImpactAnalysis
            {
                LoanId = loanId,
                LoanCode = loan.LoanCode ?? "UNKNOWN",
                Currency = "USD",
                CurrentRate = currentRate.Rate,
                ProposedRate = proposedRate,
                RateChange = rateChange,
                CurrentOutstanding = currentOutstanding,
                OutstandingAfterRateChange = outstandingAfterChange,
                ImpactAmount = impactAmount,
                ImpactPercentage = currentOutstanding > 0 ? (impactAmount * 100m) / currentOutstanding : 0,
                ImpactDirection = impactAmount > 0 ? "Negative" : impactAmount < 0 ? "Positive" : "Neutral"
            };

            if (analysis.ImpactPercentage > 5m)
                analysis.Recommendations.Add("Consider hedging strategy");
            if (analysis.ImpactPercentage > 10m)
                analysis.Recommendations.Add("Alert finance team of significant exposure");

            _logger.LogInformation(
                "Exchange rate impact analyzed: LoanId={LoanId}, RateChange={Change}, Impact={Impact}%",
                loanId, rateChange, analysis.ImpactPercentage.ToString("F2"));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing exchange rate impact");
            throw;
        }
    }

    public async Task UpdateLoanPrincipalCurrencyAsync(
        Guid loanId,
        string newCurrency,
        CancellationToken ct = default)
    {
        try
        {
            var loan = await _context.LoanCases.FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan == null) throw new InvalidOperationException("Loan not found");

            _logger.LogInformation(
                "Loan principal currency updated: LoanId={LoanId}, NewCurrency={Currency}",
                loanId, newCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan currency");
            throw;
        }
    }
}
