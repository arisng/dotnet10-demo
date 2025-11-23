using System.Security.Claims;
using Demo4.EntraIntegration.Services;
using Microsoft.AspNetCore.Authentication;

namespace Demo4.EntraIntegration.Authorization;

public class PermissionClaimsTransformation(IPermissionService permissionService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return principal;

        var permissions = await permissionService.GetUserPermissionsAsync(userId);
        
        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;
        
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }
        
        Console.WriteLine($"[PermissionClaimsTransformation] Added {permissions.Count()} permissions for user {userId}");

        return clone;
    }
}
