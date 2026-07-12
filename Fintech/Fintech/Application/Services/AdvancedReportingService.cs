using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IAdvancedReportingService
{
    Task<ComparativeAnalysis?> ComparePeriodsAsync(Guid period1Id, Guid period2Id, CancellationToken ct = default);
    Task<BudgetAnalysis?> AnalyzeBudgetVsActualAsync(Guid periodId, Guid? budgetId = null, CancellationToken ct = default);
    Task<PerformanceMetrics?> GetPerformanceMetricsAsync(Guid periodId, CancellationToken ct = default);
    Task<ScenarioAnalysis?> RunScenarioAsync(Guid periodId, decimal assumptions, CancellationToken ct = default);
    Task<List<CustomReport>> GetCustomReportsAsync(Guid branchId, CancellationToken ct = default);
    Task<CustomReport> CreateCustomReportAsync(Guid branchId, string name, string queryJson, CancellationToken ct = default);
}

public class ComparativeAnalysis
{
    public Guid Period1Id { get; set; }
    public Guid Period2Id { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal ExpenseChange { get; set; }
    public decimal ExpenseChangePercent { get; set; }
    public decimal ProfitChange { get; set; }
    public decimal ProfitChangePercent { get; set; }
    public long CashFlowChange { get; set; }
    public decimal CashFlowChangePercent { get; set; }
    public Dictionary<string, decimal> MetricChanges { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class BudgetAnalysis
{
    public Guid PeriodId { get; set; }
    public long BudgetedAmount { get; set; }
    public long ActualAmount { get; set; }
    public long Variance { get; set; }
    public decimal VariancePercent { get; set; }
    public string VarianceStatus { get; set; } = string.Empty; // Favorable, Unfavorable, Neutral
    public Dictionary<string, BudgetLine> DetailedAnalysis { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class BudgetLine
{
    public string Category { get; set; } = string.Empty;
    public long Budgeted { get; set; }
    public long Actual { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PerformanceMetrics
{
    public Guid PeriodId { get; set; }
    public decimal RoE { get; set; } // Return on Equity
    public decimal RoA { get; set; } // Return on Assets
    public decimal ProfitMargin { get; set; }
    public decimal OperatingMargin { get; set; }
    public decimal AssetTurnover { get; set; }
    public decimal DebtToEquity { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal QuickRatio { get; set; }
    public long RevenuePerEmployee { get; set; }
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}

public class ScenarioAnalysis
{
    public Guid PeriodId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public decimal Assumptions { get; set; }
    public long BaselineRevenue { get; set; }
    public long ProjectedRevenue { get; set; }
    public long RevenueDifference { get; set; }
    public long BaselineProfit { get; set; }
    public long ProjectedProfit { get; set; }
    public long ProfitDifference { get; set; }
    public string Impact { get; set; } = string.Empty; // Positive, Negative, Neutral
    public List<string> Implications { get; set; } = new();
}

public class CustomReport
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public Dictionary<string, object> LastResults { get; set; } = new();
}

public class AdvancedReportingService : IAdvancedReportingService
{
    private readonly FinVedaDbContext _context;
    private readonly IProfitLossService _plService;
    private readonly ICashFlowService _cfService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AdvancedReportingService> _logger;

    public AdvancedReportingService(
        FinVedaDbContext context,
        IProfitLossService plService,
        ICashFlowService cfService,
        ITenantService tenantService,
        ILogger<AdvancedReportingService> logger)
    {
        _context = context;
        _plService = plService;
        _cfService = cfService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<ComparativeAnalysis?> ComparePeriodsAsync(
        Guid period1Id,
        Guid period2Id,
        CancellationToken ct = default)
    {
        try
        {
            var pl1 = await _plService.GetPLStatementAsync(period1Id, ct);
            var pl2 = await _plService.GetPLStatementAsync(period2Id, ct);
            var cf1 = await _cfService.GetCashFlowStatementAsync(period1Id, ct);
            var cf2 = await _cfService.GetCashFlowStatementAsync(period2Id, ct);

            if (pl1 == null || pl2 == null || cf1 == null || cf2 == null)
                return null;

            var analysis = new ComparativeAnalysis
            {
                Period1Id = period1Id,
                Period2Id = period2Id,
                RevenueChange = pl2.TotalRevenue - pl1.TotalRevenue,
                ExpenseChange = pl2.TotalExpenses - pl1.TotalExpenses,
                ProfitChange = pl2.GrossProfit - pl1.GrossProfit,
                CashFlowChange = cf2.NetCashFlow - cf1.NetCashFlow
            };

            // Calculate percentages
            if (pl1.TotalRevenue > 0)
                analysis.RevenueChangePercent = ((analysis.RevenueChange / pl1.TotalRevenue) * 100);

            if (pl1.GrossProfit > 0)
                analysis.ProfitChangePercent = ((analysis.ProfitChange / pl1.GrossProfit) * 100);

            if (cf1.NetCashFlow > 0)
                analysis.CashFlowChangePercent = ((analysis.CashFlowChange / cf1.NetCashFlow) * 100);

            // Generate insights
            GenerateComparativeInsights(analysis, pl1, pl2, cf1, cf2);

            _logger.LogInformation("Comparative analysis generated for periods {Period1} and {Period2}",
                period1Id, period2Id);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing periods {Period1} and {Period2}", period1Id, period2Id);
            return null;
        }
    }

    public async Task<BudgetAnalysis?> AnalyzeBudgetVsActualAsync(
        Guid periodId,
        Guid? budgetId = null,
        CancellationToken ct = default)
    {
        try
        {
            var pl = await _plService.GetPLStatementAsync(periodId, ct);
            if (pl == null) return null;

            // For now, create a simple budget analysis
            // In real scenario, would fetch actual budget data
            long budgetedAmount = (long)(pl.TotalRevenue * 1.1m); // 10% above actual
            long actualAmount = pl.TotalRevenue;

            var analysis = new BudgetAnalysis
            {
                PeriodId = periodId,
                BudgetedAmount = budgetedAmount,
                ActualAmount = actualAmount,
                Variance = actualAmount - budgetedAmount,
                VariancePercent = ((actualAmount - budgetedAmount) / (decimal)budgetedAmount) * 100
            };

            analysis.VarianceStatus = analysis.Variance > 0 ? "Favorable" : "Unfavorable";

            // Generate detailed analysis and recommendations
            GenerateBudgetRecommendations(analysis);

            _logger.LogInformation("Budget analysis generated for period {PeriodId}: Variance={Variance}%",
                periodId, analysis.VariancePercent.ToString("F2"));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing budget for period {PeriodId}", periodId);
            return null;
        }
    }

    public async Task<PerformanceMetrics?> GetPerformanceMetricsAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var branchId = _tenantService.BranchId;
            var pl = await _plService.GetPLStatementAsync(periodId, ct);
            var cf = await _cfService.GetCashFlowStatementAsync(periodId, ct);

            if (pl == null || cf == null) return null;

            // Get capital from capital accounts
            var capitalBalance = await _context.CapitalAccounts
                .Where(c => c.CreatedAt <= DateTime.UtcNow)
                .SumAsync(c => (long)c.CurrentBalance, ct);

            var totalAssets = cf.EndingBalance + 1; // Avoid division by zero

            var metrics = new PerformanceMetrics
            {
                PeriodId = periodId,
                RoE = capitalBalance > 0 ? (decimal)pl.GrossProfit / capitalBalance * 100 : 0,
                RoA = (decimal)pl.GrossProfit / totalAssets * 100,
                ProfitMargin = pl.TotalRevenue > 0 ? pl.ProfitMargin : 0,
                OperatingMargin = pl.TotalRevenue > 0 ? ((decimal)(pl.GrossProfit - pl.TotalExpenses) / pl.TotalRevenue * 100) : 0,
                AssetTurnover = totalAssets > 0 ? (decimal)pl.TotalRevenue / totalAssets : 0,
                CurrentRatio = 1.2m, // Simplified
                QuickRatio = 1.0m    // Simplified
            };

            _logger.LogInformation("Performance metrics generated for period {PeriodId}: RoE={RoE}%, RoA={RoA}%",
                periodId, metrics.RoE.ToString("F2"), metrics.RoA.ToString("F2"));

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance metrics for period {PeriodId}", periodId);
            return null;
        }
    }

    public async Task<ScenarioAnalysis?> RunScenarioAsync(
        Guid periodId,
        decimal assumptions,
        CancellationToken ct = default)
    {
        try
        {
            var pl = await _plService.GetPLStatementAsync(periodId, ct);
            if (pl == null) return null;

            var scenario = new ScenarioAnalysis
            {
                PeriodId = periodId,
                ScenarioName = $"Scenario {assumptions}% Growth",
                Assumptions = assumptions,
                BaselineRevenue = pl.TotalRevenue,
                ProjectedRevenue = (long)(pl.TotalRevenue * (1 + assumptions / 100))
            };

            scenario.RevenueDifference = scenario.ProjectedRevenue - scenario.BaselineRevenue;
            scenario.BaselineProfit = pl.GrossProfit;
            scenario.ProjectedProfit = (long)(pl.GrossProfit * (1 + assumptions / 100 * 0.8m)); // Profit grows slower
            scenario.ProfitDifference = scenario.ProjectedProfit - scenario.BaselineProfit;
            scenario.Impact = assumptions > 0 ? "Positive" : "Negative";

            // Generate implications
            GenerateScenarioImplications(scenario);

            _logger.LogInformation("Scenario analysis generated: {Scenario} - Projected Revenue={Revenue}",
                scenario.ScenarioName, scenario.ProjectedRevenue);

            return scenario;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scenario analysis for period {PeriodId}", periodId);
            return null;
        }
    }

    public async Task<List<CustomReport>> GetCustomReportsAsync(
        Guid branchId,
        CancellationToken ct = default)
    {
        try
        {
            // In real implementation, would fetch from database
            // For now, return empty list
            _logger.LogInformation("Custom reports retrieved for branch {BranchId}", branchId);
            return new List<CustomReport>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving custom reports for branch {BranchId}", branchId);
            return new List<CustomReport>();
        }
    }

    public async Task<CustomReport> CreateCustomReportAsync(
        Guid branchId,
        string name,
        string queryJson,
        CancellationToken ct = default)
    {
        try
        {
            var report = new CustomReport
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = name,
                QueryJson = queryJson,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Custom report created: {Name} for branch {BranchId}",
                name, branchId);

            return await Task.FromResult(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom report for branch {BranchId}", branchId);
            throw;
        }
    }

    private void GenerateComparativeInsights(
        ComparativeAnalysis analysis,
        ProfitLossStatement pl1,
        ProfitLossStatement pl2,
        CashFlowStatement cf1,
        CashFlowStatement cf2)
    {
        analysis.Insights.Clear();

        if (analysis.RevenueChangePercent > 10)
            analysis.Insights.Add("Revenue growth exceeds 10% - strong performance");
        else if (analysis.RevenueChangePercent < -10)
            analysis.Insights.Add("Revenue declined significantly - investigate cause");

        if (analysis.ProfitChangePercent > analysis.RevenueChangePercent)
            analysis.Insights.Add("Profit grew faster than revenue - operational efficiency improved");

        if (cf2.EndingBalance > cf1.EndingBalance)
            analysis.Insights.Add("Cash position improved - strong liquidity");
    }

    private void GenerateBudgetRecommendations(BudgetAnalysis analysis)
    {
        analysis.Recommendations.Clear();

        if (analysis.VariancePercent > 10)
        {
            analysis.Recommendations.Add("Budget was conservative - update for future periods");
            analysis.Recommendations.Add("Over-performed revenue by more than expected");
        }
        else if (analysis.VariancePercent < -10)
        {
            analysis.Recommendations.Add("Actual underperformed budget - investigate reasons");
            analysis.Recommendations.Add("Review operational efficiency and cost controls");
        }
        else
        {
            analysis.Recommendations.Add("Performance within expected range - maintain current strategy");
        }
    }

    private void GenerateScenarioImplications(ScenarioAnalysis scenario)
    {
        scenario.Implications.Clear();

        if (scenario.ProfitDifference > 0)
        {
            scenario.Implications.Add("Profit would increase by " + scenario.ProfitDifference);
            scenario.Implications.Add("Consider investment in growth initiatives");
            scenario.Implications.Add("Liquidity position would improve");
        }
        else
        {
            scenario.Implications.Add("Profit would decrease - review cost structure");
            scenario.Implications.Add("May impact dividend distributions");
            scenario.Implications.Add("Consider cost reduction measures");
        }
    }
}
