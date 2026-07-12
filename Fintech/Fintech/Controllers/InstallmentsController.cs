using AutoMapper;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

using Microsoft.AspNetCore.Authorization;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentService _installmentService;
    private readonly IMapper _mapper;
    private readonly ILogger<InstallmentsController> _logger;

    public InstallmentsController(IInstallmentService installmentService, IMapper mapper, ILogger<InstallmentsController> logger)
    {
        _installmentService = installmentService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost("generate/{loanId}")]
    public async Task<IActionResult> Generate(Guid loanId, [FromQuery] int count = 12)
    {
        try
        {
            await _installmentService.GenerateInstallmentsAsync(loanId, count);
            
            return Ok(new { message = "Installments generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Generate");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while generating installments.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("due")]
    public async Task<ActionResult<IEnumerable<InstallmentDto>>> GetDue([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
            var installments = await _installmentService.GetDueInstallmentsAsync(from, to);
            
            var dtos = installments.Select(i =>
            {
                var dto = _mapper.Map<InstallmentDto>(i);
                dto.Code = i.InstallmentCode ?? string.Empty;
                return dto;
            }).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDue");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving due installments.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("loan/{loanId}")]
    public async Task<ActionResult<IEnumerable<InstallmentDto>>> GetByLoan(Guid loanId)
    {
        try
        {
            var installments = await _installmentService.GetByLoanIdAsync(loanId);
            
            var dtos = installments.Select(i =>
            {
                var dto = _mapper.Map<InstallmentDto>(i);
                dto.Code = i.InstallmentCode ?? string.Empty;
                return dto;
            }).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByLoan");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving loan installments.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get installment by business code
    /// </summary>
    /// <param name="code">Installment code (e.g., INST0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var installment = await _installmentService.GetByCodeAsync(code);
            if (installment == null)
                return NotFound(new { message = $"Installment with code {code} not found" });
            
            var dto = _mapper.Map<InstallmentDto>(installment);
            dto.Code = installment.InstallmentCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the installment by code.",
                Error = ex.Message
            });
        }
    }
}
