using AutoMapper;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

using Microsoft.AspNetCore.Authorization;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly IReceiptPdfService _receiptPdfService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReceiptsController> _logger;
    private readonly FinVedaDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly ICollectionService _collectionService;

    public ReceiptsController(
        IReceiptService receiptService,
        IReceiptPdfService receiptPdfService,
        IMapper mapper,
        ILogger<ReceiptsController> logger,
        FinVedaDbContext db,
        ITenantService tenantService,
        ICollectionService collectionService)
    {
        _receiptService = receiptService;
        _receiptPdfService = receiptPdfService;
        _mapper = mapper;
        _logger = logger;
        _db = db;
        _tenantService = tenantService;
        _collectionService = collectionService;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordPaymentRequest request)
    {
        try
        {
            var receipt = _mapper.Map<Receipt>(request);
            var result = await _receiptService.RecordPaymentAsync(receipt);
            
            var dto = _mapper.Map<ReceiptDto>(result);
            dto.Code = result.ReceiptCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Record");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while recording the payment receipt.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get receipt by business code
    /// </summary>
    /// <param name="code">Receipt code (e.g., RCP0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var receipt = await _receiptService.GetByCodeAsync(code);
            if (receipt == null)
                return NotFound(new { message = $"Receipt with code {code} not found" });
            
            var dto = _mapper.Map<ReceiptDto>(receipt);
            dto.Code = receipt.ReceiptCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the receipt by code.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download receipt as PDF/HTML
    /// </summary>
    [HttpGet("{receiptId}/pdf")]
    public async Task<IActionResult> DownloadReceiptPdf(Guid receiptId)
    {
        try
        {
            var pdfBytes = await _receiptPdfService.GenerateReceiptPdfAsync(receiptId);
            
            return File(pdfBytes, "text/html", $"receipt_{receiptId}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadReceiptPdf");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating receipt PDF.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get enhanced receipt details
    /// </summary>
    [HttpGet("{receiptId}/details")]
    public async Task<IActionResult> GetReceiptDetails(Guid receiptId)
    {
        try
        {
            var details = await _receiptPdfService.GetEnhancedReceiptDetailsAsync(receiptId);
            
            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReceiptDetails");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving receipt details.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Download multiple receipts as PDF/HTML
    /// </summary>
    [HttpPost("batch/pdf")]
    public async Task<IActionResult> DownloadReceiptsBatch([FromBody] List<Guid> receiptIds)
    {
        try
        {
            var pdfBytes = await _receiptPdfService.GenerateReceiptsPdfAsync(receiptIds);
            
            return File(pdfBytes, "text/html", $"receipts_batch_{DateTime.UtcNow:yyyyMMdd}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadReceiptsBatch");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating batch receipt PDFs.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Quick Pay — Admin immediate payment (bypasses approval).
    /// Searches by loanCode or customerCode, records payment and returns full receipt.
    /// </summary>
    [HttpPost("quick-pay")]
    public async Task<IActionResult> QuickPay([FromBody] QuickPayRequest req)
    {
        try
        {
            var branchId = _tenantService.BranchId;

            // 1. Find loan by code (LoanCode is DB-mapped; CustomerCode is not mapped so fallback to in-memory)
            LoanCase? loan = null;

            if (!string.IsNullOrWhiteSpace(req.LoanCode))
            {
                loan = await _db.LoanCases
                    .IgnoreQueryFilters()
                    .Include(l => l.Customer)
                    .Include(l => l.Installments)
                    .Where(l => l.BranchId == branchId)
                    .FirstOrDefaultAsync(l => l.LoanCode == req.LoanCode);
            }

            // Fallback: search by CustomerCode in memory (CustomerCode not stored in DB)
            if (loan == null && !string.IsNullOrWhiteSpace(req.CustomerCode))
            {
                var allLoans = await _db.LoanCases
                    .IgnoreQueryFilters()
                    .Include(l => l.Customer)
                    .Include(l => l.Installments)
                    .Where(l => l.BranchId == branchId)
                    .ToListAsync();

                loan = allLoans.FirstOrDefault(l =>
                    l.Customer?.CustomerCode == req.CustomerCode);
            }

            if (loan == null)
                return NotFound(new { success = false, message = "Loan or customer not found" });


            if (loan.Status != LoanStatus.active)
                return BadRequest(new { success = false, message = $"Loan is not active (status: {loan.Status})" });

            // 2. Find the next due installment
            var installment = loan.Installments
                .Where(i => i.Status != InstallmentStatus.paid && i.Status != InstallmentStatus.waived)
                .OrderBy(i => i.DueDate)
                .FirstOrDefault();

            if (installment == null)
                return BadRequest(new { success = false, message = "No pending installments found for this loan" });

            // 3. Process collection immediately
            if (!Enum.TryParse<PaymentMode>(req.PaymentMode ?? "cash", true, out var mode))
                mode = PaymentMode.cash;

            await _collectionService.CollectInstallmentAsync(
                installment.Id,
                req.Amount,
                mode.ToString(),
                req.UtrRef ?? "");

            // 4. Generate receipt
            var codeService = HttpContext.RequestServices.GetService<ICodeGenerationService>()!;
            var receiptCode = await codeService.GenerateCodeAsync("Receipt", branchId);

            var receipt = new Receipt
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                ReceiptCode = receiptCode,
                PublicId = "REC-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                LoanCaseId = loan.Id,
                InstallmentId = installment.Id,
                AmountPaid = req.Amount,
                Mode = mode,
                CapturedAt = DateTime.UtcNow,
                Remarks = req.Remarks ?? ""
            };

            _db.Receipts.Add(receipt);
            await _db.SaveChangesAsync();

            // 5. Calculate totals
            var totalPaid = loan.Installments
                .Where(i => i.Status == InstallmentStatus.paid)
                .Sum(i => i.Amount);
            var outstanding = loan.TotalReceivable - totalPaid - req.Amount;

            return Ok(new
            {
                success = true,
                receipt = new
                {
                    receiptCode,
                    receiptId = receipt.Id,
                    customerCode = loan.Customer.CustomerCode,
                    customerName = loan.Customer.Name,
                    customerPhone = loan.Customer.Phone,
                    loanCode = loan.LoanCode,
                    loanAmount = loan.Principal,
                    totalPaid = totalPaid + req.Amount,
                    paidBefore = totalPaid,
                    todayPayment = req.Amount,
                    outstanding = Math.Max(0, outstanding),
                    paymentMode = mode.ToString(),
                    paymentDate = DateTime.UtcNow.ToString("dd-MMM-yyyy"),
                    remarks = req.Remarks,
                    installmentNo = installment.No
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QuickPay");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Search loan by code or customer code — used by Quick Pay search bar
    /// CustomerCode is not stored in DB (computed field), so we do a two-step query:
    /// 1. Server-side filter: LoanCode + Customer.Name (translatable)
    /// 2. If not found, load all branch loans and filter by CustomerCode in memory
    /// </summary>
    [HttpGet("search-loan")]
    public async Task<IActionResult> SearchLoan([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest(new { success = false, message = "Query must be at least 2 characters" });

            var branchId = _tenantService.BranchId;
            var qUpper = q.ToUpper().Trim();

            // Step 1: Try server-side translatable query (LoanCode + Customer.Name only)
            var loan = await _db.LoanCases
                .IgnoreQueryFilters()
                .Include(l => l.Customer)
                .Include(l => l.Installments)
                .Where(l => l.BranchId == branchId)
                .Where(l => (l.LoanCode != null && l.LoanCode.ToUpper().Contains(qUpper)) ||
                             (l.Customer != null && l.Customer.Name != null && l.Customer.Name.ToLower().Contains(q.ToLower())))
                .FirstOrDefaultAsync();

            // Step 2: If not found by LoanCode/Name, try CustomerCode in-memory
            if (loan == null)
            {
                var allBranchLoans = await _db.LoanCases
                    .IgnoreQueryFilters()
                    .Include(l => l.Customer)
                    .Include(l => l.Installments)
                    .Where(l => l.BranchId == branchId)
                    .ToListAsync();

                loan = allBranchLoans.FirstOrDefault(l =>
                    l.Customer != null &&
                    l.Customer.CustomerCode != null &&
                    l.Customer.CustomerCode.ToUpper().Contains(qUpper));
            }

            if (loan == null)
                return NotFound(new { success = false, message = "No loan found" });

            var paidInstallments = loan.Installments.Where(i => i.Status == InstallmentStatus.paid).ToList();
            var dueInstallments = loan.Installments.Where(i => i.Status != InstallmentStatus.paid && i.Status != InstallmentStatus.waived).ToList();
            var nextDue = dueInstallments.OrderBy(i => i.DueDate).FirstOrDefault();

            var totalPaid = paidInstallments.Sum(i => i.Amount);
            var outstanding = loan.TotalReceivable - totalPaid;
            var overdue = dueInstallments.Where(i => i.DueDate < DateTime.UtcNow).Sum(i => i.Amount);

            return Ok(new
            {
                success = true,
                data = new
                {
                    loanId = loan.Id,
                    loanCode = loan.LoanCode,
                    customerCode = loan.Customer.CustomerCode,
                    customerName = loan.Customer.Name,
                    customerPhone = loan.Customer.Phone,
                    loanAmount = loan.Principal,
                    totalPaid,
                    outstanding = Math.Max(0, outstanding),
                    dueAmount = nextDue?.Amount ?? 0,
                    overdueAmount = overdue,
                    fineAmount = 0L,
                    loanStatus = loan.Status.ToString(),
                    nextInstallmentId = nextDue?.Id,
                    nextInstallmentNo = nextDue?.No,
                    nextDueDate = nextDue?.DueDate.ToString("dd-MMM-yyyy"),
                    productName = "Personal Loan"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchLoan");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class QuickPayRequest
    {
        public string? LoanCode { get; set; }
        public string? CustomerCode { get; set; }
        public long Amount { get; set; }
        public string? PaymentMode { get; set; } = "cash";
        public string? UtrRef { get; set; }
        public string? Remarks { get; set; }
    }
}
