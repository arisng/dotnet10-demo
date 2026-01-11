using Microsoft.AspNetCore.Authorization;

namespace Demo5_1.ApiService.Authorization;

public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        // Check for 'scp' claim
        var scopeClaim = context.User.FindFirst("scp");
        
        // Entra uses 'scp', many OAuth servers use 'scope'
        // But per requirements, we only look for 'scp' here.
        if (scopeClaim != null)
        {
            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (scopes.Contains(requirement.Scope, StringComparer.Ordinal))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
