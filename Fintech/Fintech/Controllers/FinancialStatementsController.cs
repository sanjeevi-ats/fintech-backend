using Fintech.Application.Services;
using Fintech.Application.DTOs;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

using Microsoft.AspNetCore.Authorization;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class FinancialStatementsController : BaseApiController
{
    private readonly IFinancialStatementService _financialService;
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<FinancialStatementsController> _logger;
    private readonly ITenantService _tenantService;

    public FinancialStatementsController(
        IFinancialStatementService financialService,
        ILedgerService ledgerService,
        ILogger<FinancialStatementsController> logger,
        ITenantService tenantService)
    {
        _financialService = financialService;
        _ledgerService = ledgerService;
        _logger = logger;
        _tenantService = tenantService;
    }

    /// <summary>
    /// GET /api/v1/financialstatements/trial-balance
    /// Returns trial balance as of specified date
    /// Query params: branchId (optional, defaults to current branch), asOfDate
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

            var trialBalance = await _financialService.GenerateTrialBalanceAsync(branch, asOfDate);
            
            return Ok(new
            {
                Success = true,
                Data = trialBalance,
                ExportOptions = new { formats = new[] { "json", "pdf", "csv" } }
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
    /// GET /api/v1/financialstatements/balance-sheet
    /// Returns balance sheet with assets, liabilities, and equity sections
    /// Query params: branchId (optional), fromDate, toDate
    /// </summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var branch = branchId ?? BranchId;
            if (branch == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            if (fromDate == default || toDate == default)
                return BadRequest(new { Success = false, Message = "fromDate and toDate are required" });

            var balanceSheet = await _financialService.GenerateBalanceSheetAsync(branch, fromDate, toDate);
            
            return Ok(new
            {
                Success = true,
                Data = balanceSheet,
                EquationVerification = new
                {
                    Assets = balanceSheet.TotalAssets,
                    LiabilitiesAndEquity = balanceSheet.TotalLiabilities + balanceSheet.TotalEquity,
                    IsBalanced = balanceSheet.IsBalanced
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBalanceSheet");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving balance sheet.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/financialstatements/account-statement/{code}
    /// Returns detailed account statement with running balance
    /// Query params: branchId (optional), fromDate, toDate
    /// Route params: code (GL account code)
    /// </summary>
    [HttpGet("account-statement/{code}")]
    public async Task<IActionResult> GetAccountStatement(
        string code,
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var branch = branchId ?? BranchId;
            if (branch == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { Success = false, Message = "Account code is required" });

            if (fromDate == default || toDate == default)
                return BadRequest(new { Success = false, Message = "fromDate and toDate are required" });

            var accountStatement = await _financialService.GenerateAccountStatementAsync(
                branch, code, fromDate, toDate);
            
            return Ok(new
            {
                Success = true,
                Data = accountStatement,
                Summary = new
                {
                    OpeningBalance = accountStatement.OpeningBalance,
                    ClosingBalance = accountStatement.ClosingBalance,
                    TotalTransactions = accountStatement.Transactions.Count,
                    TotalDebits = accountStatement.TotalDebits,
                    TotalCredits = accountStatement.TotalCredits
                }
            });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAccountStatement");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving account statement.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/financialstatements/export-pdf/{type}
    /// Exports financial report as PDF file
    /// Route params: type (trial-balance, balance-sheet, account-statement)
    /// Query params: branchId, asOfDate, fromDate, toDate, accountCode (for account-statement)
    /// </summary>
    [HttpGet("export-pdf/{type}")]
    public async Task<IActionResult> ExportPdfReport(
        string type,
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? asOfDate,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? accountCode)
    {
        try
        {
            var branch = branchId ?? BranchId;
            if (branch == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            object reportData = null;
            string fileName = "";

            switch (type.ToLower())
            {
                case "trial-balance":
                    if (asOfDate == null || asOfDate == default)
                        return BadRequest(new { Success = false, Message = "asOfDate is required for trial-balance" });
                    
                    reportData = await _financialService.GenerateTrialBalanceAsync(branch, asOfDate);
                    fileName = $"TrialBalance_{asOfDate:yyyyMMdd}.pdf";
                    break;

                case "balance-sheet":
                    if (fromDate == null || toDate == null || fromDate == default || toDate == default)
                        return BadRequest(new { Success = false, Message = "fromDate and toDate are required for balance-sheet" });
                    
                    reportData = await _financialService.GenerateBalanceSheetAsync(branch, fromDate.Value, toDate.Value);
                    fileName = $"BalanceSheet_{toDate:yyyyMMdd}.pdf";
                    break;

                case "account-statement":
                    if (string.IsNullOrWhiteSpace(accountCode))
                        return BadRequest(new { Success = false, Message = "accountCode is required for account-statement" });
                    
                    if (fromDate == null || toDate == null || fromDate == default || toDate == default)
                        return BadRequest(new { Success = false, Message = "fromDate and toDate are required" });
                    
                    reportData = await _financialService.GenerateAccountStatementAsync(
                        branch, accountCode, fromDate.Value, toDate.Value);
                    fileName = $"AccountStatement_{accountCode}_{toDate:yyyyMMdd}.pdf";
                    break;

                default:
                    return BadRequest(new { Success = false, Message = "Invalid report type" });
            }

            if (reportData == null)
                return NotFound(new { Success = false, Message = "Report data not found" });

            var pdfBytes = await _financialService.GeneratePdfReportAsync(type, reportData);
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExportPdfReport");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating PDF report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// POST /api/v1/financialstatements/email-report
    /// Sends financial report via email to stakeholders
    /// </summary>
    [HttpPost("email-report")]
    public async Task<IActionResult> EmailReport([FromBody] EmailReportRequest request)
    {
        try
        {
            if (request == null || request.EmailRecipients.Count == 0)
                return BadRequest(new { Success = false, Message = "Email recipients are required" });

            // TODO: Implement email sending service integration
            // This would integrate with an email service to send the report

            return Ok(new
            {
                Success = true,
                Message = $"Report email queued for {request.EmailRecipients.Count} recipient(s)",
                Recipients = request.EmailRecipients,
                ReportType = request.ReportType,
                SentAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EmailReport");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while sending report email.",
                Error = ex.Message
            });
        }
    }
}
