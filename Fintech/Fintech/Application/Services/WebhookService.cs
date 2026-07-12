using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IWebhookService
{
    Task<WebhookSubscription?> RegisterWebhookAsync(Guid integrationId, string eventType, string url, CancellationToken ct = default);
    Task<WebhookSubscription?> GetWebhookAsync(Guid webhookId, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetWebhooksByEventAsync(string eventType, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetWebhooksByIntegrationAsync(Guid integrationId, CancellationToken ct = default);
    Task UpdateWebhookStatusAsync(Guid webhookId, string status, CancellationToken ct = default);
    Task<WebhookDelivery> LogWebhookDeliveryAsync(Guid webhookId, string payload, long responseTimeMs, int statusCode, bool success, CancellationToken ct = default);
    Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(Guid webhookId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetWebhookEventStatsAsync(CancellationToken ct = default);
}

public class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid IntegrationId { get; set; }
    public string EventType { get; set; } = string.Empty; // "RateUpdated", "LoanCreated", "CollectionMade", etc.
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty; // For HMAC signature verification
    public string Status { get; set; } = "Active"; // Active, Inactive, Testing, Error
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public DateTime CreatedAt { get; set; }
    public DateTime LastTriggeredAt { get; set; }
    public long SuccessfulDeliveries { get; set; }
    public long FailedDeliveries { get; set; }
}

public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryAttempt { get; set; }
    public DateTime DeliveryTime { get; set; }
}

public class WebhookService : IWebhookService
{
    private readonly FinVedaDbContext _context;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        FinVedaDbContext context,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WebhookSubscription?> RegisterWebhookAsync(
        Guid integrationId,
        string eventType,
        string url,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("EventType and Url are required");

            var webhook = new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                IntegrationId = integrationId,
                EventType = eventType,
                Url = url,
                Secret = GenerateWebhookSecret(),
                Status = "Testing",
                CreatedAt = DateTime.UtcNow,
                LastTriggeredAt = DateTime.UtcNow
            };

            _context.Add(webhook);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Webhook registered: EventType={EventType}, Url={Url}, IntegrationId={IntegrationId}",
                eventType, url, integrationId);

            return webhook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return null;
        }
    }

    public async Task<WebhookSubscription?> GetWebhookAsync(
        Guid webhookId,
        CancellationToken ct = default)
    {
        try
        {
            var webhook = await _context.Set<WebhookSubscription>()
                .FirstOrDefaultAsync(w => w.Id == webhookId, ct);

            return webhook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook");
            return null;
        }
    }

    public async Task<List<WebhookSubscription>> GetWebhooksByEventAsync(
        string eventType,
        CancellationToken ct = default)
    {
        try
        {
            var webhooks = await _context.Set<WebhookSubscription>()
                .Where(w => w.EventType == eventType && w.Status == "Active")
                .ToListAsync(ct);

            _logger.LogInformation(
                "Webhooks retrieved by event: EventType={EventType}, Count={Count}",
                eventType, webhooks.Count);

            return webhooks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks by event");
            return new List<WebhookSubscription>();
        }
    }

    public async Task<List<WebhookSubscription>> GetWebhooksByIntegrationAsync(
        Guid integrationId,
        CancellationToken ct = default)
    {
        try
        {
            var webhooks = await _context.Set<WebhookSubscription>()
                .Where(w => w.IntegrationId == integrationId)
                .ToListAsync(ct);

            return webhooks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks by integration");
            return new List<WebhookSubscription>();
        }
    }

    public async Task UpdateWebhookStatusAsync(
        Guid webhookId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            var webhook = await _context.Set<WebhookSubscription>()
                .FirstOrDefaultAsync(w => w.Id == webhookId, ct);

            if (webhook == null)
                throw new InvalidOperationException("Webhook not found");

            webhook.Status = status;
            _context.Update(webhook);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Webhook status updated: WebhookId={Id}, Status={Status}",
                webhookId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook status");
            throw;
        }
    }

    public async Task<WebhookDelivery> LogWebhookDeliveryAsync(
        Guid webhookId,
        string payload,
        long responseTimeMs,
        int statusCode,
        bool success,
        CancellationToken ct = default)
    {
        try
        {
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookId = webhookId,
                Payload = payload,
                ResponseTimeMs = responseTimeMs,
                StatusCode = statusCode,
                Success = success,
                DeliveryTime = DateTime.UtcNow
            };

            _context.Add(delivery);

            // Update webhook statistics
            var webhook = await _context.Set<WebhookSubscription>()
                .FirstOrDefaultAsync(w => w.Id == webhookId, ct);

            if (webhook != null)
            {
                if (success)
                    webhook.SuccessfulDeliveries++;
                else
                    webhook.FailedDeliveries++;

                webhook.LastTriggeredAt = DateTime.UtcNow;
                _context.Update(webhook);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Webhook delivery logged: WebhookId={Id}, Success={Success}, ResponseTime={Time}ms",
                webhookId, success, responseTimeMs);

            return delivery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging webhook delivery");
            throw;
        }
    }

    public async Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(
        Guid webhookId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            var deliveries = await _context.Set<WebhookDelivery>()
                .Where(d => d.WebhookId == webhookId &&
                           d.DeliveryTime >= startDate &&
                           d.DeliveryTime <= endDate)
                .OrderByDescending(d => d.DeliveryTime)
                .ToListAsync(ct);

            return deliveries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook deliveries");
            return new List<WebhookDelivery>();
        }
    }

    public async Task<Dictionary<string, int>> GetWebhookEventStatsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var stats = await _context.Set<WebhookSubscription>()
                .GroupBy(w => w.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count, ct);

            _logger.LogInformation("Webhook event stats retrieved: Count={Count}", stats.Count);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook event stats");
            return new Dictionary<string, int>();
        }
    }

    private string GenerateWebhookSecret()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
