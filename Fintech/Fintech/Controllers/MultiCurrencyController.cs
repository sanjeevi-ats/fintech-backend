using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MultiCurrencyController : ControllerBase
{
    private readonly ICurrencyExchangeService _exchangeService;
    private readonly IMultiCurrencyLoanService _multiCurrencyLoanService;
    private readonly ILogger<MultiCurrencyController> _logger;

    public MultiCurrencyController(
        ICurrencyExchangeService exchangeService,
        IMultiCurrencyLoanService multiCurrencyLoanService,
        ILogger<MultiCurrencyController> logger)
    {
        _exchangeService = exchangeService;
        _multiCurrencyLoanService = multiCurrencyLoanService;
        _logger = logger;
    }

    /// <summary>
    /// Get current exchange rate between two currencies
    /// </summary>
    [HttpGet("exchange-rate/{fromCurrency}/{toCurrency}")]
    public async Task<IActionResult> GetExchangeRate(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/multicurrency/exchange-rate/{FromCurrency}/{ToCurrency}",
                fromCurrency, toCurrency);

            var rate = await _exchangeService.GetCurrentRateAsync(fromCurrency, toCurrency, ct);
            if (rate == null)
                return NotFound(new { error = $"Exchange rate not found for {fromCurrency}/{toCurrency}" });

            _logger.LogInformation(
                "GET /api/multicurrency/exchange-rate - Success (200): Rate={Rate}",
                rate.Rate);

            return Ok(new { data = rate, message = "Exchange rate retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get historical exchange rates
    /// </summary>
    [HttpGet("exchange-rate-history/{fromCurrency}/{toCurrency}")]
    public async Task<IActionResult> GetHistoricalRates(
        string fromCurrency,
        string toCurrency,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/multicurrency/exchange-rate-history - From={From}, To={To}, Period={Start} to {End}",
                fromCurrency, toCurrency, startDate.Date, endDate.Date);

            var rates = await _exchangeService.GetHistoricalRatesAsync(
                fromCurrency, toCurrency, startDate, endDate, ct);

            _logger.LogInformation(
                "GET /api/multicurrency/exchange-rate-history - Success (200): Count={Count}",
                rates.Count);

            return Ok(new { data = rates, message = "Historical rates retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical rates");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update exchange rate
    /// </summary>
    [HttpPost("exchange-rate/update")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> UpdateExchangeRate(
        [FromBody] UpdateExchangeRateRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/exchange-rate/update - From={From}, To={To}, Rate={Rate}",
                request?.FromCurrency, request?.ToCurrency, request?.Rate);

            if (request?.FromCurrency == null || request?.ToCurrency == null)
                return BadRequest(new { error = "FromCurrency and ToCurrency are required" });

            var rate = await _exchangeService.UpdateExchangeRateAsync(
                request.FromCurrency, request.ToCurrency, request.Rate, 
                request.Source ?? "Manual", ct);

            _logger.LogInformation(
                "POST /api/multicurrency/exchange-rate/update - Success (201): Rate={Rate}",
                rate.Rate);

            return Created(string.Empty, new { data = rate, message = "Exchange rate updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange rate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Convert amount from one currency to another
    /// </summary>
    [HttpPost("convert")]
    public async Task<IActionResult> ConvertCurrency(
        [FromBody] CurrencyConversionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/convert - Amount={Amount}, From={From}, To={To}",
                request?.Amount, request?.FromCurrency, request?.ToCurrency);

            if (request?.Amount <= 0)
                return BadRequest(new { error = "Amount must be positive" });

            var convertedAmount = await _exchangeService.ConvertCurrencyAsync(
                request.Amount, request.FromCurrency!, request.ToCurrency!, null, ct);

            _logger.LogInformation(
                "POST /api/multicurrency/convert - Success (200): Converted={Amount}",
                convertedAmount);

            return Ok(new
            {
                data = new
                {
                    original_amount = request.Amount,
                    original_currency = request.FromCurrency,
                    converted_amount = convertedAmount,
                    converted_currency = request.ToCurrency,
                    conversion_date = DateTime.UtcNow
                },
                message = "Currency conversion completed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get supported currencies
    /// </summary>
    [HttpGet("supported-currencies")]
    public async Task<IActionResult> GetSupportedCurrencies(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/multicurrency/supported-currencies");

            var currencies = await _exchangeService.GetSupportedCurrenciesAsync(ct);

            _logger.LogInformation(
                "GET /api/multicurrency/supported-currencies - Success (200): Count={Count}",
                currencies.Count);

            return Ok(new { data = currencies, message = "Supported currencies retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported currencies");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create multi-currency loan
    /// </summary>
    [HttpPost("loan/create")]
    public async Task<IActionResult> CreateMultiCurrencyLoan(
        [FromBody] CreateMultiCurrencyLoanRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/loan/create - LoanId={LoanId}, Currency={Currency}",
                request?.LoanId, request?.BaseCurrency);

            if (request?.LoanId == Guid.Empty)
                return BadRequest(new { error = "LoanId is required" });

            var loan = await _multiCurrencyLoanService.CreateMultiCurrencyLoanAsync(
                request.LoanId, request.BaseCurrency ?? "INR", ct);

            if (loan == null)
                return NotFound(new { error = "Loan not found" });

            _logger.LogInformation(
                "POST /api/multicurrency/loan/create - Success (201): LoanId={LoanId}",
                request.LoanId);

            return Created(string.Empty, new { data = loan, message = "Multi-currency loan created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multi-currency loan");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get loan in specific currency
    /// </summary>
    [HttpGet("loan/{loanId}/currency/{targetCurrency}")]
    public async Task<IActionResult> GetLoanInCurrency(
        Guid loanId,
        string targetCurrency,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/multicurrency/loan/{LoanId}/currency/{Currency}",
                loanId, targetCurrency);

            var loan = await _multiCurrencyLoanService.GetLoanInCurrencyAsync(loanId, targetCurrency, ct);
            if (loan == null)
                return NotFound(new { error = "Loan not found" });

            _logger.LogInformation(
                "GET /api/multicurrency/loan - Success (200): LoanId={LoanId}",
                loanId);

            return Ok(new { data = loan, message = "Loan retrieved in target currency" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan in currency");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get loan breakdown by currency
    /// </summary>
    [HttpGet("loan/{loanId}/breakdown")]
    public async Task<IActionResult> GetLoanBreakdown(Guid loanId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/multicurrency/loan/{LoanId}/breakdown", loanId);

            var breakdown = await _multiCurrencyLoanService.GetLoanBreakdownByLanguageAsync(loanId, ct);

            _logger.LogInformation(
                "GET /api/multicurrency/loan/breakdown - Success (200): Currencies={Count}",
                breakdown.Count);

            return Ok(new { data = breakdown, message = "Loan breakdown retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan breakdown");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate loan equivalent report
    /// </summary>
    [HttpPost("loan/{loanId}/equivalent-report")]
    public async Task<IActionResult> GenerateLoanEquivalentReport(
        Guid loanId,
        [FromBody] LoanEquivalentReportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/loan/{LoanId}/equivalent-report",
                loanId);

            var report = await _multiCurrencyLoanService.GenerateLoanEquivalentReportAsync(
                loanId, request?.Currencies ?? new List<string> { "USD", "EUR", "GBP" }, ct);

            _logger.LogInformation(
                "POST /api/multicurrency/loan/equivalent-report - Success (200): Currencies={Count}",
                report.EquivalentsInCurrencies.Count);

            return Ok(new { data = report, message = "Loan equivalent report generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating loan equivalent report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get portfolio summary in multiple currencies
    /// </summary>
    [HttpPost("portfolio/multi-currency-summary")]
    public async Task<IActionResult> GetPortfolioMultiCurrencySummary(
        [FromBody] PortfolioMultiCurrencyRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/portfolio/multi-currency-summary - Branch={Branch}",
                request?.BranchId);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var summary = await _multiCurrencyLoanService.GetPortfolioInMultipleCurrenciesAsync(
                request.BranchId, request?.Currencies ?? new List<string> { "USD", "EUR" }, ct);

            _logger.LogInformation(
                "POST /api/multicurrency/portfolio/multi-currency-summary - Success (200): Branch={Code}",
                request.BranchId);

            return Ok(new { data = summary, message = "Portfolio multi-currency summary generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio summary");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze exchange rate impact on loan
    /// </summary>
    [HttpPost("loan/{loanId}/rate-impact")]
    public async Task<IActionResult> AnalyzeRateImpact(
        Guid loanId,
        [FromBody] RateImpactAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/multicurrency/loan/{LoanId}/rate-impact - RateChange={Change}",
                loanId, request?.RateChange);

            var analysis = await _multiCurrencyLoanService.AnalyzeExchangeRateImpactAsync(
                loanId, request?.RateChange ?? 0.01m, ct);

            _logger.LogInformation(
                "POST /api/multicurrency/loan/rate-impact - Success (200): LoanId={LoanId}",
                loanId);

            return Ok(new { data = analysis, message = "Exchange rate impact analysis completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing rate impact");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class UpdateExchangeRateRequest
{
    public string? FromCurrency { get; set; }
    public string? ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public string? Source { get; set; }
}

public class CurrencyConversionRequest
{
    public long Amount { get; set; }
    public string? FromCurrency { get; set; }
    public string? ToCurrency { get; set; }
}

public class CreateMultiCurrencyLoanRequest
{
    public Guid LoanId { get; set; }
    public string? BaseCurrency { get; set; }
}

public class LoanEquivalentReportRequest
{
    public List<string> Currencies { get; set; } = new();
}

public class PortfolioMultiCurrencyRequest
{
    public Guid BranchId { get; set; }
    public List<string> Currencies { get; set; } = new();
}

public class RateImpactAnalysisRequest
{
    public decimal RateChange { get; set; }
}
