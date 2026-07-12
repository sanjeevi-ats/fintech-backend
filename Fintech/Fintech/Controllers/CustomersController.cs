using AutoMapper;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Infrastructure.Security;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

using Microsoft.AspNetCore.Authorization;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, IMapper mapper, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _mapper = mapper;
        _logger = logger;
    }

    [HasPermission(Permission.MANAGE_USERS)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerRequest request)
    {
        try
        {
            var customer = _mapper.Map<Customer>(request);
            var result = await _customerService.CreateAsync(customer);
            
            var dto = _mapper.Map<CustomerDto>(result);
            dto.Code = result.CustomerCode ?? string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the customer.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        try
        {
            var customers = await _customerService.GetAllAsync();
            
            var dtos = customers.Select(c => 
            {
                var dto = _mapper.Map<CustomerDto>(c);
                dto.Code = c.CustomerCode ?? string.Empty;
                dto.Phone = c.Phone ?? string.Empty;
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
                Message = "An unexpected error occurred while retrieving customers.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Search customers by name, customer code, or phone number
    /// </summary>
    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { success = false, message = "Search query is required" });

            var all = await _customerService.GetAllAsync();
            var query = q.Trim().ToLowerInvariant();

            var matched = all.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLowerInvariant().Contains(query)) ||
                (c.Name != null && c.Name.ToLowerInvariant().Contains(query)) ||
                (c.Phone != null && c.Phone.Contains(query))
            ).Select(c => new CustomerDto
            {
                Id = c.Id,
                Code = c.CustomerCode ?? string.Empty,
                Name = c.Name,
                Phone = c.Phone ?? string.Empty
            }).ToList();

            return Ok(matched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Search");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string phone)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Phone number is required"
                });
            }

            var existing = await _customerService.GetByPhoneAsync(phone);

            var response = new
            {
                exists = existing != null,
                customerId = existing?.Id,
                customerName = existing?.Name
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckDuplicate");

            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while checking for duplicate.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            
            var dto = _mapper.Map<CustomerDto>(customer);
            dto.Code = customer.CustomerCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the customer.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get customer by business code
    /// </summary>
    /// <param name="code">Customer code (e.g., CUS0001)</param>
    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var customer = await _customerService.GetByCodeAsync(code);
            if (customer == null)
                return NotFound(new { message = $"Customer with code {code} not found" });
            
            var dto = _mapper.Map<CustomerDto>(customer);
            dto.Code = customer.CustomerCode ?? string.Empty;
            dto.Phone = customer.Phone ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the customer by code.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_USERS)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CustomerRequest request)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            
            _mapper.Map(request, customer);
            await _customerService.UpdateAsync(customer);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating the customer.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_USERS)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _customerService.DeleteAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the customer.",
                Error = ex.Message
            });
        }
    }
}
