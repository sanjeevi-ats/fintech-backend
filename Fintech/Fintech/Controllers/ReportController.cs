using Fintech.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Route("api/v1/[controller]")]
public class ReportController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly IReportPdfService _reportPdfService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, IReportPdfService reportPdfService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _reportPdfService = reportPdfService;
        _logger = logger;
    }

    // PDF Download endpoints (more specific routes first)
    /// <summary>
    /// Download customer loan report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("customer/{customerId}/pdf")]
    public async Task<IActionResult> DownloadCustomerLoanReport(Guid customerId)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GenerateCustomerLoanReportPdfAsync(customerId);
            
            return File(pdfBytes, "text/html", $"customer_loan_report_{customerId}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadCustomerLoanReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating customer loan report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download company turnover report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("turnover/pdf")]
    public async Task<IActionResult> DownloadTurnoverReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GenerateTurnoverReportPdfAsync(startDate, endDate);
            
            return File(pdfBytes, "text/html", $"turnover_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadTurnoverReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating turnover report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download Profit & Loss report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("pnl/pdf")]
    public async Task<IActionResult> DownloadPnLReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GeneratePnLReportPdfAsync(startDate, endDate);
            
            return File(pdfBytes, "text/html", $"pnl_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadPnLReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating P&L report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download partner investment report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("partner/{partnerId}/pdf")]
    public async Task<IActionResult> DownloadPartnerReport(Guid partnerId)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GeneratePartnerReportPdfAsync(partnerId);
            
            return File(pdfBytes, "text/html", $"partner_report_{partnerId}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadPartnerReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating partner report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download Portfolio at Risk (PAR) report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("par/pdf")]
    public async Task<IActionResult> DownloadParReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GenerateParReportPdfAsync(startDate, endDate);
            
            return File(pdfBytes, "text/html", $"par_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadParReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating PAR report.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download collection efficiency report as PDF/HTML
    /// </summary>
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("efficiency/pdf")]
    public async Task<IActionResult> DownloadCollectionEfficiencyReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var pdfBytes = await _reportPdfService.GenerateCollectionEfficiencyReportPdfAsync(startDate, endDate);
            
            return File(pdfBytes, "text/html", $"collection_efficiency_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadCollectionEfficiencyReport");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating collection efficiency report.",
                Error = ex.Message
            });
        }
    }

    // Data endpoints (less specific routes)
    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("par")]
    public async Task<IActionResult> GetPar([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        try
        {
            var result = await _reportService.GetPortfolioAtRiskAsync(start, end);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPar");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving portfolio at risk data.",
                Error = ex.Message
            });
        }
    }

    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("efficiency")]
    public async Task<IActionResult> GetEfficiency([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        try
        {
            var result = await _reportService.GetCollectionEfficiencyAsync(start, end);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEfficiency");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving collection efficiency data.",
                Error = ex.Message
            });
        }
    }

    [Authorize(Roles = "super_admin,branch_manager,accountant")]
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var result = await _reportService.GetDashboardStatsAsync();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDashboardStats");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving dashboard statistics.",
                Error = ex.Message
            });
        }
    }
}
