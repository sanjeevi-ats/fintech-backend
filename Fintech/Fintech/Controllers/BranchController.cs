using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Fintech.Infrastructure.Security;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Route("api/v1/[controller]")]
public class BranchController : BaseApiController
{
    private readonly IBranchService _branchService;
    private readonly ILogger<BranchController> _logger;

    public BranchController(IBranchService branchService, ILogger<BranchController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _branchService.GetAllAsync();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAll");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving branches.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _branchService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the branch.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get branch by business code
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var result = await _branchService.GetByCodeAsync(code);
            if (result == null)
                return NotFound(new { message = $"Branch with code {code} not found" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the branch by code.",
                Error = ex.Message
            });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        try
        {
            var result = await _branchService.SearchAsync(query);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Search");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred during branch search.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_BRANCHES)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Branch branch)
    {
        try
        {
            var result = await _branchService.CreateAsync(branch);
            
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the branch.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_BRANCHES)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Branch branch)
    {
        try
        {
            if (id != branch.Id)
            {
                return BadRequest();
            }
            
            await _branchService.UpdateAsync(branch);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating the branch.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_BRANCHES)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _branchService.DeleteAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the branch.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_BRANCHES)]
    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] string settingsJson)
    {
        try
        {
            await _branchService.UpdateSettingsAsync(id, settingsJson);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateSettings");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating branch settings.",
                Error = ex.Message
            });
        }
    }
}
