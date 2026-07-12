using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Features.Stubs;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Fintech.Application.Features.Auth;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

/// <summary>
/// Authentication Controller - Handles user authentication and token management
/// All actions are automatically logged to Auth/log-yyyy-MM-dd.txt via AutoLog attribute
/// </summary>
[AutoLog]
public class AuthController : BaseApiController
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginUserCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoginAsync");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred during login.",
                Error = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterUserCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok(new { id = result, message = "User registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterAsync");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred during registration.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RefreshTokenAsync");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred during token refresh.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("enable-totp")]
    public async Task<IActionResult> EnableTotpAsync([FromBody] EnableTotpCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EnableTotpAsync");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while enabling TOTP.",
                Error = ex.Message
            });
        }
    }

    [AutoLog(logRequestBody: false)]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChangePasswordAsync");
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while changing password.",
                Error = ex.Message
            });
        }
    }
}
