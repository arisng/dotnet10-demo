using Demo4.EntraIntegration.Data;
using Demo4.EntraIntegration.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Demo4.EntraIntegration.Authorization;

public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PermissionClaimsTransformation> _logger;

    public PermissionClaimsTransformation(
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<PermissionClaimsTransformation> logger)
    {
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Check if transformation already applied
        if (principal.HasClaim(c => c.Type == "permissions_loaded"))
            return principal;

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

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
                // Provisioning may have failed or not run yet.
                // Still add token-acquisition hints so Graph OBO can work for Entra-authenticated sessions.
                _logger.LogWarning("Entra user with OID {Oid} not found in database. Skipping permissions but enriching principal for token acquisition.", oid);

                var tidFallback = principal.FindFirstValue("tid")
                                  ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

                var preferredUsernameFallback = principal.FindFirstValue("preferred_username")
                                                ?? principal.FindFirstValue(ClaimTypes.Upn)
                                                ?? principal.FindFirstValue(ClaimTypes.Email);

                if (!string.IsNullOrWhiteSpace(tidFallback) && !identity.HasClaim(c => c.Type == "tid"))
                {
                    identity.AddClaim(new Claim("tid", tidFallback));
                }

                if (!string.IsNullOrWhiteSpace(preferredUsernameFallback) && !identity.HasClaim(c => c.Type == "preferred_username"))
                {
                    identity.AddClaim(new Claim("preferred_username", preferredUsernameFallback));
                }

                if (!identity.HasClaim(c => c.Type == "msal_account_id"))
                {
                    var msalAccountId = principal.GetMsalAccountId();
                    if (string.IsNullOrWhiteSpace(msalAccountId) && !string.IsNullOrWhiteSpace(oid) && !string.IsNullOrWhiteSpace(tidFallback))
                    {
                        msalAccountId = $"{oid}.{tidFallback}";
                    }

                    if (!string.IsNullOrWhiteSpace(msalAccountId))
                    {
                        identity.AddClaim(new Claim("msal_account_id", msalAccountId));
                    }
                }

                return clone;
            }

            userId = user.Id;
            _logger.LogDebug("Processing Entra ID user: {Email} (OID: {Oid})", user.Email, oid);

            // Ensure token-acquisition-required hints exist in the principal.
            // Without these, Microsoft.Identity.Web can fail with user_null (no account/login hint)
            // when trying to AcquireTokenSilent.
            var tid = principal.FindFirstValue("tid")
                      ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid")
                      ?? (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "tid")?.Value;

            var preferredUsername = principal.FindFirstValue("preferred_username")
                                    ?? principal.FindFirstValue(ClaimTypes.Upn)
                                    ?? principal.FindFirstValue(ClaimTypes.Email)
                                    ?? user.Email;

            if (!string.IsNullOrWhiteSpace(tid) && !identity.HasClaim(c => c.Type == "tid"))
            {
                identity.AddClaim(new Claim("tid", tid));
            }

            if (!string.IsNullOrWhiteSpace(preferredUsername) && !identity.HasClaim(c => c.Type == "preferred_username"))
            {
                identity.AddClaim(new Claim("preferred_username", preferredUsername));
            }

            if (!identity.HasClaim(c => c.Type == "msal_account_id"))
            {
                var msalAccountId = principal.GetMsalAccountId();
                if (string.IsNullOrWhiteSpace(msalAccountId) && !string.IsNullOrWhiteSpace(oid) && !string.IsNullOrWhiteSpace(tid))
                {
                    msalAccountId = $"{oid}.{tid}";
                }

                if (!string.IsNullOrWhiteSpace(msalAccountId))
                {
                    identity.AddClaim(new Claim("msal_account_id", msalAccountId));
                }
            }
        }
        else
        {
            // Local Identity user
            userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            
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
