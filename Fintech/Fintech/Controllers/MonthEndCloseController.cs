using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;

namespace Fintech.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class MonthEndCloseController : ControllerBase
    {
        private readonly MonthEndCloseService _closeService;
        private readonly ProfitLossService _plService;
        private readonly CashFlowService _cfService;

        public MonthEndCloseController(MonthEndCloseService closeService, ProfitLossService plService, CashFlowService cfService)
        {
            _closeService = closeService;
            _plService = plService;
            _cfService = cfService;
        }

        [HttpPost("period/{periodId}/close")]
        [Authorize(Roles = "super_admin,branch_manager")]
        public async Task<IActionResult> ClosePeriod(Guid periodId, CancellationToken ct)
        {
            var user = User.FindFirst("email")?.Value ?? "system";
            var result = await _closeService.CloseAccountingPeriodAsync(periodId, user, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("period/{periodId}/validate")]
        [Authorize(Roles = "super_admin,branch_manager")]
        public async Task<IActionResult> ValidateCloseReadiness(Guid periodId, CancellationToken ct)
        {
            var result = await _closeService.ValidateCloseReadinessAsync(periodId, ct);
            return Ok(result);
        }

        [HttpGet("period/{periodId}/status")]
        public async Task<IActionResult> GetCloseStatus(Guid periodId, CancellationToken ct)
        {
            var status = await _closeService.GetCloseStatusAsync(periodId, ct);
            return status == null ? NotFound() : Ok(status);
        }

        [HttpPost("period/{periodId}/reverse")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ReversePeriod(Guid periodId, CancellationToken ct)
        {
            var user = User.FindFirst("email")?.Value ?? "system";
            var result = await _closeService.ReverseCloseAsync(periodId, user, ct);
            return result ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpGet("profit-loss/{periodId}")]
        public async Task<IActionResult> GetProfitLoss(Guid periodId, CancellationToken ct)
        {
            var pl = await _plService.GeneratePLStatementAsync(periodId, null, ct);
            return pl == null ? NotFound() : Ok(pl);
        }

        [HttpGet("cash-flow/{periodId}")]
        public async Task<IActionResult> GetCashFlow(Guid periodId, CancellationToken ct)
        {
            var cf = await _cfService.GenerateCashFlowStatementAsync(periodId, null, ct);
            return cf == null ? NotFound() : Ok(cf);
        }
    }
}
