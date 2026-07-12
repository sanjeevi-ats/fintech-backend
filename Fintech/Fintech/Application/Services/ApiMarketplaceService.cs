using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IApiMarketplaceService
{
    Task<ApiIntegration?> RegisterIntegrationAsync(string name, string category, string baseUrl, string apiKey, CancellationToken ct = default);
    Task<ApiIntegration?> GetIntegrationAsync(Guid integrationId, CancellationToken ct = default);
    Task<List<ApiIntegration>> GetIntegrationsByCategoryAsync(string category, CancellationToken ct = default);
    Task<List<ApiIntegration>> GetAllIntegrationsAsync(CancellationToken ct = default);
    Task UpdateIntegrationStatusAsync(Guid integrationId, string status, CancellationToken ct = default);
    Task<ApiIntegrationLog> LogIntegrationCallAsync(Guid integrationId, string endpoint, string method, long responseTimeMs, bool success, string? errorMessage = null, CancellationToken ct = default);
    Task<List<ApiIntegrationLog>> GetIntegrationLogsAsync(Guid integrationId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

public class ApiIntegration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "RateProvider", "PaymentGateway", "Analytics", etc.
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Inactive, Testing, Error
    public Dictionary<string, string> Configuration { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastHealthCheckAt { get; set; }
    public int SuccessfulCalls { get; set; }
    public int FailedCalls { get; set; }
    public decimal UptimePercentage { get; set; }
}

public class ApiIntegrationLog
{
    public Guid Id { get; set; }
    public Guid IntegrationId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty; // GET, POST, PUT, DELETE
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CallTime { get; set; }
}

public class ApiMarketplaceService : IApiMarketplaceService
{
    private readonly FinVedaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ApiMarketplaceService> _logger;

    public ApiMarketplaceService(
        FinVedaDbContext context,
        ITenantService tenantService,
        ILogger<ApiMarketplaceService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<ApiIntegration?> RegisterIntegrationAsync(
        string name,
        string category,
        string baseUrl,
        string apiKey,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Name and BaseUrl are required");

            var integration = new ApiIntegration
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                BaseUrl = baseUrl,
                ApiKey = apiKey,
                Status = "Testing", // Start in testing mode
                CreatedAt = DateTime.UtcNow,
                LastHealthCheckAt = DateTime.UtcNow,
                UptimePercentage = 100m
            };

            _context.Add(integration);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "API Integration registered: Name={Name}, Category={Category}, BaseUrl={BaseUrl}",
                name, category, baseUrl);

            return integration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering API integration");
            return null;
        }
    }

    public async Task<ApiIntegration?> GetIntegrationAsync(
        Guid integrationId,
        CancellationToken ct = default)
    {
        try
        {
            var integration = await _context.Set<ApiIntegration>()
                .FirstOrDefaultAsync(i => i.Id == integrationId, ct);

            return integration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integration");
            return null;
        }
    }

    public async Task<List<ApiIntegration>> GetIntegrationsByCategoryAsync(
        string category,
        CancellationToken ct = default)
    {
        try
        {
            var integrations = await _context.Set<ApiIntegration>()
                .Where(i => i.Category == category && i.Status == "Active")
                .ToListAsync(ct);

            _logger.LogInformation(
                "Integrations retrieved by category: Category={Category}, Count={Count}",
                category, integrations.Count);

            return integrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations by category");
            return new List<ApiIntegration>();
        }
    }

    public async Task<List<ApiIntegration>> GetAllIntegrationsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var integrations = await _context.Set<ApiIntegration>()
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            _logger.LogInformation("All integrations retrieved: Count={Count}", integrations.Count);

            return integrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all integrations");
            return new List<ApiIntegration>();
        }
    }

    public async Task UpdateIntegrationStatusAsync(
        Guid integrationId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            var integration = await _context.Set<ApiIntegration>()
                .FirstOrDefaultAsync(i => i.Id == integrationId, ct);

            if (integration == null)
                throw new InvalidOperationException("Integration not found");

            integration.Status = status;
            integration.LastHealthCheckAt = DateTime.UtcNow;

            _context.Update(integration);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Integration status updated: IntegrationId={Id}, Status={Status}",
                integrationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integration status");
            throw;
        }
    }

    public async Task<ApiIntegrationLog> LogIntegrationCallAsync(
        Guid integrationId,
        string endpoint,
        string method,
        long responseTimeMs,
        bool success,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new ApiIntegrationLog
            {
                Id = Guid.NewGuid(),
                IntegrationId = integrationId,
                Endpoint = endpoint,
                Method = method,
                ResponseTimeMs = responseTimeMs,
                Success = success,
                ErrorMessage = errorMessage,
                CallTime = DateTime.UtcNow
            };

            _context.Add(log);

            // Update integration statistics
            var integration = await _context.Set<ApiIntegration>()
                .FirstOrDefaultAsync(i => i.Id == integrationId, ct);

            if (integration != null)
            {
                if (success)
                    integration.SuccessfulCalls++;
                else
                    integration.FailedCalls++;

                var totalCalls = integration.SuccessfulCalls + integration.FailedCalls;
                integration.UptimePercentage = totalCalls > 0
                    ? (integration.SuccessfulCalls * 100m) / totalCalls
                    : 100m;

                _context.Update(integration);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Integration call logged: IntegrationId={Id}, Endpoint={Endpoint}, Success={Success}, ResponseTime={Time}ms",
                integrationId, endpoint, success, responseTimeMs);

            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging integration call");
            throw;
        }
    }

    public async Task<List<ApiIntegrationLog>> GetIntegrationLogsAsync(
        Guid integrationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            var logs = await _context.Set<ApiIntegrationLog>()
                .Where(l => l.IntegrationId == integrationId &&
                           l.CallTime >= startDate &&
                           l.CallTime <= endDate)
                .OrderByDescending(l => l.CallTime)
                .ToListAsync(ct);

            _logger.LogInformation(
                "Integration logs retrieved: IntegrationId={Id}, Period={Start}-{End}, Count={Count}",
                integrationId, startDate.Date, endDate.Date, logs.Count);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integration logs");
            return new List<ApiIntegrationLog>();
        }
    }
}
