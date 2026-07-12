using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Fintech.Application.Services;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodController : ControllerBase
{
    private readonly MonthEndCloseService _closeService;
    private readonly AccrualService _accrualService;
    private readonly ProvisionService _provisionService;
    private readonly ProfitLossService _profitLossService;
    private readonly CashFlowService _cashFlowService;
    private readonly BankReconciliationService _bankService;
    private readonly LoanLedgerReconciliationService _loanService;
    private readonly CollectionReconciliationService _collectionService;
    private readonly IControllerFileLoggerService _logger;

    public PeriodController(
        MonthEndCloseService closeService,
        AccrualService accrualService,
        ProvisionService provisionService,
        ProfitLossService profitLossService,
        CashFlowService cashFlowService,
        BankReconciliationService bankService,
        LoanLedgerReconciliationService loanService,
        CollectionReconciliationService collectionService,
        IControllerFileLoggerService logger)
    {
        _closeService = closeService;
        _accrualService = accrualService;
        _provisionService = provisionService;
        _profitLossService = profitLossService;
        _cashFlowService = cashFlowService;
        _bankService = bankService;
        _loanService = loanService;
        _collectionService = collectionService;
        _logger = logger;
    }

    /// <summary>
    /// Close an accounting period - creates accruals, provisions, and marks period as closed
    /// </summary>
    [HttpPost("{periodId}/close")]
    public async Task<IActionResult> ClosePeriod(Guid periodId, [FromBody] string closedBy)
    {
        try
        {
            var result = await _closeService.CloseAccountingPeriodAsync(periodId, closedBy ?? "system");
            
            if (!result.Success)
            {
                await _logger.LogWarningAsync("PeriodController", "ClosePeriod", $"Close failed: {result.Message}");
                return BadRequest(new { success = false, message = result.Message });
            }

            await _logger.LogInfoAsync("PeriodController", "ClosePeriod", null, null, new { periodId, closedBy });
            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ClosePeriod", ex);
            return StatusCode(500, new { success = false, message = "Failed to close period" });
        }
    }

    /// <summary>
    /// Get close status for a period
    /// </summary>
    [HttpGet("{periodId}/status")]
    public async Task<IActionResult> GetStatus(Guid periodId)
    {
        try
        {
            var status = await _closeService.GetCloseStatusAsync(periodId);
            
            if (status == null)
            {
                await _logger.LogWarningAsync("PeriodController", "GetStatus", "Period not found");
                return NotFound(new { success = false, message = "Period not found" });
            }

            await _logger.LogInfoAsync("PeriodController", "GetStatus", null, null, new { periodId });
            return Ok(status);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "GetStatus", ex);
            return StatusCode(500, new { success = false, message = "Failed to get status" });
        }
    }

    /// <summary>
    /// Validate if a period is ready to be closed
    /// </summary>
    [HttpPost("{periodId}/validate")]
    public async Task<IActionResult> ValidateCloseReadiness(Guid periodId)
    {
        try
        {
            var result = await _closeService.ValidateCloseReadinessAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "ValidateCloseReadiness", null, null, new { periodId, isReady = result.IsReady });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ValidateCloseReadiness", ex);
            return StatusCode(500, new { success = false, message = "Validation failed" });
        }
    }

    /// <summary>
    /// Reverse a closed period - reverts accruals and provisions
    /// </summary>
    [HttpPost("{periodId}/reverse")]
    public async Task<IActionResult> ReversePeriod(Guid periodId, [FromBody] string reversedBy)
    {
        try
        {
            var success = await _closeService.ReverseCloseAsync(periodId, reversedBy ?? "system");
            
            if (!success)
            {
                await _logger.LogWarningAsync("PeriodController", "ReversePeriod", "Reversal failed");
                return BadRequest(new { success = false, message = "Period reversal failed" });
            }

            await _logger.LogInfoAsync("PeriodController", "ReversePeriod", null, null, new { periodId, reversedBy });
            return Ok(new { success = true, message = "Period reversed successfully" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ReversePeriod", ex);
            return StatusCode(500, new { success = false, message = "Failed to reverse period" });
        }
    }

    /// <summary>
    /// Get all accrual entries for a period
    /// </summary>
    [HttpGet("{periodId}/accruals")]
    public async Task<IActionResult> GetAccruals(Guid periodId)
    {
        try
        {
            var accruals = await _accrualService.GetAccrualsAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "GetAccruals", null, null, new { periodId, count = accruals.Count });
            return Ok(accruals);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "GetAccruals", ex);
            return StatusCode(500, new { success = false, message = "Failed to get accruals" });
        }
    }

    /// <summary>
    /// Get all provision entries for a period
    /// </summary>
    [HttpGet("{periodId}/provisions")]
    public async Task<IActionResult> GetProvisions(Guid periodId)
    {
        try
        {
            var provisions = await _provisionService.GetProvisionsAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "GetProvisions", null, null, new { periodId, count = provisions.Count });
            return Ok(provisions);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "GetProvisions", ex);
            return StatusCode(500, new { success = false, message = "Failed to get provisions" });
        }
    }

    /// <summary>
    /// Generate Profit & Loss statement for a period
    /// </summary>
    [HttpGet("{periodId}/profit-loss")]
    public async Task<IActionResult> GetProfitLoss(Guid periodId)
    {
        try
        {
            var pl = await _profitLossService.GeneratePLStatementAsync(periodId);
            
            if (pl == null)
            {
                await _logger.LogWarningAsync("PeriodController", "GetProfitLoss", "Period not found");
                return NotFound(new { success = false, message = "Period not found" });
            }

            await _logger.LogInfoAsync("PeriodController", "GetProfitLoss", null, null, new { periodId, netProfit = pl.NetProfit });
            return Ok(pl);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "GetProfitLoss", ex);
            return StatusCode(500, new { success = false, message = "Failed to generate P&L" });
        }
    }

    /// <summary>
    /// Generate Cash Flow statement for a period
    /// </summary>
    [HttpGet("{periodId}/cash-flow")]
    public async Task<IActionResult> GetCashFlow(Guid periodId)
    {
        try
        {
            var cf = await _cashFlowService.GenerateCashFlowStatementAsync(periodId, null, CancellationToken.None);
            
            if (cf == null)
            {
                await _logger.LogWarningAsync("PeriodController", "GetCashFlow", "Period not found");
                return NotFound(new { success = false, message = "Period not found" });
            }

            await _logger.LogInfoAsync("PeriodController", "GetCashFlow", null, null, new { periodId, netChange = cf.NetCashFlow });
            return Ok(cf);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "GetCashFlow", ex);
            return StatusCode(500, new { success = false, message = "Failed to generate cash flow" });
        }
    }

    /// <summary>
    /// Reconcile bank balances for a period
    /// </summary>
    [HttpPost("{periodId}/reconcile/bank")]
    public async Task<IActionResult> ReconcileBank(Guid periodId)
    {
        try
        {
            var result = await _bankService.ReconcileBankAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "ReconcileBank", null, null, new { periodId, isReconciled = result.IsReconciled });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ReconcileBank", ex);
            return StatusCode(500, new { success = false, message = "Bank reconciliation failed" });
        }
    }

    /// <summary>
    /// Reconcile loan ledger for a period
    /// </summary>
    [HttpPost("{periodId}/reconcile/loans")]
    public async Task<IActionResult> ReconcileLoans(Guid periodId)
    {
        try
        {
            var result = await _loanService.ReconcileLoansAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "ReconcileLoans", null, null, new { periodId, reconciled = result.Reconciled });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ReconcileLoans", ex);
            return StatusCode(500, new { success = false, message = "Loan reconciliation failed" });
        }
    }

    /// <summary>
    /// Reconcile collections for a period
    /// </summary>
    [HttpPost("{periodId}/reconcile/collections")]
    public async Task<IActionResult> ReconcileCollections(Guid periodId)
    {
        try
        {
            var result = await _collectionService.ReconcileCollectionsAsync(periodId);
            
            await _logger.LogInfoAsync("PeriodController", "ReconcileCollections", null, null, new { periodId, reconciled = result.Reconciled });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PeriodController", "ReconcileCollections", ex);
            return StatusCode(500, new { success = false, message = "Collection reconciliation failed" });
        }
    }
}
