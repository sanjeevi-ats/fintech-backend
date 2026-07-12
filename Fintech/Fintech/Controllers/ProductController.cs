using AutoMapper;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Authorize(Roles = "super_admin,branch_manager")]
[Route("api/v1/[controller]")]
public class ProductController : BaseApiController
{
    private readonly ILoanProductService _loanProductService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ILoanProductService loanProductService, IMapper mapper, ILogger<ProductController> logger)
    {
        _loanProductService = loanProductService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveProducts()
    {
        try
        {
            var products = await _loanProductService.GetActiveProductsAsync();
            
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetActiveProducts");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving active loan products.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get loan product by business code
    /// </summary>
    /// <param name="code">Product code (e.g., PRO0001)</param>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var product = await _loanProductService.GetByCodeAsync(code);
            if (product == null)
                return NotFound(new { message = $"Product with code {code} not found" });
            
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the product by code.",
                Error = ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LoanProduct product)
    {
        try
        {
            var result = await _loanProductService.CreateProductAsync(product);
            
            return Created("", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the loan product.",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await _loanProductService.DeactivateProductAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Deactivate");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deactivating the loan product.",
                Error = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LoanProduct product)
    {
        try
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "Product ID mismatch" });
            }
            await _loanProductService.UpdateProductAsync(product);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating the loan product.",
                Error = ex.Message
            });
        }
    }
}
