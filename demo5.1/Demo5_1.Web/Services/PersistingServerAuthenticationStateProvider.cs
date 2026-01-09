using System.Security.Claims;
using Demo5_1.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Demo5_1.Web.Services;

public class PersistingServerAuthenticationStateProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IDownstreamApi _downstreamApi;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        AuthenticationStateProvider authenticationStateProvider,
        IDownstreamApi downstreamApi)
    {
        _state = state;
        _authenticationStateProvider = authenticationStateProvider;
        _downstreamApi = downstreamApi;

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
            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("preferred_username")?.Value;

            if (userId != null && email != null)
            {
                // Fetch permissions from ApiService using the user's token (OBO/Cookie exchange handled by IDownstreamApi)
                List<string> permissions = [];
                try 
                {
                    permissions = await _downstreamApi.GetForUserAsync<List<string>>("ApiService", 
                        options => options.RelativePath = "/api/users/permissions") ?? [];
                }
                catch (Exception)
                {
                    // Fallback or log. If API is down, we have no permissions.
                }

                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

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
