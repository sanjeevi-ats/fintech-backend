using Fintech.Application.Services;
using Fintech.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class DayEndController : BaseApiController
{
    private readonly IAccountingService _accountingService;
    private readonly ILogger<DayEndController> _logger;
    private readonly FinVedaDbContext _db;
    private readonly ITenantService _tenantService;

    public DayEndController(
        IAccountingService accountingService,
        ILogger<DayEndController> logger,
        FinVedaDbContext db,
        ITenantService tenantService)
    {
        _accountingService = accountingService;
        _logger = logger;
        _db = db;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Close the day end for a given date
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseDayEnd([FromQuery] DateTime? date, [FromQuery] long? verifiedCash)
    {
        try
        {
            var closeDate = date ?? DateTime.UtcNow;
            var cash = verifiedCash ?? 0;

            await _accountingService.CloseDayAsync(closeDate, cash);
            
            var responseMessage = $"Day {closeDate:yyyy-MM-dd} closed successfully";
            return Ok(new { success = true, message = responseMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CloseDayEnd");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while closing the day end.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get system cash balance for day-end reconciliation
    /// Returns sum of all cash/bank ledger accounts as systemCash
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var branchId = BranchId;

            // Get cash/bank account balances from ledger
            var cashAccounts = await _db.LedgerBalances
                .Where(l => l.BranchId == branchId)
                .Where(l => l.GLAccountCode.StartsWith("CASH") || 
                            l.GLAccountCode.StartsWith("BANK") ||
                            l.AccountName.ToLower().Contains("cash") ||
                            l.AccountName.ToLower().Contains("bank"))
                .ToListAsync();

            var systemCash = cashAccounts.Sum(a => a.Balance);

            // Get today's total collections
            var today = DateTime.UtcNow.Date;
            var todayCollections = await _db.Receipts
                .Where(r => r.BranchId == branchId && r.CapturedAt >= today)
                .SumAsync(r => r.AmountPaid);

            return Ok(new
            {
                success = true,
                systemCash,
                expectedCash = systemCash,
                todayCollections,
                date = today.ToString("yyyy-MM-dd"),
                message = cashAccounts.Count == 0 
                    ? "No cash/bank accounts configured in ledger" 
                    : $"Balance from {cashAccounts.Count} account(s)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBalance");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the day-end balance.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get day-end record by business code
    /// </summary>
    /// <param name="code">Day end code (e.g., DE0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var dayEnd = await _accountingService.GetByCodeAsync(code);
            if (dayEnd == null)
                return NotFound(new { message = $"Day end record with code {code} not found" });
            
            return Ok(dayEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the day end record by code.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get today's day-end status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var branchId = BranchId;
            var today = DateTime.UtcNow.Date;

            var todayDayEnd = await _db.DayEnds
                .Where(d => d.BranchId == branchId && d.Date == today)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                success = true,
                isClosed = todayDayEnd?.IsClosed ?? false,
                date = today.ToString("yyyy-MM-dd"),
                dayEndCode = todayDayEnd?.DayEndCode,
                totalCollected = todayDayEnd?.TotalCollected ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStatus");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving day-end status.",
                Error = ex.Message
            });
        }
    }
}
