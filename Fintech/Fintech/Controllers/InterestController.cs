using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Fintech.Application.Services;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterestController : ControllerBase
{
    private readonly InterestCalculationService _calculationService;
    private readonly InterestPostingService _postingService;
    private readonly InterestWaiverService _waiverService;
    private readonly InterestReportingService _reportingService;
    private readonly IControllerFileLoggerService _logger;

    public InterestController(
        InterestCalculationService calculationService,
        InterestPostingService postingService,
        InterestWaiverService waiverService,
        InterestReportingService reportingService,
        IControllerFileLoggerService logger)
    {
        _calculationService = calculationService;
        _postingService = postingService;
        _waiverService = waiverService;
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate daily interest for a specific loan
    /// Formula: DailyInterest = (LoanBalance × AnnualRate / 365)
    /// </summary>
    [HttpPost("calculate/{loanId}")]
    public async Task<IActionResult> CalculateDailyInterest(
        Guid loanId,
        [FromBody] InterestCalculationRequestDto request)
    {
        try
        {
            var calculation = await _calculationService.CalculateDailyInterestAsync(
                loanId,
                request.AnnualRate,
                request.CalculationDate,
                request.InterestType ?? 1);

            if (calculation == null)
            {
                await _logger.LogWarningAsync("InterestController", "CalculateDailyInterest", $"Failed to calculate interest for loan {loanId}");
                return BadRequest(new { success = false, message = "Failed to calculate interest" });
            }

            await _logger.LogInfoAsync("InterestController", "CalculateDailyInterest", null, null,
                new { loanId, amount = calculation.AccrualAmount, rate = calculation.DailyRate });
            return Ok(new { success = true, data = calculation });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "CalculateDailyInterest", ex);
            return StatusCode(500, new { success = false, message = "Error calculating interest" });
        }
    }

    /// <summary>
    /// Calculate interest for all active loans in an accounting period
    /// </summary>
    [HttpPost("calculate-period/{periodId}")]
    public async Task<IActionResult> CalculatePeriodInterest(
        Guid periodId,
        [FromBody] PeriodInterestRequestDto request)
    {
        try
        {
            var calculations = await _calculationService.CalculatePeriodInterestAsync(
                periodId,
                request.DefaultAnnualRate ?? 0.10m);

            await _logger.LogInfoAsync("InterestController", "CalculatePeriodInterest", null, null,
                new { periodId, count = calculations.Count, rate = request.DefaultAnnualRate });
            return Ok(new { success = true, data = calculations, count = calculations.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "CalculatePeriodInterest", ex);
            return StatusCode(500, new { success = false, message = "Error calculating period interest" });
        }
    }

    /// <summary>
    /// Get all interest calculations for a specific loan
    /// </summary>
    [HttpGet("by-loan/{loanId}")]
    public async Task<IActionResult> GetInterestByLoan(Guid loanId)
    {
        try
        {
            var calculations = await _calculationService.GetInterestByLoanAsync(loanId);

            await _logger.LogInfoAsync("InterestController", "GetInterestByLoan", null, null,
                new { loanId, count = calculations.Count });
            return Ok(new { success = true, data = calculations, count = calculations.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestByLoan", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving interest calculations" });
        }
    }

    /// <summary>
    /// Get all interest calculations for an accounting period
    /// </summary>
    [HttpGet("by-period/{periodId}")]
    public async Task<IActionResult> GetInterestByPeriod(Guid periodId)
    {
        try
        {
            var calculations = await _calculationService.GetInterestByPeriodAsync(periodId);

            await _logger.LogInfoAsync("InterestController", "GetInterestByPeriod", null, null,
                new { periodId, count = calculations.Count });
            return Ok(new { success = true, data = calculations, count = calculations.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestByPeriod", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving period interest" });
        }
    }

    /// <summary>
    /// Get total pending (not yet posted) interest for a loan
    /// </summary>
    [HttpGet("pending/{loanId}")]
    public async Task<IActionResult> GetPendingInterest(Guid loanId)
    {
        try
        {
            var pendingAmount = await _calculationService.GetPendingInterestAsync(loanId);

            await _logger.LogInfoAsync("InterestController", "GetPendingInterest", null, null,
                new { loanId, amount = pendingAmount });
            return Ok(new { success = true, pendingAmount });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetPendingInterest", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving pending interest" });
        }
    }

    /// <summary>
    /// Get comprehensive interest summary for a loan
    /// Includes: total calculated, posted, waived, and pending interest
    /// </summary>
    [HttpGet("summary/{loanId}")]
    public async Task<IActionResult> GetInterestSummary(Guid loanId)
    {
        try
        {
            var summary = await _calculationService.GetInterestSummaryAsync(loanId);

            await _logger.LogInfoAsync("InterestController", "GetInterestSummary", null, null,
                new { loanId, calculated = summary.TotalCalculated, posted = summary.TotalPosted, waived = summary.TotalWaived });
            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestSummary", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving interest summary" });
        }
    }

    /// <summary>
    /// Post monthly interest for all loans in an accounting period
    /// </summary>
    [HttpPost("post-period/{periodId}")]
    [Authorize(Roles = "super_admin,accountant")]
    public async Task<IActionResult> PostMonthlyInterest(Guid periodId)
    {
        try
        {
            var postings = await _postingService.PostMonthlyInterestAsync(periodId);

            await _logger.LogInfoAsync("InterestController", "PostMonthlyInterest", null, null,
                new { periodId, count = postings.Count, totalAmount = postings.Sum(p => p.PostedAmount) });
            return Ok(new { success = true, data = postings, count = postings.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "PostMonthlyInterest", ex);
            return StatusCode(500, new { success = false, message = "Error posting monthly interest" });
        }
    }

    /// <summary>
    /// Post interest for a single loan in a period
    /// </summary>
    [HttpPost("post/{loanId}/{periodId}")]
    [Authorize(Roles = "super_admin,accountant")]
    public async Task<IActionResult> PostInterestForLoan(Guid loanId, Guid periodId)
    {
        try
        {
            var posting = await _postingService.PostInterestForLoanAsync(loanId, periodId);

            if (posting == null)
            {
                await _logger.LogWarningAsync("InterestController", "PostInterestForLoan", "Failed to post interest");
                return BadRequest(new { success = false, message = "Failed to post interest" });
            }

            await _logger.LogInfoAsync("InterestController", "PostInterestForLoan", null, null,
                new { loanId, periodId, amount = posting.PostedAmount });
            return Ok(new { success = true, data = posting });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "PostInterestForLoan", ex);
            return StatusCode(500, new { success = false, message = "Error posting interest" });
        }
    }

    /// <summary>
    /// Get posting history for a loan
    /// </summary>
    [HttpGet("postings/{loanId}")]
    public async Task<IActionResult> GetPostingHistory(Guid loanId)
    {
        try
        {
            var postings = await _postingService.GetPostingHistoryAsync(loanId);

            await _logger.LogInfoAsync("InterestController", "GetPostingHistory", null, null,
                new { loanId, count = postings.Count });
            return Ok(new { success = true, data = postings, count = postings.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetPostingHistory", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving posting history" });
        }
    }

    /// <summary>
    /// Reverse a posted interest entry
    /// </summary>
    [HttpPost("reverse-posting/{postingId}")]
    [Authorize(Roles = "super_admin")]
    public async Task<IActionResult> ReversePosting(Guid postingId, [FromBody] ReversePostingRequestDto request)
    {
        try
        {
            var success = await _postingService.ReversePostingAsync(postingId, request.ReversedBy ?? "system");

            if (!success)
            {
                await _logger.LogWarningAsync("InterestController", "ReversePosting", "Failed to reverse posting");
                return BadRequest(new { success = false, message = "Failed to reverse posting" });
            }

            await _logger.LogInfoAsync("InterestController", "ReversePosting", null, null, new { postingId });
            return Ok(new { success = true, message = "Posting reversed successfully" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "ReversePosting", ex);
            return StatusCode(500, new { success = false, message = "Error reversing posting" });
        }
    }

    /// <summary>
    /// Apply interest waiver (creates pending waiver for approval)
    /// </summary>
    [HttpPost("waiver/{loanId}")]
    [Authorize(Roles = "super_admin,branch_manager")]
    public async Task<IActionResult> ApplyWaiver(Guid loanId, [FromBody] WaiverRequestDto request)
    {
        try
        {
            var waiver = await _waiverService.ApplyWaiverAsync(loanId, request.WaiverAmount, request.Reason);

            if (waiver == null)
            {
                await _logger.LogWarningAsync("InterestController", "ApplyWaiver", "Failed to apply waiver");
                return BadRequest(new { success = false, message = "Failed to apply waiver" });
            }

            await _logger.LogInfoAsync("InterestController", "ApplyWaiver", null, null,
                new { loanId, amount = request.WaiverAmount });
            return Ok(new { success = true, data = waiver });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "ApplyWaiver", ex);
            return StatusCode(500, new { success = false, message = "Error applying waiver" });
        }
    }

    /// <summary>
    /// Approve a pending waiver (role: super_admin only)
    /// </summary>
    [HttpPost("waiver/{waiverId}/approve")]
    [Authorize(Roles = "super_admin")]
    public async Task<IActionResult> ApproveWaiver(Guid waiverId, [FromBody] ApproveWaiverRequestDto request)
    {
        try
        {
            var success = await _waiverService.ApproveWaiverAsync(waiverId, request.ApprovedBy ?? "system");

            if (!success)
            {
                await _logger.LogWarningAsync("InterestController", "ApproveWaiver", "Failed to approve waiver");
                return BadRequest(new { success = false, message = "Failed to approve waiver" });
            }

            await _logger.LogInfoAsync("InterestController", "ApproveWaiver", null, null, new { waiverId });
            return Ok(new { success = true, message = "Waiver approved successfully" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "ApproveWaiver", ex);
            return StatusCode(500, new { success = false, message = "Error approving waiver" });
        }
    }

    /// <summary>
    /// Reject a pending waiver
    /// </summary>
    [HttpPost("waiver/{waiverId}/reject")]
    [Authorize(Roles = "super_admin")]
    public async Task<IActionResult> RejectWaiver(Guid waiverId, [FromBody] RejectWaiverRequestDto request)
    {
        try
        {
            var success = await _waiverService.RejectWaiverAsync(waiverId, request.RejectionReason ?? "");

            if (!success)
            {
                await _logger.LogWarningAsync("InterestController", "RejectWaiver", "Failed to reject waiver");
                return BadRequest(new { success = false, message = "Failed to reject waiver" });
            }

            await _logger.LogInfoAsync("InterestController", "RejectWaiver", null, null, new { waiverId });
            return Ok(new { success = true, message = "Waiver rejected successfully" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "RejectWaiver", ex);
            return StatusCode(500, new { success = false, message = "Error rejecting waiver" });
        }
    }

    /// <summary>
    /// Get all waivers for a loan
    /// </summary>
    [HttpGet("waivers/{loanId}")]
    public async Task<IActionResult> GetWaivers(Guid loanId)
    {
        try
        {
            var waivers = await _waiverService.GetWaiversAsync(loanId);

            await _logger.LogInfoAsync("InterestController", "GetWaivers", null, null,
                new { loanId, count = waivers.Count });
            return Ok(new { success = true, data = waivers, count = waivers.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetWaivers", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving waivers" });
        }
    }

    /// <summary>
    /// Get pending waivers requiring approval
    /// </summary>
    [HttpGet("waivers/pending")]
    [Authorize(Roles = "super_admin")]
    public async Task<IActionResult> GetPendingWaivers()
    {
        try
        {
            var waivers = await _waiverService.GetPendingWaiversAsync();

            await _logger.LogInfoAsync("InterestController", "GetPendingWaivers", null, null,
                new { count = waivers.Count });
            return Ok(new { success = true, data = waivers, count = waivers.Count });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetPendingWaivers", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving pending waivers" });
        }
    }

    /// <summary>
    /// Generate comprehensive interest statement for a customer
    /// Shows all interest transactions across all their loans
    /// </summary>
    [HttpGet("statement/{customerId}")]
    public async Task<IActionResult> GetInterestStatement(
        Guid customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var statement = await _reportingService.GenerateInterestStatementAsync(customerId, fromDate, toDate);

            await _logger.LogInfoAsync("InterestController", "GetInterestStatement", null, null,
                new { customerId, total = statement.TotalCalculated });
            return Ok(new { success = true, data = statement });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestStatement", ex);
            return StatusCode(500, new { success = false, message = "Error generating statement" });
        }
    }

    /// <summary>
    /// Get interest aging analysis - shows overdue interest not collected
    /// </summary>
    [HttpGet("aging")]
    public async Task<IActionResult> GetInterestAging([FromQuery] Guid? customerId = null)
    {
        try
        {
            var aging = await _reportingService.GetInterestAgingAsync(customerId);

            await _logger.LogInfoAsync("InterestController", "GetInterestAging", null, null,
                new { total = aging.TotalOutstanding });
            return Ok(new { success = true, data = aging });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestAging", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving aging analysis" });
        }
    }

    /// <summary>
    /// Get system-wide interest summary report
    /// </summary>
    [HttpGet("report/summary")]
    [Authorize(Roles = "super_admin,accountant")]
    public async Task<IActionResult> GetSystemSummary()
    {
        try
        {
            var report = await _reportingService.GetSystemInterestSummaryAsync();

            await _logger.LogInfoAsync("InterestController", "GetSystemSummary", null, null,
                new { total = report.TotalCalculated, posted = report.TotalPosted });
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetSystemSummary", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving summary report" });
        }
    }

    /// <summary>
    /// Get interest by branch
    /// </summary>
    [HttpGet("report/by-branch")]
    [Authorize(Roles = "super_admin,accountant")]
    public async Task<IActionResult> GetInterestByBranch()
    {
        try
        {
            var byBranch = await _reportingService.GetInterestByBranchAsync();

            await _logger.LogInfoAsync("InterestController", "GetInterestByBranch", null, null,
                new { count = byBranch.Count });
            return Ok(new { success = true, data = byBranch });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("InterestController", "GetInterestByBranch", ex);
            return StatusCode(500, new { success = false, message = "Error retrieving branch analysis" });
        }
    }
}

public class InterestCalculationRequestDto
{
    public decimal AnnualRate { get; set; } // e.g., 0.10 for 10%
    public DateTime CalculationDate { get; set; }
    public int? InterestType { get; set; } = 1; // 1=Fixed, 2=Declining, 3=Variable, 4=StepUp
}

public class PeriodInterestRequestDto
{
    public decimal? DefaultAnnualRate { get; set; } = 0.10m; // Default 10% if not specified
}

public class ReversePostingRequestDto
{
    public string? ReversedBy { get; set; }
}

public class WaiverRequestDto
{
    public long WaiverAmount { get; set; }
    public string? Reason { get; set; }
}

public class ApproveWaiverRequestDto
{
    public string? ApprovedBy { get; set; }
}

public class RejectWaiverRequestDto
{
    public string? RejectionReason { get; set; }
}
