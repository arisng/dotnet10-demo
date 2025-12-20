using System.Security.Claims;
using Demo4.EntraIntegration.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Demo4.EntraIntegration.Client.Services;

public class PersistentAuthenticationStateProvider(PersistentComponentState state) : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            return _unauthenticatedTask;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.Email),
            new Claim(ClaimTypes.Email, userInfo.Email)
        };

        if (!string.IsNullOrWhiteSpace(userInfo.AuthProvider))
        {
            claims.Add(new Claim("auth_provider", userInfo.AuthProvider));
        }

        if (!string.IsNullOrWhiteSpace(userInfo.EntraObjectId))
        {
            claims.Add(new Claim("oid", userInfo.EntraObjectId));
        }

        if (!string.IsNullOrWhiteSpace(userInfo.EntraTenantId))
        {
            claims.Add(new Claim("tid", userInfo.EntraTenantId));
        }

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in userInfo.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "PersistentAuthentication");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
