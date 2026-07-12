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
public class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;
    private readonly IMapper _mapper;
    private readonly ILogger<PartnersController> _logger;

    public PartnersController(IPartnerService partnerService, IMapper mapper, ILogger<PartnersController> logger)
    {
        _partnerService = partnerService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PartnerRequest request)
    {
        try
        {
            var partner = _mapper.Map<Partner>(request);
            var result = await _partnerService.CreateAsync(partner);
            
            var dto = _mapper.Map<PartnerDto>(result);
            dto.Code = result.PartnerCode ?? string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the partner.",
                Error = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PartnerDto>>> GetAll()
    {
        try
        {
            var partners = await _partnerService.GetAllAsync();
            
            var dtos = partners.Select(p =>
            {
                var dto = _mapper.Map<PartnerDto>(p);
                dto.Code = p.PartnerCode ?? string.Empty;
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
                Message = "An unexpected error occurred while retrieving partners.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PartnerDto>> GetById(Guid id)
    {
        try
        {
            var partner = await _partnerService.GetByIdAsync(id);
            if (partner == null)
            {
                return NotFound();
            }
            
            var dto = _mapper.Map<PartnerDto>(partner);
            dto.Code = partner.PartnerCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the partner.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get partner by business code
    /// </summary>
    /// <param name="code">Partner code (e.g., PAR0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var partner = await _partnerService.GetByCodeAsync(code);
            if (partner == null)
                return NotFound(new { message = $"Partner with code {code} not found" });
            
            var dto = _mapper.Map<PartnerDto>(partner);
            dto.Code = partner.PartnerCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the partner by code.",
                Error = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PartnerRequest request)
    {
        try
        {
            var partner = await _partnerService.GetByIdAsync(id);
            if (partner == null)
            {
                return NotFound();
            }
            
            _mapper.Map(request, partner);
            await _partnerService.UpdateAsync(partner);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating the partner.",
                Error = ex.Message
            });
        }
    }
}
