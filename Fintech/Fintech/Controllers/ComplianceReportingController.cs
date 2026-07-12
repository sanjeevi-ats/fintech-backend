using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;

namespace Fintech.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComplianceReportingController : ControllerBase
{
    private readonly IComplianceReportingService _complianceService;
    private readonly ILogger<ComplianceReportingController> _logger;

    public ComplianceReportingController(
        IComplianceReportingService complianceService,
        ILogger<ComplianceReportingController> logger)
    {
        _complianceService = complianceService;
        _logger = logger;
    }

    /// <summary>
    /// Generate RBI monthly compliance report
    /// </summary>
    [HttpPost("rbi-monthly")]
    public async Task<IActionResult> GenerateRBIMonthlyReport(
        [FromBody] RBIReportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/compliancereporting/rbi-monthly - Branch={Branch}, Month={Month}/{Year}",
                request?.BranchId, request?.Month, request?.Year);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _complianceService.GenerateRBIMonthlyReportAsync(
                request.BranchId, request.Month, request.Year, ct);

            if (result == null)
                return NotFound(new { error = "Branch not found" });

            _logger.LogInformation(
                "POST /api/compliancereporting/rbi-monthly - Success (200): NPA={NPA}%",
                result.NPARatio.ToString("F2"));

            return Ok(new { data = result, message = "RBI monthly report generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RBI report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate GST compliance report
    /// </summary>
    [HttpPost("gst-report")]
    public async Task<IActionResult> GenerateGSTReport(
        [FromBody] GSTReportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/compliancereporting/gst-report - Quarter={Quarter}, Year={Year}",
                request?.Quarter, request?.Year);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _complianceService.GenerateGSTReportAsync(
                request.BranchId, request.Quarter ?? "Q1", request.Year, ct);

            if (result == null)
                return NotFound(new { error = "Branch not found" });

            _logger.LogInformation(
                "POST /api/compliancereporting/gst-report - Success (200): Liability={Liability}",
                result.TotalGSTLiability);

            return Ok(new { data = result, message = "GST report generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GST report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze loan portfolio compliance
    /// </summary>
    [HttpPost("portfolio-compliance")]
    public async Task<IActionResult> AnalyzePortfolioCompliance(
        [FromBody] ComplianceAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/compliancereporting/portfolio-compliance - Branch={Branch}",
                request?.BranchId);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _complianceService.AnalyzeLoanPortfolioComplianceAsync(
                request.BranchId, ct);

            if (result == null)
                return NotFound(new { error = "Branch not found" });

            _logger.LogInformation(
                "POST /api/compliancereporting/portfolio-compliance - Success (200): Compliant={Compliant}",
                result.MeetsMinimumCapitalRatio);

            return Ok(new { data = result, message = "Portfolio compliance analysis completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing portfolio compliance");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate NPA report
    /// </summary>
    [HttpPost("npa-report")]
    public async Task<IActionResult> GenerateNPAReport(
        [FromBody] ComplianceAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/compliancereporting/npa-report - Branch={Branch}",
                request?.BranchId);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _complianceService.GenerateNPAReportAsync(request.BranchId, ct);

            if (result == null)
                return NotFound(new { error = "Branch not found" });

            _logger.LogInformation(
                "POST /api/compliancereporting/npa-report - Success (200): NPA Ratio={Ratio}%",
                result.NPARatio.ToString("F2"));

            return Ok(new { data = result, message = "NPA report generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NPA report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate provisioning report
    /// </summary>
    [HttpPost("provisioning-report")]
    public async Task<IActionResult> GenerateProvisioningReport(
        [FromBody] RBIReportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST /api/compliancereporting/provisioning-report - Month={Month}/{Year}",
                request?.Month, request?.Year);

            if (request?.BranchId == Guid.Empty)
                return BadRequest(new { error = "BranchId is required" });

            var result = await _complianceService.GenerateProvisioningReportAsync(
                request.BranchId, request.Month, request.Year, ct);

            if (result == null)
                return NotFound(new { error = "Branch not found" });

            _logger.LogInformation(
                "POST /api/compliancereporting/provisioning-report - Success (200): Coverage={Coverage}%",
                result.ProvisioningCoverageRatio.ToString("F2"));

            return Ok(new { data = result, message = "Provisioning report generated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provisioning report");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class RBIReportRequest
{
    public Guid BranchId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class GSTReportRequest
{
    public Guid BranchId { get; set; }
    public string? Quarter { get; set; }
    public int Year { get; set; }
}

public class ComplianceAnalysisRequest
{
    public Guid BranchId { get; set; }
}

