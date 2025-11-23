using System.Security.Claims;
using Demo3.BffRbac.Client.Models;
using Demo3.BffRbac.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Demo3.BffRbac.Components.Account;

// This service is responsible for persisting the authentication state to the client
// so that the client (WASM) can start with the same user state without calling an API immediately.
public class PersistingServerAuthenticationStateProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IPermissionService _permissionService;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        AuthenticationStateProvider authenticationStateProvider,
        IPermissionService permissionService)
    {
        _state = state;
        _authenticationStateProvider = authenticationStateProvider;
        _permissionService = permissionService;

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
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (userId != null && email != null)
            {
                var permissions = await _permissionService.GetUserPermissionsAsync(userId);
                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                var userInfo = new UserInfo
                {
                    UserId = userId,
                    Email = email,
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
