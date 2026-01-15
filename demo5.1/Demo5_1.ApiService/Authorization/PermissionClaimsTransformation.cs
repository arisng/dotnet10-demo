using Demo5_1.ApiService.Data;
using Demo5_1.ApiService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Demo5_1.ApiService.Authorization;

public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PermissionClaimsTransformation> _logger;

    public PermissionClaimsTransformation(
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ITenantProvider tenantProvider,
        ILogger<PermissionClaimsTransformation> logger)
    {
        _permissionService = permissionService;
        _userManager = userManager;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Check if transformation already applied
        if (principal.HasClaim(c => c.Type == "permissions_loaded"))
            return principal;

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

        // Add Simulated Tenant Claim
        var tenantId = _tenantProvider.GetTenantId();
        if (!string.IsNullOrEmpty(tenantId))
        {
            identity.AddClaim(new Claim("tenant", tenantId));
        }

        ApplicationUser? user = null;
        string? userId = null;

        // Detect authentication source
        string? oid = principal.GetObjectId(); // Entra ID Object ID
        var isEntraUser = !string.IsNullOrEmpty(oid);

        if (isEntraUser && !string.IsNullOrEmpty(oid))
        {
            // Entra ID user - lookup by Object ID
            // NOTE: User provisioning now happens in OIDC OnTokenValidated event (see Program.cs)
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.EntraObjectId == oid);

            if (user == null)
            {
                // This should not happen if OnTokenValidated ran successfully
                _logger.LogWarning("Entra user with OID {Oid} not found in database. Provisioning may have failed.", oid);
                return principal; // Return without permissions
            }

            userId = user.Id;
            _logger.LogDebug("Processing Entra ID user: {Email} (OID: {Oid})", user.Email, oid);
        }
        else
        {
            // Local Identity user
            userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            
            if (!string.IsNullOrEmpty(userId))
            {
                user = await _userManager.FindByIdAsync(userId);
                _logger.LogDebug("Processing local user: {Email}", user?.Email);
            }
        }

        if (user == null || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to resolve user from claims principal");
            return principal;
        }

        // Load permissions (unified for both local and Entra users)
        var permissions = await _permissionService.GetUserPermissionsAsync(userId);

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        // Mark transformation as complete
        identity.AddClaim(new Claim("permissions_loaded", "true"));

        _logger.LogInformation("Added {Count} permissions for user {UserId} ({Provider})",
            permissions.Count(),
            userId,
            isEntraUser ? "Entra" : "Local");

        return clone;
    }
}
