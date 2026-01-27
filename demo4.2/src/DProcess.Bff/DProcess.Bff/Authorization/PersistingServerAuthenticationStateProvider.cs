using System;
using System.Security.Claims;
using DProcess.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;

namespace DProcess.Bff.Authorization;

// This service is responsible for persisting the authentication state to the client
// so that the client (WASM) can start with the same user state without calling an API immediately.
public class PersistingServerAuthenticationStateProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _state = state;
        _authenticationStateProvider = authenticationStateProvider;

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
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("email")?.Value
                        ?? principal.FindFirst("preferred_username")?.Value;

            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(email))
            {
                var roles = principal.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Concat(principal.FindAll("roles").Select(c => c.Value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var permissions = principal.FindAll("permission")
                    .Select(c => c.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var userInfo = new UserInfo
                {
                    UserId = userId,
                    Email = email,
                    Roles = roles,
                    Permissions = permissions
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
