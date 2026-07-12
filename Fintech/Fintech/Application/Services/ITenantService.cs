using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Fintech.Application.Services;

public interface ITenantService
{
    Guid BranchId { get; }
    Guid UserId { get; }
    string UserRole { get; }
}

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid BranchId
    {
        get
        {
            var branchIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("BranchId")?.Value;
            if (Guid.TryParse(branchIdClaim, out Guid branchId))
            {
                return branchId;
            }
            return Guid.Empty;
        }
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }

    public string UserRole
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
