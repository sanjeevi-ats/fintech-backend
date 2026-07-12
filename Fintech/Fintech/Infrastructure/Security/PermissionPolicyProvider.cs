using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Fintech.Core.Domain;

namespace Fintech.Infrastructure.Security;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix))
        {
            var permissionName = policyName.Substring(HasPermissionAttribute.PolicyPrefix.Length);
            
            if (Enum.TryParse<Permission>(permissionName, true, out var permission))
            {
                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new PermissionRequirement(permission));
                return policy.Build();
            }
        }

        return await base.GetPolicyAsync(policyName);
    }
}
