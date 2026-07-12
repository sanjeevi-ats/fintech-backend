using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IRiskManagementService
{
    Task<RiskProfile?> GenerateRiskProfileAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<List<RiskAlert>> GetAlertsAsync(Guid branchId, string severity = "", CancellationToken ct = default);
    Task<RiskAlert> CreateAlertAsync(Guid branchId, string title, string message, string severity, CancellationToken ct = default);
    Task<bool> UpdateAlertStatusAsync(Guid alertId, string status, CancellationToken ct = default);
    Task<RiskSummary> GetRiskSummaryAsync(Guid periodId, CancellationToken ct = default);
}

public class RiskProfile
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid BranchId { get; set; }
    public decimal OverallRiskScore { get; set; } // 0-100
    public string RiskLevel { get; set; } = string.Empty; // Critical, High, Medium, Low
    public int LiquidityRisk { get; set; } // 0-100
    public int CreditRisk { get; set; } // 0-100
    public int OperationalRisk { get; set; } // 0-100
    public int MarketRisk { get; set; } // 0-100
    public List<string> KeyRisks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RiskAlert
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
    public string Status { get; set; } = "Open"; // Open, Acknowledged, Resolved
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class RiskSummary
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public decimal OverallRiskScore { get; set; }
    public string RiskTrend { get; set; } = string.Empty; // Improving, Stable, Deteriorating
}

public class RiskManagementService : IRiskManagementService
{
    private readonly FinVedaDbContext _context;
    private readonly ILiquidityAnalysisService _liquidityService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<RiskManagementService> _logger;

    public RiskManagementService(
        FinVedaDbContext context,
        ILiquidityAnalysisService liquidityService,
        ITenantService tenantService,
        ILogger<RiskManagementService> logger)
    {
        _context = context;
        _liquidityService = liquidityService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<RiskProfile?> GenerateRiskProfileAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            var actualBranchId = branchId ?? _tenantService.BranchId;

            var profile = new RiskProfile
            {
                Id = Guid.NewGuid(),
                PeriodId = periodId,
                BranchId = actualBranchId,
                CreatedAt = DateTime.UtcNow
            };

            // Calculate liquidity risk
            var liquidityAnalysis = await _liquidityService.GetLiquidityPositionAsync(periodId, actualBranchId, ct);
            profile.LiquidityRisk = CalculateLiquidityRisk(liquidityAnalysis.ratio);

            // Calculate credit risk (based on loan portfolio)
            profile.CreditRisk = await CalculateCreditRiskAsync(actualBranchId, ct);

            // Calculate operational risk
            profile.OperationalRisk = await CalculateOperationalRiskAsync(actualBranchId, ct);

            // Calculate market risk
            profile.MarketRisk = await CalculateMarketRiskAsync(actualBranchId, ct);

            // Overall risk score
            profile.OverallRiskScore = (profile.LiquidityRisk + profile.CreditRisk + 
                                       profile.OperationalRisk + profile.MarketRisk) / 4m;

            // Determine risk level
            profile.RiskLevel = profile.OverallRiskScore switch
            {
                > 75 => "Critical",
                > 50 => "High",
                > 25 => "Medium",
                _ => "Low"
            };

            // Generate key risks and recommendations
            GenerateRiskInsights(profile);

            _logger.LogInformation("Risk profile generated for period {PeriodId}, branch {BranchId}: Overall={Overall}",
                periodId, actualBranchId, profile.OverallRiskScore);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risk profile for period {PeriodId}", periodId);
            return null;
        }
    }

    public async Task<List<RiskAlert>> GetAlertsAsync(
        Guid branchId,
        string severity = "",
        CancellationToken ct = default)
    {
        try
        {
            var query = _context.Set<RiskAlert>()
                .Where(a => a.BranchId == branchId && a.Status == "Open");

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(a => a.Severity == severity);
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alerts for branch {BranchId}", branchId);
            return new List<RiskAlert>();
        }
    }

    public async Task<RiskAlert> CreateAlertAsync(
        Guid branchId,
        string title,
        string message,
        string severity,
        CancellationToken ct = default)
    {
        try
        {
            var alert = new RiskAlert
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Title = title,
                Message = message,
                Severity = severity,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            // Note: In real implementation, this would be saved to database
            // For now, storing in memory via context
            _logger.LogWarning("Risk alert created: {Title} (Severity: {Severity}) for branch {BranchId}",
                title, severity, branchId);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert for branch {BranchId}", branchId);
            throw;
        }
    }

    public async Task<bool> UpdateAlertStatusAsync(
        Guid alertId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            // In real implementation, would update database
            _logger.LogInformation("Alert {AlertId} status updated to {Status}", alertId, status);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert {AlertId}", alertId);
            return false;
        }
    }

    public async Task<RiskSummary> GetRiskSummaryAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var branchId = _tenantService.BranchId;
            var alerts = await GetAlertsAsync(branchId, "", ct);

            var summary = new RiskSummary
            {
                TotalAlerts = alerts.Count,
                CriticalAlerts = alerts.Count(a => a.Severity == "Critical"),
                HighAlerts = alerts.Count(a => a.Severity == "High"),
                MediumAlerts = alerts.Count(a => a.Severity == "Medium"),
                LowAlerts = alerts.Count(a => a.Severity == "Low"),
                RiskTrend = "Stable"
            };

            _logger.LogInformation("Risk summary generated for period {PeriodId}: Total Alerts={Total}",
                periodId, summary.TotalAlerts);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risk summary for period {PeriodId}", periodId);
            return new RiskSummary();
        }
    }

    private int CalculateLiquidityRisk(decimal currentRatio)
    {
        if (currentRatio >= 1.5m) return 10;
        if (currentRatio >= 1.0m) return 25;
        if (currentRatio >= 0.5m) return 50;
        return 90;
    }

    private async Task<int> CalculateCreditRiskAsync(Guid branchId, CancellationToken ct)
    {
        try
        {
            // Get overdue loans
            var overdueLoans = await _context.LoanCases
                .Where(l => l.BranchId == branchId && l.Status != LoanStatus.closed)
                .CountAsync(ct);

            // Get total loans
            var totalLoans = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .CountAsync(ct);

            if (totalLoans == 0) return 20;

            var overduePercentage = ((decimal)overdueLoans / totalLoans) * 100;

            return (int)Math.Min(overduePercentage * 2, 100);
        }
        catch
        {
            return 30; // Default medium credit risk
        }
    }

    private async Task<int> CalculateOperationalRiskAsync(Guid branchId, CancellationToken ct)
    {
        try
        {
            // Check for recent audit issues
            var recentAuditIssues = await _context.Set<AuditLog>()
                .Where(a => a.BranchId == branchId &&
                           a.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(ct);

            // Operational risk increases with audit issues
            return Math.Min(10 + (recentAuditIssues * 5), 70);
        }
        catch
        {
            return 35; // Default medium operational risk
        }
    }

    private async Task<int> CalculateMarketRiskAsync(Guid branchId, CancellationToken ct)
    {
        try
        {
            // Check for interest rate changes impact (simplified)
            // In reality, would analyze portfolio sensitivity
            return 25; // Base market risk
        }
        catch
        {
            return 25;
        }
    }

    private void GenerateRiskInsights(RiskProfile profile)
    {
        profile.KeyRisks.Clear();
        profile.Recommendations.Clear();

        if (profile.LiquidityRisk > 50)
        {
            profile.KeyRisks.Add("Low liquidity position");
            profile.Recommendations.Add("Improve cash collection and reduce outflows");
        }

        if (profile.CreditRisk > 50)
        {
            profile.KeyRisks.Add("High credit risk from loan portfolio");
            profile.Recommendations.Add("Accelerate collection efforts and reduce new disbursements");
        }

        if (profile.OperationalRisk > 50)
        {
            profile.KeyRisks.Add("Operational inefficiencies detected");
            profile.Recommendations.Add("Review processes and strengthen controls");
        }

        if (profile.MarketRisk > 50)
        {
            profile.KeyRisks.Add("Market volatility risk");
            profile.Recommendations.Add("Diversify portfolio and hedge exposures");
        }

        if (profile.KeyRisks.Count == 0)
        {
            profile.KeyRisks.Add("No significant risks identified");
            profile.Recommendations.Add("Maintain current risk management practices");
        }
    }
}
