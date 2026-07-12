using Fintech.Application.Services;
using Fintech.Application.DTOs;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Fintech.Infrastructure.Persistence;
using Fintech.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Controllers;

[AutoLog]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
public class LedgerController : BaseApiController
{
    private readonly IAccountingService _accountingService;
    private readonly ILedgerService _ledgerService;
    private readonly IReconciliationService _reconciliationService;
    private readonly FinVedaDbContext _dbContext;
    private readonly ILogger<LedgerController> _logger;
    private readonly ITenantService _tenantService;

    public LedgerController(
        IAccountingService accountingService,
        ILedgerService ledgerService,
        IReconciliationService reconciliationService,
        FinVedaDbContext dbContext,
        ILogger<LedgerController> logger,
        ITenantService tenantService)
    {
        _accountingService = accountingService;
        _ledgerService = ledgerService;
        _reconciliationService = reconciliationService;
        _dbContext = dbContext;
        _logger = logger;
        _tenantService = tenantService;
    }

    /// <summary>
    /// GET /api/v1/ledger/accounts
    /// Returns all GL accounts with current balances and totals
    /// Query params: limit (50), offset (0)
    /// </summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAllAccounts(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var branchId = BranchId;
            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            // Validate pagination
            if (limit <= 0 || limit > 500)
                limit = 50;
            if (offset < 0)
                offset = 0;

            var total = await _dbContext.Accounts
                .Where(a => a.BranchId == branchId)
                .CountAsync();

            var accounts = await _dbContext.Accounts
                .Where(a => a.BranchId == branchId)
                .OrderBy(a => a.AccountCode)
                .Skip(offset)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.AccountCode,
                    a.Name,
                    Balance = _dbContext.LedgerBalances
                        .Where(l => l.GLAccountId == a.Id)
                        .Select(l => l.Balance)
                        .FirstOrDefault(),
                    TotalDebits = _dbContext.LedgerBalances
                        .Where(l => l.GLAccountId == a.Id)
                        .Select(l => l.TotalDebits)
                        .FirstOrDefault(),
                    TotalCredits = _dbContext.LedgerBalances
                        .Where(l => l.GLAccountId == a.Id)
                        .Select(l => l.TotalCredits)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = accounts,
                Pagination = new { Total = total, Limit = limit, Offset = offset, Returned = accounts.Count }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAccounts");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving accounts.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/ledger/accounts/{code}/balance
    /// Returns current balance for specific account and optional historical balances
    /// Route params: code (GL account code)
    /// Query params: fromDate, toDate (optional for historical data)
    /// </summary>
    [HttpGet("accounts/{code}/balance")]
    public async Task<IActionResult> GetAccountBalance(
        string code,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var branchId = BranchId;
            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { Success = false, Message = "Account code is required" });

            var account = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.BranchId == branchId && a.AccountCode == code);

            if (account == null)
                return NotFound(new { Success = false, Message = $"GL account {code} not found" });

            var currentBalance = await _ledgerService.GetAccountBalanceAsync(code);

            // If date range provided, include historical balances
            if (fromDate.HasValue && toDate.HasValue)
            {
                var history = await _dbContext.LedgerHistories
                    .Where(h => h.GLAccountCode == code && h.BranchId == branchId &&
                           h.Date >= fromDate && h.Date <= toDate)
                    .OrderBy(h => h.Date)
                    .Select(h => new
                    {
                        h.Date,
                        h.Balance,
                        h.Debits,
                        h.Credits
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        AccountCode = code,
                        AccountName = account.Name,
                        CurrentBalance = currentBalance,
                        HistoricalBalances = history
                    }
                });
            }

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    AccountCode = code,
                    AccountName = account.Name,
                    CurrentBalance = currentBalance
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAccountBalance");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving account balance.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/ledger/trial-balance
    /// Returns trial balance from LedgerService
    /// Query params: branchId (optional), asOfDate
    /// </summary>
    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? asOfDate)
    {
        try
        {
            var branch = branchId ?? BranchId;
            if (branch == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            // Call LedgerService to get trial balance
            var result = await _ledgerService.GetTrialBalanceAsync(asOfDate);

            return Ok(new
            {
                Success = true,
                Data = result,
                VerificationStatus = new
                {
                    IsBalanced = result.IsBalanced,
                    TotalDebits = result.TotalDebits,
                    TotalCredits = result.TotalCredits,
                    Difference = result.TotalDebits - result.TotalCredits
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTrialBalance");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving trial balance.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/ledger/reconciliation
    /// Returns reconciliation status and discrepancies (Admin only)
    /// Query params: branchId (optional), startDate, endDate
    /// </summary>
    [HttpGet("reconciliation")]
    public async Task<IActionResult> GetReconciliation(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var branch = branchId ?? BranchId;
            if (branch == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var report = await _reconciliationService.GenerateReconciliationReportAsync(branch, start, end);

            return Ok(new
            {
                Success = true,
                Data = report
            });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReconciliation");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving reconciliation data.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Legacy endpoint - kept for backward compatibility
    /// </summary>
    [HttpGet("pnl")]
    public async Task<IActionResult> GetPnl([FromQuery] System.DateTime start, [FromQuery] System.DateTime end)
    {
        try
        {
            // Ensure dates are in UTC
            var startUtc = start.Kind == System.DateTimeKind.Utc ? start : System.DateTime.SpecifyKind(start, System.DateTimeKind.Utc);
            var endUtc = end.Kind == System.DateTimeKind.Utc ? end : System.DateTime.SpecifyKind(end, System.DateTimeKind.Utc);
            
            var result = await _accountingService.GetProfitAndLossAsync(startUtc, endUtc);
            
            return Ok(new { profitAndLoss = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPnl");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving profit and loss statement.",
                Error = ex.Message
            });
        }
    }
}
