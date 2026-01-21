using System.Security.Claims;
using Demo4.EntraIntegration.Data;
using Demo4.EntraIntegration.Shared.Models;
using Demo4.EntraIntegration.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Demo4.EntraIntegration.Authorization;

// This service is responsible for persisting the authentication state to the client
// so that the client (WASM) can start with the same user state without calling an API immediately.
public class PersistingServerAuthenticationStateProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        AuthenticationStateProvider authenticationStateProvider,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _state = state;
        _authenticationStateProvider = authenticationStateProvider;
        _permissionService = permissionService;
        _userManager = userManager;

        _subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
        {
            _authenticationStateTask = _authenticationStateProvider.GetAuthenticationStateAsync();
        }

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            // In this app, Blazor's AuthenticationState is based on the Identity application cookie.
            // That cookie principal may not include all OIDC claims (like oid/tid) even for Entra logins.
            // So we always resolve the local ApplicationUser first, then derive Entra identifiers from it.
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                user = await _userManager.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }

            var oid = principal.GetObjectId() ?? user?.EntraObjectId;
            var isEntraUser = !string.IsNullOrWhiteSpace(oid) || user?.ExternalAuthenticationProvider == OpenIdConnectDefaults.AuthenticationScheme;

            var tid = principal.FindFirst("tid")?.Value
                      ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            if (user != null && string.IsNullOrWhiteSpace(tid))
            {
                var userClaims = await _userManager.GetClaimsAsync(user);
                tid = userClaims.FirstOrDefault(c => c.Type == "tid")?.Value
                      ?? userClaims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            }

            var email = principal.FindFirstValue(ClaimTypes.Email)
                        ?? principal.FindFirstValue("preferred_username")
                        ?? principal.FindFirstValue(ClaimTypes.Upn);

            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(email))
            {
                var permissions = await _permissionService.GetUserPermissionsAsync(userId);

                // Roles might come as ClaimTypes.Role or raw "roles" depending on mapping.
                var roles = principal.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Concat(principal.FindAll("roles").Select(c => c.Value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var userInfo = new UserInfo
                {
                    UserId = isEntraUser ? (oid ?? userId) : userId,
                    Email = email,
                    AuthProvider = isEntraUser ? "entra" : "local",
                    EntraObjectId = oid,
                    EntraTenantId = tid,
                    Roles = roles,
                    Permissions = permissions.ToList()
                };

                _state.PersistAsJson(nameof(UserInfo), userInfo);
            }
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
