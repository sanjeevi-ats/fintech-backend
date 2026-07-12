using Microsoft.AspNetCore.Authorization;
using Fintech.Core.Domain;

namespace Fintech.Infrastructure.Security;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(Permission permission)
    {
        Policy = $"{PolicyPrefix}{permission}";
    }
}
