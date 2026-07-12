using AutoMapper;
using Fintech.Application.Services;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Infrastructure.Security;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IMapper mapper, ILogger<UsersController> logger)
    {
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }

    [HasPermission(Permission.MANAGE_USERS)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegisterRequest request)
    {
        try
        {
            var user = _mapper.Map<User>(request);
            var result = await _userService.CreateAsync(user, request.Password);
            
            var dto = _mapper.Map<UserDto>(result);
            dto.Code = result.UserCode ?? string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating the user.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            
            var dtos = users.Select(u => 
            {
                var dto = _mapper.Map<UserDto>(u);
                dto.Code = u.UserCode ?? string.Empty;
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
                Message = "An unexpected error occurred while retrieving users.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            var dto = _mapper.Map<UserDto>(user);
            dto.Code = user.UserCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetById");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the user.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get user by business code
    /// </summary>
    /// <param name="code">User code (e.g., USR0001)</param>
    [HasPermission(Permission.VIEW_USERS)]
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var user = await _userService.GetByCodeAsync(code);
            if (user == null)
                return NotFound(new { message = $"User with code {code} not found" });
            
            var dto = _mapper.Map<UserDto>(user);
            dto.Code = user.UserCode ?? string.Empty;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByCode");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the user by code.",
                Error = ex.Message
            });
        }
    }

    [HasPermission(Permission.MANAGE_USERS)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserDto userDto)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            _mapper.Map(userDto, user);
            await _userService.UpdateAsync(user);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while updating the user.",
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
            await _userService.DeleteAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the user.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Deactivate (soft-delete) an employee/user account
    /// </summary>
    [HasPermission(Permission.MANAGE_USERS)]
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            var success = await _userService.DeactivateAsync(id);
            if (!success)
                return NotFound(new { success = false, message = "User not found" });

            return Ok(new { success = true, message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Deactivate");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deactivating the user.",
                Error = ex.Message
            });
        }
    }
}
