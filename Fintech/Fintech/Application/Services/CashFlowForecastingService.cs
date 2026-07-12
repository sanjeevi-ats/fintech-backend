using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface ICashFlowForecastingService
{
    Task<CashFlowForecast?> GenerateForecastAsync(Guid periodId, int monthsAhead = 3, Guid? branchId = null, CancellationToken ct = default);
    Task<List<CashFlowForecast>> GetForecastsAsync(Guid periodId, CancellationToken ct = default);
    Task<(long min, long max, long expected)> AnalyzeTrendsAsync(Guid periodId, int monthsHistory = 12, CancellationToken ct = default);
    Task<CashFlowForecast> UpdateForecastAsync(Guid forecastId, long projectedAmount, int confidenceLevel, CancellationToken ct = default);
}

public class CashFlowForecastingService : ICashFlowForecastingService
{
    private readonly FinVedaDbContext _context;
    private readonly ICashFlowService _cashFlowService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CashFlowForecastingService> _logger;

    public CashFlowForecastingService(
        FinVedaDbContext context,
        ICashFlowService cashFlowService,
        ITenantService tenantService,
        ILogger<CashFlowForecastingService> logger)
    {
        _context = context;
        _cashFlowService = cashFlowService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Generate cash flow forecast for specified months ahead
    /// </summary>
    public async Task<CashFlowForecast?> GenerateForecastAsync(
        Guid periodId,
        int monthsAhead = 3,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            // Get historical data for trend analysis
            var historicalStatements = await _context.CashFlowStatements
                .Where(c => c.BranchId == branchId && !c.IsDeleted)
                .OrderByDescending(c => c.StatementDate)
                .Take(12)
                .ToListAsync(ct);

            if (historicalStatements.Count == 0)
            {
                _logger.LogWarning("No historical data available for forecasting");
                return null;
            }

            // Calculate average monthly CF
            double avgNetCF = historicalStatements.Average(c => c.NetCashFlow);

            // Calculate projected total
            long projectedTotal = (long)(avgNetCF * monthsAhead);

            // Determine confidence level (0-100) based on data variance
            int confidenceLevel = CalculateConfidenceLevel(historicalStatements);

            // Create forecast
            var forecast = new CashFlowForecast
            {
                Id = Guid.NewGuid(),
                PeriodId = periodId,
                BranchId = branchId,
                ForecastPeriod = DateTime.UtcNow.AddMonths(monthsAhead),
                ProjectedCashFlow = projectedTotal,
                ConfidenceLevel = confidenceLevel,
                Status = (int)CashFlowForecastStatus.Generated,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashFlowForecasts.Add(forecast);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Cash flow forecast generated for period {PeriodId}, months={MonthsAhead}, confidence={Confidence}", 
                periodId, monthsAhead, confidenceLevel);
            
            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow forecast for period {PeriodId}", periodId);
            return null;
        }
    }

    /// <summary>
    /// Get all forecasts for a period
    /// </summary>
    public async Task<List<CashFlowForecast>> GetForecastsAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var forecasts = await _context.CashFlowForecasts
                .Where(f => f.PeriodId == periodId && !f.IsDeleted)
                .OrderByDescending(f => f.ForecastPeriod)
                .ToListAsync(ct);

            return forecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash flow forecasts for period {PeriodId}", periodId);
            return new List<CashFlowForecast>();
        }
    }

    /// <summary>
    /// Analyze cash flow trends (min, max, expected values)
    /// </summary>
    public async Task<(long min, long max, long expected)> AnalyzeTrendsAsync(
        Guid periodId,
        int monthsHistory = 12,
        CancellationToken ct = default)
    {
        try
        {
            var branchId = _tenantService.BranchId;

            // Get historical statements
            var statements = await _context.CashFlowStatements
                .Where(c => c.BranchId == branchId && !c.IsDeleted)
                .OrderByDescending(c => c.StatementDate)
                .Take(monthsHistory)
                .ToListAsync(ct);

            if (statements.Count == 0)
                return (0, 0, 0);

            var netCFs = statements.Select(c => c.NetCashFlow).ToList();

            long minCF = netCFs.Min();
            long maxCF = netCFs.Max();
            long expectedCF = (long)netCFs.Average();

            _logger.LogInformation("Cash flow trends analyzed: Min={Min}, Max={Max}, Expected={Expected}", minCF, maxCF, expectedCF);
            
            return (minCF, maxCF, expectedCF);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing cash flow trends for period {PeriodId}", periodId);
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Update forecast with new projections
    /// </summary>
    public async Task<CashFlowForecast> UpdateForecastAsync(
        Guid forecastId,
        long projectedAmount,
        int confidenceLevel,
        CancellationToken ct = default)
    {
        try
        {
            var forecast = await _context.CashFlowForecasts
                .FirstOrDefaultAsync(f => f.Id == forecastId, ct);

            if (forecast == null)
                throw new InvalidOperationException($"Forecast {forecastId} not found");

            forecast.ProjectedCashFlow = projectedAmount;
            forecast.ConfidenceLevel = confidenceLevel;
            forecast.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Forecast {ForecastId} updated: Amount={Amount}, Confidence={Confidence}", 
                forecastId, projectedAmount, confidenceLevel);
            
            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forecast {ForecastId}", forecastId);
            throw;
        }
    }

    /// <summary>
    /// Calculate confidence level based on historical variance
    /// </summary>
    private int CalculateConfidenceLevel(List<CashFlowStatement> statements)
    {
        if (statements.Count < 2)
            return 5;

        var netCFs = statements.Select(s => (double)s.NetCashFlow).ToList();
        double mean = netCFs.Average();
        
        // Calculate coefficient of variation
        double variance = netCFs.Sum(x => Math.Pow(x - mean, 2)) / netCFs.Count;
        double stdDev = Math.Sqrt(variance);
        double cv = mean != 0 ? (stdDev / Math.Abs(mean)) : 0;

        // Convert CV to confidence level (1-10 scale)
        int confidence = Math.Max(1, Math.Min(10, (int)(10 - (cv * 10))));
        
        return confidence;
    }
}

