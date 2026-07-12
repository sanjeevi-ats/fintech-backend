using Microsoft.AspNetCore.Authorization;
using Fintech.Core.Domain;

namespace Fintech.Infrastructure.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }

    public PermissionRequirement(Permission permission)
    {
        Permission = permission;
    }
}
