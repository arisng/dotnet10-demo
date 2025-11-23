using Microsoft.AspNetCore.Authorization;

namespace Demo4.EntraIntegration.Authorization;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder, string permission)
    {
        var requirement = new PermissionRequirement(permission);
        return builder.RequireAuthorization(
            new AuthorizationPolicyBuilder()
                .AddRequirements(requirement)
                .Build());
    }
}
