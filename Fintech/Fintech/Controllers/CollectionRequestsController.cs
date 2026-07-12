using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class CollectionRequestsController : ControllerBase
{
    private readonly ICollectionRequestService _requestService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CollectionRequestsController> _logger;

    public CollectionRequestsController(
        ICollectionRequestService requestService,
        ITenantService tenantService,
        ILogger<CollectionRequestsController> logger)
    {
        _requestService = requestService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public class CreateRequestInput
    {
        [Required] public Guid InstallmentId { get; set; }
        [Required] public long AmountPaid { get; set; }
        [Required] public string Mode { get; set; } = "cash";
        public string UtrRef { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestInput input)
    {
        try
        {
            if (!Enum.TryParse<PaymentMode>(input.Mode, true, out var paymentMode))
            {
                return BadRequest(new { success = false, message = "Invalid payment mode. Allowed values: cash, upi, bank_transfer" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var request = await _requestService.CreateRequestAsync(
                input.InstallmentId,
                input.AmountPaid,
                paymentMode,
                input.UtrRef,
                input.Remarks,
                ipAddress
            );

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, new
            {
                success = true,
                message = "Collection request submitted successfully.",
                data = request
            });
        }
        catch (FinVedaException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create collection request");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while creating the request.", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        try
        {
            CollectionRequestStatus? requestStatus = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<CollectionRequestStatus>(status, true, out var parsedStatus))
                {
                    requestStatus = parsedStatus;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid status filter. Allowed: Pending, Approved, Rejected, Cancelled" });
                }
            }

            var requests = await _requestService.GetAllRequestsAsync(requestStatus);
            var dtos = requests.Select(r => new Fintech.Application.DTOs.CollectionRequestDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                InstallmentId = r.InstallmentId,
                InstallmentCode = r.Installment?.InstallmentCode,
                InstallmentNo = r.Installment?.No ?? 0,
                LoanCaseId = r.LoanCaseId,
                LoanCode = r.LoanCase?.LoanCode,
                CustomerName = r.LoanCase?.Customer?.Name,
                Amount = r.Amount,
                PaymentMode = r.PaymentMode.ToString(),
                UtrRef = r.UtrRef,
                Remarks = r.Remarks,
                Status = r.Status.ToString(),
                RequestedById = r.RequestedById,
                RequestedByName = r.RequestedBy?.Name,
                RequestedAt = r.RequestedAt,
                ApprovedById = r.ApprovedById,
                ApprovedByName = r.ApprovedBy?.Name,
                ApprovedAt = r.ApprovedAt,
                PreviousDueAmount = r.PreviousDueAmount,
                NewDueAmount = r.NewDueAmount
            }).ToList();
            return Ok(new { success = true, data = dtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAll collection requests");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while retrieving requests.", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var request = await _requestService.GetByIdAsync(id);
            if (request == null)
            {
                return NotFound(new { success = false, message = "Collection request not found" });
            }
            var dto = new Fintech.Application.DTOs.CollectionRequestDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                InstallmentId = request.InstallmentId,
                InstallmentCode = request.Installment?.InstallmentCode,
                InstallmentNo = request.Installment?.No ?? 0,
                LoanCaseId = request.LoanCaseId,
                LoanCode = request.LoanCase?.LoanCode,
                CustomerName = request.LoanCase?.Customer?.Name,
                Amount = request.Amount,
                PaymentMode = request.PaymentMode.ToString(),
                UtrRef = request.UtrRef,
                Remarks = request.Remarks,
                Status = request.Status.ToString(),
                RequestedById = request.RequestedById,
                RequestedByName = request.RequestedBy?.Name,
                RequestedAt = request.RequestedAt,
                ApprovedById = request.ApprovedById,
                ApprovedByName = request.ApprovedBy?.Name,
                ApprovedAt = request.ApprovedAt,
                PreviousDueAmount = request.PreviousDueAmount,
                NewDueAmount = request.NewDueAmount
            };
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById collection request");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while retrieving the request.", error = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            // Security: Enforce that only Admin/Super Admin can approve
            var role = _tenantService.UserRole;
            if (role != "super_admin" && role != "branch_manager")
            {
                return StatusCode(403, new { success = false, message = "Access Denied: Only Admins can approve collection requests." });
            }

            var approvedById = _tenantService.UserId;
            var request = await _requestService.ApproveRequestAsync(id, approvedById);

            return Ok(new
            {
                success = true,
                message = $"Collection request {request.RequestNumber} approved successfully.",
                data = request
            });
        }
        catch (FinVedaException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Approve collection request");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while approving the request.", error = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            // Security: Enforce that only Admin/Super Admin can reject
            var role = _tenantService.UserRole;
            if (role != "super_admin" && role != "branch_manager")
            {
                return StatusCode(403, new { success = false, message = "Access Denied: Only Admins can reject collection requests." });
            }

            var approvedById = _tenantService.UserId;
            var request = await _requestService.RejectRequestAsync(id, approvedById);

            return Ok(new
            {
                success = true,
                message = $"Collection request {request.RequestNumber} rejected.",
                data = request
            });
        }
        catch (FinVedaException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reject collection request");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while rejecting the request.", error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var requestedById = _tenantService.UserId;
            var request = await _requestService.CancelRequestAsync(id, requestedById);

            return Ok(new
            {
                success = true,
                message = $"Collection request {request.RequestNumber} cancelled successfully.",
                data = request
            });
        }
        catch (FinVedaException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Cancel collection request");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred while cancelling the request.", error = ex.Message });
        }
    }
}
