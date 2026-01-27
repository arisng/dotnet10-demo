using Microsoft.AspNetCore.Authorization;

namespace DProcess.Api.Authorization;

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

    public static RouteHandlerBuilder RequireScope(
        this RouteHandlerBuilder builder, string scope)
    {
        var requirement = new ScopeRequirement(scope);
        return builder.RequireAuthorization(
            new AuthorizationPolicyBuilder()
                .AddRequirements(requirement)
                .Build());
    }
}
