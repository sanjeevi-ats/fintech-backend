using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface ILiquidityAnalysisService
{
    Task<(long currentAssets, long currentLiabilities, decimal ratio)> GetLiquidityPositionAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<(decimal operatingRatio, decimal cashRatio, decimal quickRatio)> CalculateCoverageRatiosAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<(string riskLevel, string recommendation, int riskScore)> AssessRiskAsync(Guid periodId, Guid? branchId = null, CancellationToken ct = default);
    Task<Dictionary<string, long>> GetLiquidityBreakdownAsync(Guid periodId, CancellationToken ct = default);
}

public class LiquidityAnalysisService : ILiquidityAnalysisService
{
    private readonly FinVedaDbContext _context;
    private readonly ICashFlowService _cashFlowService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<LiquidityAnalysisService> _logger;

    public LiquidityAnalysisService(
        FinVedaDbContext context,
        ICashFlowService cashFlowService,
        ITenantService tenantService,
        ILogger<LiquidityAnalysisService> logger)
    {
        _context = context;
        _cashFlowService = cashFlowService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get current liquidity position (current assets vs current liabilities)
    /// </summary>
    public async Task<(long currentAssets, long currentLiabilities, decimal ratio)> GetLiquidityPositionAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            // Get cash from CF statement
            var cfStatement = await _context.CashFlowStatements
                .FirstOrDefaultAsync(c => c.PeriodId == periodId && c.BranchId == branchId, ct);

            long cash = cfStatement?.EndingBalance ?? 0;

            // Get receivables (loans outstanding)
            var receivables = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .SumAsync(l => (long)l.Principal, ct);

            long currentAssets = cash + receivables;

            // Get current liabilities (credit journal lines)
            var liabilities = await _context.JournalLines
                .Where(jl => jl.Type == "Credit")
                .SumAsync(jl => (long)jl.Amount, ct);

            long currentLiabilities = Math.Max(liabilities, 1); // Avoid division by zero
            decimal ratio = (decimal)currentAssets / currentLiabilities;

            _logger.LogInformation("Liquidity position calculated: Assets={Assets}, Liabilities={Liabilities}, Ratio={Ratio}", 
                currentAssets, currentLiabilities, ratio);
            
            return (currentAssets, currentLiabilities, ratio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating liquidity position for period {PeriodId}", periodId);
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Calculate liquidity coverage ratios
    /// </summary>
    public async Task<(decimal operatingRatio, decimal cashRatio, decimal quickRatio)> CalculateCoverageRatiosAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            // Get period
            var period = await _context.AccountingPeriods
                .FirstOrDefaultAsync(p => p.Id == periodId, ct);

            if (period == null)
                return (0, 0, 0);

            // Get cash flow data
            var cfStatement = await _context.CashFlowStatements
                .FirstOrDefaultAsync(c => c.PeriodId == periodId && c.BranchId == branchId, ct);

            if (cfStatement == null)
                return (0, 0, 0);

            long cash = cfStatement.EndingBalance;
            long operatingCF = cfStatement.OperatingCashFlow;

            // Get operating expenses from journal lines
            var operatingExpenses = await _context.JournalLines
                .Include(jl => jl.JournalEntry)
                .Where(jl => jl.JournalEntry.BranchId == branchId
                    && jl.JournalEntry.Date >= period.StartDate
                    && jl.JournalEntry.Date <= period.EndDate
                    && jl.JournalEntry.Description.Contains("Operating", StringComparison.OrdinalIgnoreCase)
                    && jl.Type == "Debit")
                .SumAsync(jl => (long)jl.Amount, ct);

            operatingExpenses = Math.Max(operatingExpenses, 1); // Avoid division by zero

            // Get current liabilities (credit journal lines)
            var currentLiabilities = await _context.JournalLines
                .Where(jl => jl.Type == "Credit")
                .SumAsync(jl => (long)jl.Amount, ct);

            currentLiabilities = Math.Max(currentLiabilities, 1);

            // Calculate ratios
            // Operating Cash Flow Ratio = Operating CF / Current Liabilities
            decimal operatingRatio = (decimal)operatingCF / currentLiabilities;

            // Cash Ratio = Cash / Current Liabilities
            decimal cashRatio = (decimal)cash / currentLiabilities;

            // Quick Ratio = (Current Assets - Inventory) / Current Liabilities
            // For microfinance, approximated as (Cash + Receivables) / Current Liabilities
            var receivables = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .SumAsync(l => (long)l.Principal, ct);

            decimal quickRatio = (decimal)(cash + receivables) / currentLiabilities;

            _logger.LogInformation("Coverage ratios calculated: Operating={Operating}, Cash={Cash}, Quick={Quick}", 
                operatingRatio, cashRatio, quickRatio);
            
            return (operatingRatio, cashRatio, quickRatio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating coverage ratios for period {PeriodId}", periodId);
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Assess liquidity risk level
    /// </summary>
    public async Task<(string riskLevel, string recommendation, int riskScore)> AssessRiskAsync(
        Guid periodId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            branchId ??= _tenantService.BranchId;

            // Get liquidity position
            var (currentAssets, currentLiabilities, ratio) = await GetLiquidityPositionAsync(periodId, branchId, ct);

            // Get coverage ratios
            var (operatingRatio, cashRatio, quickRatio) = await CalculateCoverageRatiosAsync(periodId, branchId, ct);

            // Get cash flow
            var cfStatement = await _context.CashFlowStatements
                .FirstOrDefaultAsync(c => c.PeriodId == periodId && c.BranchId == branchId, ct);

            int riskScore = 0;
            string riskLevel = "Low";
            string recommendation = "Maintain current liquidity management practices.";

            // Risk assessment logic
            if (ratio < 0.5m)
            {
                riskScore += 40;
                riskLevel = "Critical";
                recommendation = "URGENT: Improve current asset position. Increase collections or reduce liabilities.";
            }
            else if (ratio < 1.0m)
            {
                riskScore += 25;
                riskLevel = "High";
                recommendation = "Improve liquidity position. Accelerate collections and optimize expenses.";
            }
            else if (ratio < 1.5m)
            {
                riskScore += 10;
                riskLevel = "Moderate";
                recommendation = "Monitor liquidity trends closely. Consider diversifying cash sources.";
            }

            // Operating cash flow assessment
            if (cfStatement != null && cfStatement.OperatingCashFlow < 0)
            {
                riskScore += 30;
                recommendation = $"{recommendation} Negative operating CF detected - urgent action required.";
            }
            else if (cfStatement != null && cfStatement.OperatingCashFlow < currentLiabilities / 3)
            {
                riskScore += 15;
                recommendation = $"{recommendation} Operating CF is insufficient to cover liabilities.";
            }

            // Quick ratio assessment
            if (quickRatio < 0.5m)
            {
                riskScore += 20;
                recommendation = $"{recommendation} Quick ratio is critically low.";
            }

            // Cap risk score at 100
            riskScore = Math.Min(riskScore, 100);

            // Determine final risk level if not already set
            if (riskScore > 70)
                riskLevel = "Critical";
            else if (riskScore > 50)
                riskLevel = "High";
            else if (riskScore > 25)
                riskLevel = "Moderate";
            else
                riskLevel = "Low";

            _logger.LogInformation("Liquidity risk assessed: Level={RiskLevel}, Score={RiskScore}", riskLevel, riskScore);
            
            return (riskLevel, recommendation, riskScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing liquidity risk for period {PeriodId}", periodId);
            return ("Unknown", "Unable to assess risk at this time.", 0);
        }
    }

    /// <summary>
    /// Get breakdown of liquidity by component
    /// </summary>
    public async Task<Dictionary<string, long>> GetLiquidityBreakdownAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            var branchId = _tenantService.BranchId;

            var breakdown = new Dictionary<string, long>();

            // Cash on hand
            var cfStatement = await _context.CashFlowStatements
                .FirstOrDefaultAsync(c => c.PeriodId == periodId && c.BranchId == branchId, ct);

            breakdown["Cash"] = cfStatement?.EndingBalance ?? 0;

            // Receivables
            var receivables = await _context.LoanCases
                .Where(l => l.BranchId == branchId)
                .SumAsync(l => (long)l.Principal, ct);

            breakdown["Receivables"] = receivables;

            // Bank balance from receipts
            var bankBalance = await _context.Receipts
                .Where(r => r.BranchId == branchId)
                .SumAsync(r => (long)r.AmountPaid, ct);

            breakdown["Bank_Balance"] = bankBalance;

            // Get current liabilities (credit journal lines)
            var liabilities = await _context.JournalLines
                .Where(jl => jl.Type == "Credit")
                .SumAsync(jl => (long)jl.Amount, ct);

            breakdown["Current_Liabilities"] = liabilities;

            breakdown["Total_Assets"] = breakdown["Cash"] + breakdown["Receivables"] + breakdown["Bank_Balance"];

            _logger.LogInformation("Liquidity breakdown retrieved for period {PeriodId}", periodId);
            
            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving liquidity breakdown for period {PeriodId}", periodId);
            return new Dictionary<string, long>();
        }
    }
}
