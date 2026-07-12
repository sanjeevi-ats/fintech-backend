using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IRateProviderService
{
    Task<RateProviderConfiguration?> ConfigureRateProviderAsync(string name, string apiUrl, string apiKey, Dictionary<string, string> settings, CancellationToken ct = default);
    Task<RateProviderConfiguration?> GetRateProviderAsync(Guid providerId, CancellationToken ct = default);
    Task<List<RateProviderConfiguration>> GetAllRateProvidersAsync(CancellationToken ct = default);
    Task SetPrimaryProviderAsync(Guid providerId, CancellationToken ct = default);
    Task<RateProviderFetch> RecordRateFetchAsync(Guid providerId, decimal rate, string fromCurrency, string toCurrency, bool success, long responseTimeMs, CancellationToken ct = default);
    Task<List<RateProviderFetch>> GetProviderFetchHistoryAsync(Guid providerId, int limitDays, CancellationToken ct = default);
    Task<RateProviderHealthReport> GetProviderHealthAsync(Guid providerId, CancellationToken ct = default);
}

public class RateProviderConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // "RBI", "Bloomberg", "OANDA", "XE", etc.
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = "Active"; // Active, Testing, Inactive, Error
    public Dictionary<string, string> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastFetchAt { get; set; }
    public long SuccessfulFetches { get; set; }
    public long FailedFetches { get; set; }
    public decimal AverageResponseTimeMs { get; set; }
}

public class RateProviderFetch
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool Success { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime FetchTime { get; set; }
}

public class RateProviderHealthReport
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public long TotalFetches { get; set; }
    public long SuccessfulFetches { get; set; }
    public long FailedFetches { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal AverageResponseTimeMs { get; set; }
    public DateTime LastHealthCheckAt { get; set; }
    public Dictionary<string, decimal> LatestRates { get; set; } = new();
}

public class RateProviderService : IRateProviderService
{
    private readonly FinVedaDbContext _context;
    private readonly ILogger<RateProviderService> _logger;

    public RateProviderService(
        FinVedaDbContext context,
        ILogger<RateProviderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RateProviderConfiguration?> ConfigureRateProviderAsync(
        string name,
        string apiUrl,
        string apiKey,
        Dictionary<string, string> settings,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(apiUrl))
                throw new InvalidOperationException("Name and ApiUrl are required");

            var provider = new RateProviderConfiguration
            {
                Id = Guid.NewGuid(),
                Name = name,
                ApiUrl = apiUrl,
                ApiKey = apiKey,
                Settings = settings ?? new Dictionary<string, string>(),
                Status = "Testing",
                CreatedAt = DateTime.UtcNow,
                LastFetchAt = DateTime.UtcNow
            };

            _context.Add(provider);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Rate provider configured: Name={Name}, ApiUrl={Url}",
                name, apiUrl);

            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring rate provider");
            return null;
        }
    }

    public async Task<RateProviderConfiguration?> GetRateProviderAsync(
        Guid providerId,
        CancellationToken ct = default)
    {
        try
        {
            var provider = await _context.Set<RateProviderConfiguration>()
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);

            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate provider");
            return null;
        }
    }

    public async Task<List<RateProviderConfiguration>> GetAllRateProvidersAsync(
        CancellationToken ct = default)
    {
        try
        {
            var providers = await _context.Set<RateProviderConfiguration>()
                .OrderByDescending(p => p.IsPrimary)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);

            _logger.LogInformation("All rate providers retrieved: Count={Count}", providers.Count);

            return providers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate providers");
            return new List<RateProviderConfiguration>();
        }
    }

    public async Task SetPrimaryProviderAsync(
        Guid providerId,
        CancellationToken ct = default)
    {
        try
        {
            // Remove primary from all others
            var allProviders = await _context.Set<RateProviderConfiguration>().ToListAsync(ct);
            foreach (var provider in allProviders)
            {
                provider.IsPrimary = false;
                _context.Update(provider);
            }

            // Set as primary
            var primaryProvider = await _context.Set<RateProviderConfiguration>()
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);

            if (primaryProvider != null)
            {
                primaryProvider.IsPrimary = true;
                _context.Update(primaryProvider);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Primary rate provider set: ProviderId={Id}",
                providerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary provider");
            throw;
        }
    }

    public async Task<RateProviderFetch> RecordRateFetchAsync(
        Guid providerId,
        decimal rate,
        string fromCurrency,
        string toCurrency,
        bool success,
        long responseTimeMs,
        CancellationToken ct = default)
    {
        try
        {
            var fetch = new RateProviderFetch
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Rate = rate,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Success = success,
                ResponseTimeMs = responseTimeMs,
                FetchTime = DateTime.UtcNow
            };

            _context.Add(fetch);

            // Update provider statistics
            var provider = await _context.Set<RateProviderConfiguration>()
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);

            if (provider != null)
            {
                if (success)
                    provider.SuccessfulFetches++;
                else
                    provider.FailedFetches++;

                // Update average response time
                var totalFetches = provider.SuccessfulFetches + provider.FailedFetches;
                provider.AverageResponseTimeMs = (provider.AverageResponseTimeMs + responseTimeMs) / 2;
                provider.LastFetchAt = DateTime.UtcNow;

                _context.Update(provider);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Rate fetch recorded: ProviderId={Id}, Pair={From}/{To}, Success={Success}, ResponseTime={Time}ms",
                providerId, fromCurrency, toCurrency, success, responseTimeMs);

            return fetch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording rate fetch");
            throw;
        }
    }

    public async Task<List<RateProviderFetch>> GetProviderFetchHistoryAsync(
        Guid providerId,
        int limitDays,
        CancellationToken ct = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-limitDays);

            var history = await _context.Set<RateProviderFetch>()
                .Where(f => f.ProviderId == providerId && f.FetchTime >= cutoffDate)
                .OrderByDescending(f => f.FetchTime)
                .ToListAsync(ct);

            _logger.LogInformation(
                "Provider fetch history retrieved: ProviderId={Id}, Days={Days}, Count={Count}",
                providerId, limitDays, history.Count);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider fetch history");
            return new List<RateProviderFetch>();
        }
    }

    public async Task<RateProviderHealthReport> GetProviderHealthAsync(
        Guid providerId,
        CancellationToken ct = default)
    {
        try
        {
            var provider = await _context.Set<RateProviderConfiguration>()
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);

            if (provider == null)
                throw new InvalidOperationException("Provider not found");

            var totalFetches = provider.SuccessfulFetches + provider.FailedFetches;
            var successRate = totalFetches > 0
                ? (provider.SuccessfulFetches * 100m) / totalFetches
                : 100m;

            // Get latest rates
            var latestFetches = await _context.Set<RateProviderFetch>()
                .Where(f => f.ProviderId == providerId)
                .OrderByDescending(f => f.FetchTime)
                .Take(10)
                .ToListAsync(ct);

            var latestRates = latestFetches
                .GroupBy(f => $"{f.FromCurrency}/{f.ToCurrency}")
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Rate
                );

            var report = new RateProviderHealthReport
            {
                ProviderId = providerId,
                ProviderName = provider.Name,
                Status = provider.Status,
                IsPrimary = provider.IsPrimary,
                TotalFetches = totalFetches,
                SuccessfulFetches = provider.SuccessfulFetches,
                FailedFetches = provider.FailedFetches,
                SuccessRate = successRate,
                AverageResponseTimeMs = provider.AverageResponseTimeMs,
                LastHealthCheckAt = DateTime.UtcNow,
                LatestRates = latestRates
            };

            _logger.LogInformation(
                "Provider health report generated: Provider={Name}, SuccessRate={Rate}%",
                provider.Name, successRate.ToString("F2"));

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provider health report");
            throw;
        }
    }
}
