using Microsoft.AspNetCore.Authorization;

namespace Demo5_1.ApiService.Authorization;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Enforces the "Outer Lock" (access_as_user scope) AND "Inner Lock" (local permission).
    /// </summary>
    public static RouteHandlerBuilder RequireApiPermission(
        this RouteHandlerBuilder builder, string permission)
    {
        return builder.RequireAuthorization("Api.Access", permission);
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
