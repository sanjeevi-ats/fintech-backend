using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Authorize(Roles = "super_admin,branch_manager,accountant")]
[Route("api/[controller]")]
public class LoanClosureController : BaseApiController
{
    private readonly ILoanClosureService _loanClosureService;
    private readonly ILogger<LoanClosureController> _logger;

    public LoanClosureController(ILoanClosureService loanClosureService, ILogger<LoanClosureController> logger)
    {
        _loanClosureService = loanClosureService;
        _logger = logger;
    }

    /// <summary>
    /// Check if a loan is eligible for closure and close it if all installments are paid
    /// </summary>
    [HttpPost("{loanId}/check-and-close")]
    public async Task<IActionResult> CheckAndClose(Guid loanId)
    {
        try
        {
            await _loanClosureService.CheckAndCloseLoanAsync(loanId);
            
            return Ok(new { message = "Loan closure check completed successfully" });
        }
        catch (FinVedaException ex)
        {
            _logger.LogError(ex, "FinVedaException in CheckAndClose");
            
            return StatusCode(ex.StatusCode, new { error = ex.ErrorCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckAndClose");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred during loan closure check.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Manually close a loan with a reason (e.g., NPA, customer request)
    /// </summary>
    [HttpPost("{loanId}/manual-close")]
    public async Task<IActionResult> ManualClose(Guid loanId, [FromBody] ManualClosureRequest request)
    {
        try
        {
            await _loanClosureService.ManuallyCloseLoanAsync(loanId, request.Reason);
            
            return Ok(new { message = "Loan closed successfully", reason = request.Reason });
        }
        catch (FinVedaException ex)
        {
            _logger.LogError(ex, "FinVedaException in ManualClose");
            
            return StatusCode(ex.StatusCode, new { error = ex.ErrorCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ManualClose");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while manually closing the loan.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Verify the closure status of a loan
    /// </summary>
    [HttpGet("{loanId}/status")]
    public async Task<IActionResult> VerifyStatus(Guid loanId)
    {
        try
        {
            var status = await _loanClosureService.VerifyLoanClosureStatusAsync(loanId);
            
            return Ok(status);
        }
        catch (FinVedaException ex)
        {
            _logger.LogError(ex, "FinVedaException in VerifyStatus");
            
            return StatusCode(ex.StatusCode, new { error = ex.ErrorCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyStatus");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while verifying loan closure status.",
                Error = ex.Message
            });
        }
    }
}

public class ManualClosureRequest
{
    public string Reason { get; set; } = string.Empty;
}
