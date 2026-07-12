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
public class LoanCasesController : ControllerBase
{
    private readonly ILoanCaseService _loanService;
    private readonly IMapper _mapper;
    private readonly ILogger<LoanCasesController> _logger;
    private readonly ITenantService _tenantService;
    private readonly FinVedaDbContext _db;

    public LoanCasesController(
        ILoanCaseService loanService, 
        IMapper mapper, 
        ILogger<LoanCasesController> logger,
        ITenantService tenantService,
        FinVedaDbContext db)
    {
        _loanService = loanService;
        _mapper = mapper;
        _logger = logger;
        _tenantService = tenantService;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest request)
    {
        try
        {
            var loan = _mapper.Map<LoanCase>(request);
            var result = await _loanService.CreateAsync(loan);
            
            var dto = _mapper.Map<LoanCaseDto>(result);
            dto.LoanCode = result.LoanCode ?? string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the loan case.",
                Error = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanCaseDto>>> GetAll()
    {
        try
        {
            var loans = await _loanService.GetAllAsync();
            
            var dtos = loans.Select(l => 
            {
                var dto = _mapper.Map<LoanCaseDto>(l);
                dto.LoanCode = l.LoanCode ?? string.Empty;
                // Populate customer data from the related Customer entity
                // Generate CustomerCode if not in database (format: CUS + last 4 digits of customer ID)
                if (l.Customer != null)
                {
                    dto.CustomerCode = !string.IsNullOrEmpty(l.Customer.CustomerCode) 
                        ? l.Customer.CustomerCode 
                        : $"CUS{l.Customer.Id.ToString().Substring(0, 8).ToUpper()}";
                    dto.CustomerName = l.Customer.Name;
                }
                return dto;
            }).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAll");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving loan cases.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanCaseDto>> GetById(Guid id)
    {
        try
        {
            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null)
            {
                return NotFound();
            }
            
            var dto = _mapper.Map<LoanCaseDto>(loan);
            dto.LoanCode = loan.LoanCode ?? string.Empty;
            // Populate customer data from the related Customer entity
            if (loan.Customer != null)
            {
                dto.CustomerCode = !string.IsNullOrEmpty(loan.Customer.CustomerCode) 
                    ? loan.Customer.CustomerCode 
                    : $"CUS{loan.Customer.Id.ToString().Substring(0, 8).ToUpper()}";
                dto.CustomerName = loan.Customer.Name;
            }
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the loan case.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get loan case by business code (loan code)
    /// </summary>
    /// <param name="code">Loan code (e.g., LN0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var loan = await _loanService.GetByCodeAsync(code);
            if (loan == null)
                return NotFound(new { message = $"Loan with code {code} not found" });
            
            var dto = _mapper.Map<LoanCaseDto>(loan);
            dto.LoanCode = loan.LoanCode ?? string.Empty;
            // Populate customer data from the related Customer entity
            if (loan.Customer != null)
            {
                dto.CustomerCode = !string.IsNullOrEmpty(loan.Customer.CustomerCode) 
                    ? loan.Customer.CustomerCode 
                    : $"CUS{loan.Customer.Id.ToString().Substring(0, 8).ToUpper()}";
                dto.CustomerName = loan.Customer.Name;
            }
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the loan case by code.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("search/{codeOrId}")]
    public async Task<ActionResult<LoanCaseDto>> Search(string codeOrId)
    {
        try
        {
            LoanCase? loan = null;

            // Try to search by loan code first
            loan = await _loanService.GetByLoanCodeAsync(codeOrId);

            // If not found, try to search by ID
            if (loan == null && Guid.TryParse(codeOrId, out var id))
            {
                loan = await _loanService.GetByIdAsync(id);
            }

            if (loan == null)
            {
                return NotFound(new { message = "Loan not found" });
            }

            var result = _mapper.Map<LoanCaseDto>(loan);
            // Populate customer data from the related Customer entity
            if (loan.Customer != null)
            {
                result.CustomerCode = !string.IsNullOrEmpty(loan.Customer.CustomerCode) 
                    ? loan.Customer.CustomerCode 
                    : $"CUS{loan.Customer.Id.ToString().Substring(0, 8).ToUpper()}";
                result.CustomerName = loan.Customer.Name;
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Search");

            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while searching for the loan.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            await _loanService.ApproveLoanAsync(id);
            
            return Ok(new { message = "Loan approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Approve");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while approving the loan.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("{id}/disburse")]
    public async Task<IActionResult> Disburse(Guid id)
    {
        try
        {
            await _loanService.DisburseLoanAsync(id);
            
            return Ok(new { message = "Loan disbursed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Disburse");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while disbursing the loan.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Upload temporary document files before loan case is created
    /// </summary>
    [HttpPost("upload-temp")]
    public async Task<IActionResult> UploadTempDocuments([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files uploaded" });
            }

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "loans");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var urls = new List<string>();
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileExt = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/uploads/loans/{uniqueFileName}";
                urls.Add(url);
            }

            return Ok(new { success = true, urls });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadTempDocuments");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Upload loan documentation files (Aadhaar, PAN, KYC) and link them to the loan case
    /// </summary>
    [HttpPost("{id}/documents")]
    public async Task<IActionResult> UploadDocuments(Guid id, [FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files uploaded" });
            }

            var loan = await _db.LoanCases.FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound(new { message = "Loan case not found" });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "loans");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var urls = new List<string>();
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileExt = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/uploads/loans/{uniqueFileName}";
                urls.Add(url);
            }

            if (urls.Count > 0)
            {
                var existingUrls = string.IsNullOrEmpty(loan.DocumentUrls)
                    ? new List<string>()
                    : loan.DocumentUrls.Split(',').ToList();

                existingUrls.AddRange(urls);
                loan.DocumentUrls = string.Join(",", existingUrls);

                _db.LoanCases.Update(loan);
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, urls });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadDocuments");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Submit a loan for approval (transitions DRAFT -> PENDING_APPROVAL)
    /// </summary>
    [HttpPost("{id}/submit-approval")]
    public async Task<IActionResult> SubmitApproval(Guid id)
    {
        try
        {
            var loan = await _db.LoanCases.FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound(new { message = "Loan case not found" });

            if (loan.Status != LoanStatus.draft)
            {
                return BadRequest(new { message = $"Cannot submit loan for approval with status: {loan.Status}" });
            }

            loan.Status = LoanStatus.pending_approval;
            loan.SubmittedById = _tenantService.UserId != Guid.Empty ? _tenantService.UserId : Guid.Parse("22222222-2222-2222-2222-222222222227"); // Fallback to Loan Officer
            loan.SubmittedAt = DateTime.UtcNow;

            _db.LoanCases.Update(loan);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Loan submitted for approval successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitApproval");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Approve a loan application (transitions PENDING_APPROVAL -> PENDING_DISBURSE)
    /// </summary>
    [HttpPost("{id}/approve-application")]
    public async Task<IActionResult> ApproveApplication(Guid id)
    {
        try
        {
            var loan = await _db.LoanCases.FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound(new { message = "Loan case not found" });

            if (loan.Status != LoanStatus.pending_approval && loan.Status != LoanStatus.draft)
            {
                return BadRequest(new { message = $"Cannot approve loan with status: {loan.Status}" });
            }

            loan.Status = LoanStatus.pending_disburse;
            loan.ApprovedById = _tenantService.UserId != Guid.Empty ? _tenantService.UserId : Guid.Parse("22222222-2222-2222-2222-222222222221"); // Fallback to Admin
            loan.ApprovedAt = DateTime.UtcNow;

            _db.LoanCases.Update(loan);
            await _db.SaveChangesAsync();

            // Run database stored procedure
            await _loanService.ApproveLoanAsync(id);

            return Ok(new { success = true, message = "Loan approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveApplication");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Reject a loan application (transitions PENDING_APPROVAL -> REJECTED)
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest req)
    {
        try
        {
            var loan = await _db.LoanCases.FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound(new { message = "Loan case not found" });

            if (loan.Status != LoanStatus.pending_approval)
            {
                return BadRequest(new { message = $"Cannot reject loan with status: {loan.Status}" });
            }

            loan.Status = LoanStatus.rejected;
            loan.RejectionReason = req.RejectionReason;

            _db.LoanCases.Update(loan);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Loan rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reject");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class RejectRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
    }
}
