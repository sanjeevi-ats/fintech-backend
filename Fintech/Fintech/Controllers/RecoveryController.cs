using Fintech.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Route("api/[controller]")]
public class RecoveryController : BaseApiController
{
    private readonly IRecoveryService _recoveryService;
    private readonly ILogger<RecoveryController> _logger;

    public RecoveryController(IRecoveryService recoveryService, ILogger<RecoveryController> logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        try
        {
            var result = await _recoveryService.GetOverdueLoansAsync();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOverdue");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving overdue loans.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("{id}/follow-up")]
    public async Task<IActionResult> RecordFollowUp(Guid id, [FromBody] string notes)
    {
        try
        {
            await _recoveryService.RecordFollowUpAsync(id, notes);
            
            return Ok(new { message = "Follow-up recorded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RecordFollowUp");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while recording follow-up.",
                Error = ex.Message
            });
        }
    }
}
