using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Fintech.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected Guid BranchId
    {
        get
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return Guid.TryParse(branchIdClaim, out var id) ? id : Guid.Empty;
        }
    }

    protected Guid UserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
        }
    }
}
