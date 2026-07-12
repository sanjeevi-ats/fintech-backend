using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface ICurrencyExchangeService
{
    Task<ExchangeRateSnapshot?> GetCurrentRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default);
    Task<List<ExchangeRateSnapshot>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<ExchangeRateSnapshot> UpdateExchangeRateAsync(string fromCurrency, string toCurrency, decimal rate, string source, CancellationToken ct = default);
    Task<long> ConvertCurrencyAsync(long amount, string fromCurrency, string toCurrency, DateTime? asOfDate = null, CancellationToken ct = default);
    Task<List<string>> GetSupportedCurrenciesAsync(CancellationToken ct = default);
    Task<RateCalculationHistory> LogRateConversionAsync(long amount, string fromCurrency, string toCurrency, decimal rate, long convertedAmount, CancellationToken ct = default);
}

public class ExchangeRateSnapshot
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Source { get; set; } = string.Empty; // "RBI", "Manual", "API"
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class RateCalculationHistory
{
    public Guid Id { get; set; }
    public DateTime ConversionDate { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public long SourceAmount { get; set; } // In paise
    public long TargetAmount { get; set; } // In paise
    public decimal RateUsed { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid? LoanId { get; set; }
    public Guid? ReceiptId { get; set; }
}

public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CurrencyExchangeService> _logger;

    // Default supported currencies
    private readonly HashSet<string> _supportedCurrencies = new()
    {
        "INR", "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "SGD", "HKD", "AED"
    };

    public CurrencyExchangeService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<CurrencyExchangeService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<ExchangeRateSnapshot?> GetCurrentRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default)
    {
        try
        {
            // If same currency, return rate of 1
            if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new ExchangeRateSnapshot
                {
                    Rate = 1m,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    EffectiveDate = DateTime.UtcNow,
                    IsActive = true,
                    Source = "Internal"
                };
            }

            var rate = await _context.Set<ExchangeRateSnapshot>()
                .Where(r => r.FromCurrency == fromCurrency &&
                           r.ToCurrency == toCurrency &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveDate)
                .FirstOrDefaultAsync(ct);

            if (rate == null)
            {
                _logger.LogWarning("No exchange rate found: {From} -> {To}", fromCurrency, toCurrency);
                return null;
            }

            _logger.LogInformation("Exchange rate retrieved: {From}/{To} = {Rate}", 
                fromCurrency, toCurrency, rate.Rate);

            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exchange rate");
            return null;
        }
    }

    public async Task<List<ExchangeRateSnapshot>> GetHistoricalRatesAsync(
        string fromCurrency,
        string toCurrency,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            var rates = await _context.Set<ExchangeRateSnapshot>()
                .Where(r => r.FromCurrency == fromCurrency &&
                           r.ToCurrency == toCurrency &&
                           r.EffectiveDate >= startDate &&
                           r.EffectiveDate <= endDate)
                .OrderByDescending(r => r.EffectiveDate)
                .ToListAsync(ct);

            _logger.LogInformation(
                "Historical rates retrieved: {From}/{To}, Period={Start} to {End}, Count={Count}",
                fromCurrency, toCurrency, startDate.Date, endDate.Date, rates.Count);

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical rates");
            return new List<ExchangeRateSnapshot>();
        }
    }

    public async Task<ExchangeRateSnapshot> UpdateExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        decimal rate,
        string source,
        CancellationToken ct = default)
    {
        try
        {
            if (rate <= 0)
                throw new InvalidOperationException("Exchange rate must be positive");

            if (!_supportedCurrencies.Contains(fromCurrency) || !_supportedCurrencies.Contains(toCurrency))
                throw new InvalidOperationException("Unsupported currency");

            // Deactivate previous rate
            var previousRate = await _context.Set<ExchangeRateSnapshot>()
                .Where(r => r.FromCurrency == fromCurrency &&
                           r.ToCurrency == toCurrency &&
                           r.IsActive)
                .FirstOrDefaultAsync(ct);

            if (previousRate != null)
            {
                previousRate.IsActive = false;
                _context.Update(previousRate);
            }

            // Create new rate
            var newRate = new ExchangeRateSnapshot
            {
                Id = Guid.NewGuid(),
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = rate,
                Source = source,
                EffectiveDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Add(newRate);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Exchange rate updated: {From}/{To} = {Rate} (Source: {Source})",
                fromCurrency, toCurrency, rate, source);

            return newRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange rate");
            throw;
        }
    }

    public async Task<long> ConvertCurrencyAsync(
        long amount,
        string fromCurrency,
        string toCurrency,
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        try
        {
            // Same currency
            if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
                return amount;

            // Get rate
            var rateSnapshot = await GetCurrentRateAsync(fromCurrency, toCurrency, ct);
            if (rateSnapshot == null)
                throw new InvalidOperationException(
                    $"Cannot convert {fromCurrency} to {toCurrency}: rate not available");

            // Convert amount (amount is in paise, maintain precision)
            var convertedAmount = (long)(amount * (decimal)rateSnapshot.Rate);

            _logger.LogInformation(
                "Currency converted: {Amount} {From} = {Converted} {To} (Rate: {Rate})",
                amount, fromCurrency, convertedAmount, toCurrency, rateSnapshot.Rate);

            return convertedAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            throw;
        }
    }

    public async Task<List<string>> GetSupportedCurrenciesAsync(CancellationToken ct = default)
    {
        try
        {
            var currencies = _supportedCurrencies.OrderBy(c => c).ToList();
            
            _logger.LogInformation("Supported currencies retrieved: {Count} currencies", currencies.Count);
            
            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported currencies");
            return new List<string>();
        }
    }

    public async Task<RateCalculationHistory> LogRateConversionAsync(
        long amount,
        string fromCurrency,
        string toCurrency,
        decimal rate,
        long convertedAmount,
        CancellationToken ct = default)
    {
        try
        {
            var history = new RateCalculationHistory
            {
                Id = Guid.NewGuid(),
                ConversionDate = DateTime.UtcNow,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                SourceAmount = amount,
                TargetAmount = convertedAmount,
                RateUsed = rate,
                Source = "System"
            };

            _context.Add(history);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Rate conversion logged: {Amount} {From} -> {Converted} {To}",
                amount, fromCurrency, convertedAmount, toCurrency);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging rate conversion");
            throw;
        }
    }
}
