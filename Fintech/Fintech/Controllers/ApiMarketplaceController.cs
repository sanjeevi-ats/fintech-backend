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
public class ApiMarketplaceController : ControllerBase
{
    private readonly IApiMarketplaceService _marketplaceService;
    private readonly IWebhookService _webhookService;
    private readonly IRateProviderService _rateProviderService;
    private readonly ILogger<ApiMarketplaceController> _logger;

    public ApiMarketplaceController(
        IApiMarketplaceService marketplaceService,
        IWebhookService webhookService,
        IRateProviderService rateProviderService,
        ILogger<ApiMarketplaceController> logger)
    {
        _marketplaceService = marketplaceService;
        _webhookService = webhookService;
        _rateProviderService = rateProviderService;
        _logger = logger;
    }

    #region API Integration Endpoints

    /// <summary>
    /// Register a new API integration
    /// </summary>
    [HttpPost("integrations/register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterIntegration(
        [FromBody] RegisterIntegrationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/apimarketplace/integrations/register - Name={Name}, Category={Category}",
                request?.Name, request?.Category);

            var integration = await _marketplaceService.RegisterIntegrationAsync(
                request?.Name ?? string.Empty,
                request?.Category ?? string.Empty,
                request?.BaseUrl ?? string.Empty,
                request?.ApiKey ?? string.Empty,
                ct);

            if (integration == null)
                return BadRequest(new { error = "Failed to register integration" });

            _logger.LogInformation(
                "POST /api/apimarketplace/integrations/register - Success (201): IntegrationId={Id}",
                integration.Id);

            return Created(string.Empty, new { data = integration, message = "Integration registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering integration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all integrations
    /// </summary>
    [HttpGet("integrations")]
    public async Task<IActionResult> GetAllIntegrations(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/integrations");

            var integrations = await _marketplaceService.GetAllIntegrationsAsync(ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/integrations - Success (200): Count={Count}",
                integrations.Count);

            return Ok(new { data = integrations, message = "Integrations retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get integrations by category
    /// </summary>
    [HttpGet("integrations/category/{category}")]
    public async Task<IActionResult> GetIntegrationsByCategory(
        string category,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/integrations/category/{Category}", category);

            var integrations = await _marketplaceService.GetIntegrationsByCategoryAsync(category, ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/integrations/category - Success (200): Count={Count}",
                integrations.Count);

            return Ok(new { data = integrations, message = "Integrations retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations by category");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update integration status
    /// </summary>
    [HttpPut("integrations/{integrationId}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateIntegrationStatus(
        Guid integrationId,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "PUT /api/apimarketplace/integrations/{IntegrationId}/status - Status={Status}",
                integrationId, request?.Status);

            await _marketplaceService.UpdateIntegrationStatusAsync(
                integrationId, request?.Status ?? "Inactive", ct);

            _logger.LogInformation(
                "PUT /api/apimarketplace/integrations/status - Success (200): IntegrationId={Id}",
                integrationId);

            return Ok(new { message = "Integration status updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integration status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Webhook Endpoints

    /// <summary>
    /// Register a webhook
    /// </summary>
    [HttpPost("webhooks/register")]
    public async Task<IActionResult> RegisterWebhook(
        [FromBody] RegisterWebhookRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/apimarketplace/webhooks/register - EventType={EventType}, Url={Url}",
                request?.EventType, request?.Url);

            var webhook = await _webhookService.RegisterWebhookAsync(
                request?.IntegrationId ?? Guid.Empty,
                request?.EventType ?? string.Empty,
                request?.Url ?? string.Empty,
                ct);

            if (webhook == null)
                return BadRequest(new { error = "Failed to register webhook" });

            _logger.LogInformation(
                "POST /api/apimarketplace/webhooks/register - Success (201): WebhookId={Id}",
                webhook.Id);

            return Created(string.Empty, new { data = webhook, message = "Webhook registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get webhooks by event type
    /// </summary>
    [HttpGet("webhooks/event/{eventType}")]
    public async Task<IActionResult> GetWebhooksByEvent(
        string eventType,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/webhooks/event/{EventType}", eventType);

            var webhooks = await _webhookService.GetWebhooksByEventAsync(eventType, ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/webhooks/event - Success (200): Count={Count}",
                webhooks.Count);

            return Ok(new { data = webhooks, message = "Webhooks retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get webhook deliveries
    /// </summary>
    [HttpGet("webhooks/{webhookId}/deliveries")]
    public async Task<IActionResult> GetWebhookDeliveries(
        Guid webhookId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/apimarketplace/webhooks/{WebhookId}/deliveries - Period={Start} to {End}",
                webhookId, startDate.Date, endDate.Date);

            var deliveries = await _webhookService.GetWebhookDeliveriesAsync(
                webhookId, startDate, endDate, ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/webhooks/deliveries - Success (200): Count={Count}",
                deliveries.Count);

            return Ok(new { data = deliveries, message = "Webhook deliveries retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook deliveries");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get webhook event statistics
    /// </summary>
    [HttpGet("webhooks/stats/events")]
    public async Task<IActionResult> GetWebhookEventStats(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/webhooks/stats/events");

            var stats = await _webhookService.GetWebhookEventStatsAsync(ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/webhooks/stats/events - Success (200): EventTypes={Count}",
                stats.Count);

            return Ok(new { data = stats, message = "Webhook event statistics retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Rate Provider Endpoints

    /// <summary>
    /// Configure a rate provider
    /// </summary>
    [HttpPost("rate-providers/configure")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> ConfigureRateProvider(
        [FromBody] ConfigureRateProviderRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/apimarketplace/rate-providers/configure - Name={Name}",
                request?.Name);

            var provider = await _rateProviderService.ConfigureRateProviderAsync(
                request?.Name ?? string.Empty,
                request?.ApiUrl ?? string.Empty,
                request?.ApiKey ?? string.Empty,
                request?.Settings ?? new Dictionary<string, string>(),
                ct);

            if (provider == null)
                return BadRequest(new { error = "Failed to configure provider" });

            _logger.LogInformation(
                "POST /api/apimarketplace/rate-providers/configure - Success (201): ProviderId={Id}",
                provider.Id);

            return Created(string.Empty, new { data = provider, message = "Rate provider configured" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring rate provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all rate providers
    /// </summary>
    [HttpGet("rate-providers")]
    public async Task<IActionResult> GetAllRateProviders(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/rate-providers");

            var providers = await _rateProviderService.GetAllRateProvidersAsync(ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/rate-providers - Success (200): Count={Count}",
                providers.Count);

            return Ok(new { data = providers, message = "Rate providers retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate providers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set primary rate provider
    /// </summary>
    [HttpPut("rate-providers/{providerId}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPrimaryProvider(
        Guid providerId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "PUT /api/apimarketplace/rate-providers/{ProviderId}/primary",
                providerId);

            await _rateProviderService.SetPrimaryProviderAsync(providerId, ct);

            _logger.LogInformation(
                "PUT /api/apimarketplace/rate-providers/primary - Success (200): ProviderId={Id}",
                providerId);

            return Ok(new { message = "Primary rate provider set" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get rate provider health
    /// </summary>
    [HttpGet("rate-providers/{providerId}/health")]
    public async Task<IActionResult> GetProviderHealth(
        Guid providerId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GET /api/apimarketplace/rate-providers/{ProviderId}/health", providerId);

            var health = await _rateProviderService.GetProviderHealthAsync(providerId, ct);

            _logger.LogInformation(
                "GET /api/apimarketplace/rate-providers/health - Success (200): Provider={Name}, SuccessRate={Rate}%",
                health.ProviderName, health.SuccessRate.ToString("F2"));

            return Ok(new { data = health, message = "Provider health report retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider health");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion
}

public class RegisterIntegrationRequest
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class RegisterWebhookRequest
{
    public Guid IntegrationId { get; set; }
    public string? EventType { get; set; }
    public string? Url { get; set; }
}

public class ConfigureRateProviderRequest
{
    public string? Name { get; set; }
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}

public class UpdateStatusRequest
{
    public string? Status { get; set; }
}
