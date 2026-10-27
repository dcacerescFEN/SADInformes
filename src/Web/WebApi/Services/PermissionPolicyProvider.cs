using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WebApi.Authentication;

namespace WebApi.Services;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options, PublicKeycloakConfiguration config)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null || !policyName.StartsWith("REPORT_", StringComparison.Ordinal)) return policy;
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                KeycloakClaimsMapper.HasPermissionOrGlobalAccess(context.User, policyName, config.ClientId))
            .Build();
    }
}
