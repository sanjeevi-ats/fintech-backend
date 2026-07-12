using Fintech.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fintech.Infrastructure.Logging;
using Fintech.Core.Domain;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class CapitalAccountsController : BaseApiController
{
    private readonly ICapitalAccountService _capitalService;
    private readonly ILogger<CapitalAccountsController> _logger;

    public CapitalAccountsController(ICapitalAccountService capitalService, ILogger<CapitalAccountsController> logger)
    {
        _capitalService = capitalService;
        _logger = logger;
    }

    [HttpPost("investment")]
    public async Task<IActionResult> AddInvestment([FromBody] InvestmentRequest request)
    {
        try
        {
            var createdBy = UserId;
            var transaction = await _capitalService.AddInvestmentAsync(request.PartnerId, request.Amount, request.PaymentMode, request.Remarks, createdBy);
            return Ok(new { success = true, message = "Investment added successfully", transaction });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddInvestment");
            return StatusCode(500, new { Success = false, Message = "An unexpected error occurred while adding investment.", Error = ex.Message });
        }
    }


    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var account = await _capitalService.GetByCodeAsync(code);
            if (account == null)
                return NotFound(new { message = $"Capital account with code {code} not found" });
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            return StatusCode(500, new { Success = false, Message = "An unexpected error occurred while retrieving the capital account by code.", Error = ex.Message });
        }
    }

    [HttpPost("withdrawal")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest request)
    {
        try
        {
            var createdBy = UserId;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isImmediate = (roleClaim == "super_admin" || roleClaim == "branch_manager" || roleClaim == "admin");

            var transaction = await _capitalService.WithdrawAsync(request.PartnerId, request.Amount, request.PaymentMode, request.Remarks, createdBy, isImmediate);
            
            if (isImmediate)
            {
                return Ok(new { success = true, message = "Withdrawal completed successfully", transaction });
            }
            else
            {
                return Ok(new { success = true, message = "Withdrawal request submitted for approval", transaction });
            }
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Withdraw");
            return StatusCode(500, new { Success = false, Message = "An unexpected error occurred while recording withdrawal.", Error = ex.Message });
        }
    }

    [HttpGet("summary/{partnerId}")]
    public async Task<ActionResult<PartnerCapitalSummaryDto>> GetSummary(Guid partnerId)
    {
        try
        {
            var summary = await _capitalService.GetSummaryAsync(partnerId);
            if (summary == null) return NotFound();
            summary.PartnerCode = summary.PartnerCode ?? string.Empty;
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSummary");
            return StatusCode(500, new { Success = false, Message = "An unexpected error occurred while retrieving capital account summary.", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all capital accounts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CapitalAccount>>> GetAllAccounts()
    {
        try
        {
            var accounts = await _capitalService.GetAllAccountsAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAccounts");
            return StatusCode(500, new { Success = false, Message = "Error retrieving capital accounts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get capital account by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CapitalAccount>> GetAccountById(Guid id)
    {
        try
        {
            var account = await _capitalService.GetAccountByIdAsync(id);
            if (account == null) return NotFound(new { message = "Capital account not found" });
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAccountById");
            return StatusCode(500, new { Success = false, Message = "Error retrieving capital account", Error = ex.Message });
        }
    }

    /// <summary>
    /// Create new capital account
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CapitalAccount>> CreateAccount([FromBody] CreateCapitalAccountRequest request)
    {
        try
        {
            var account = await _capitalService.CreateAccountAsync(request.PartnerId, request.OpeningBalance, UserId);
            return CreatedAtAction(nameof(GetAccountById), new { id = account.Id }, account);
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAccount");
            return StatusCode(500, new { Success = false, Message = "Error creating capital account", Error = ex.Message });
        }
    }

    /// <summary>
    /// Record capital transaction (contribution, withdrawal, distribution)
    /// </summary>
    [HttpPost("{accountId}/transactions")]
    public async Task<ActionResult<CapitalTransaction>> RecordTransaction(Guid accountId, [FromBody] RecordCapitalTransactionRequest request)
    {
        try
        {
            var transaction = await _capitalService.RecordTransactionAsync(
                accountId, 
                request.Type, 
                request.Amount, 
                request.TransactionDate, 
                request.Description, 
                request.ReferenceNumber, 
                UserId);
            return CreatedAtAction(nameof(GetTransaction), new { transactionId = transaction.Id }, transaction);
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RecordTransaction");
            return StatusCode(500, new { Success = false, Message = "Error recording transaction", Error = ex.Message });
        }
    }

    /// <summary>
    /// Approve a capital transaction
    /// </summary>
    [HttpPost("transactions/{transactionId}/approve")]
    public async Task<ActionResult<CapitalTransaction>> ApproveTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _capitalService.ApproveTransactionAsync(transactionId, UserId);
            return Ok(transaction);
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveTransaction");
            return StatusCode(500, new { Success = false, Message = "Error approving transaction", Error = ex.Message });
        }
    }

    /// <summary>
    /// Reject a capital transaction
    /// </summary>
    [HttpPost("transactions/{transactionId}/reject")]
    public async Task<ActionResult<CapitalTransaction>> RejectTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _capitalService.RejectTransactionAsync(transactionId, UserId);
            return Ok(transaction);
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectTransaction");
            return StatusCode(500, new { Success = false, Message = "Error rejecting transaction", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get transactions for an account
    /// </summary>
    [HttpGet("{accountId}/transactions")]
    public async Task<ActionResult<List<CapitalTransaction>>> GetTransactions(Guid accountId)
    {
        try
        {
            var transactions = await _capitalService.GetTransactionsAsync(accountId);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTransactions");
            return StatusCode(500, new { Success = false, Message = "Error retrieving transactions", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all transactions across all accounts
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<CapitalTransaction>>> GetAllTransactions()
    {
        try
        {
            var transactions = await _capitalService.GetTransactionsAsync(Guid.Empty);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllTransactions");
            return StatusCode(500, new { Success = false, Message = "Error retrieving all transactions", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific transaction
    /// </summary>
    [HttpGet("transactions/{transactionId}")]
    public async Task<ActionResult<CapitalTransaction>> GetTransaction(Guid transactionId)
    {
        try
        {
            var transactions = await _capitalService.GetTransactionsAsync(Guid.Empty);
            var transaction = transactions.FirstOrDefault(t => t.Id == transactionId);
            if (transaction == null) return NotFound(new { message = "Transaction not found" });
            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTransaction");
            return StatusCode(500, new { Success = false, Message = "Error retrieving transaction", Error = ex.Message });
        }
    }

    /// <summary>
    /// Recalculate ownership percentages
    /// </summary>
    [HttpPost("recalculate-ownership")]
    public async Task<IActionResult> RecalculateOwnership()
    {
        try
        {
            await _capitalService.RecalculateOwnershipPercentagesAsync();
            return Ok(new { message = "Ownership percentages recalculated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RecalculateOwnership");
            return StatusCode(500, new { Success = false, Message = "Error recalculating ownership", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get total capital
    /// </summary>
    [HttpGet("total-capital")]
    public async Task<ActionResult<object>> GetTotalCapital()
    {
        try
        {
            var total = await _capitalService.GetTotalCapitalAsync();
            return Ok(new { totalCapital = total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTotalCapital");
            return StatusCode(500, new { Success = false, Message = "Error calculating total capital", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get ownership percentage for account
    /// </summary>
    [HttpGet("{accountId}/ownership")]
    public async Task<ActionResult<object>> GetOwnershipPercentage(Guid accountId)
    {
        try
        {
            var percentage = await _capitalService.GetOwnershipPercentageAsync(accountId);
            return Ok(new { ownershipPercentage = percentage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOwnershipPercentage");
            return StatusCode(500, new { Success = false, Message = "Error retrieving ownership percentage", Error = ex.Message });
        }
    }

    public class DistributeProfitRequest
    {
        public long ProfitAmount { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    /// <summary>
    /// Distribute company profit pro-rata to all active partners
    /// </summary>
    [HttpPost("distribute-profit")]
    public async Task<IActionResult> DistributeProfit([FromBody] DistributeProfitRequest request)
    {
        try
        {
            await _capitalService.DistributeProfitDirectAsync(request.ProfitAmount, request.Period, UserId);
            return Ok(new { success = true, message = "Profit distributed successfully to all partners" });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DistributeProfit");
            return StatusCode(500, new { Success = false, Message = "Error distributing profit", Error = ex.Message });
        }
    }
}

public class CreateCapitalAccountRequest
{
    public Guid PartnerId { get; set; }
    public long OpeningBalance { get; set; } // In paise
}
