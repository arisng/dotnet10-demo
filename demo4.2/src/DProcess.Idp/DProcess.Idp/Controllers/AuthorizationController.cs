using DProcess.Idp.Data;
using DProcess.Idp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Linq;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DProcess.Idp.Controllers;

[ApiController]
public sealed class AuthorizationController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IPermissionService permissionService;

    public AuthorizationController(UserManager<ApplicationUser> userManager, IPermissionService permissionService)
    {
        this.userManager = userManager;
        this.permissionService = permissionService;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> AuthorizeEndpoint()
    {
        // Manual authentication check to return 302 redirect (not 401)
        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!authenticateResult.Succeeded)
        {
            // Build return URL with all OIDC query parameters preserved
            var returnUrl = $"{HttpContext.Request.Path}{HttpContext.Request.QueryString}";
            return Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request is required.");

        var user = await userManager.GetUserAsync(authenticateResult.Principal)
            ?? throw new InvalidOperationException("Unable to load the authenticated user.");

        var permissions = await permissionService.GetPermissionsAsync(user.Id);

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName ?? user.Email ?? user.Id));

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources("api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(claim.Type switch
            {
                "permission" => new[] { Destinations.AccessToken, Destinations.IdentityToken },
                OpenIddictConstants.Claims.Email => new[] { Destinations.IdentityToken, Destinations.AccessToken },
                OpenIddictConstants.Claims.Name => new[] { Destinations.IdentityToken, Destinations.AccessToken },
                OpenIddictConstants.Claims.Subject => new[] { Destinations.IdentityToken, Destinations.AccessToken },
                _ => new[] { Destinations.AccessToken }
            });
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TokenEndpoint()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request is required.");

        ClaimsPrincipal principal;

        if (request.IsAuthorizationCodeGrantType())
        {
            principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException("Unable to retrieve authorization context.");
        }
        else if (request.IsRefreshTokenGrantType())
        {
            principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException("Unable to retrieve refresh token context.");
        }
        else
        {
            throw new InvalidOperationException("Unsupported grant type.");
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult UserInfo()
    {
        var permissions = User.FindAll("permission").Select(c => c.Value).ToArray();
        return Ok(new
        {
            sub = User.GetClaim(OpenIddictConstants.Claims.Subject),
            name = User.GetClaim(OpenIddictConstants.Claims.Name),
            email = User.GetClaim(OpenIddictConstants.Claims.Email),
            permission = permissions
        });
    }

    [HttpGet("~/connect/endsession")]
    public async Task<IActionResult> LogoutEndpoint()
    {
        // Get the OIDC logout request
        var request = HttpContext.GetOpenIddictServerRequest();

        // Sign out the user from the local Identity cookie
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        // Get the post_logout_redirect_uri from the request
        var redirectUri = request?.PostLogoutRedirectUri;
        
        if (!string.IsNullOrEmpty(redirectUri))
        {
            // Redirect to the registered post_logout_redirect_uri
            // This will typically be the BFF's /signout-callback-oidc endpoint
            return Redirect(redirectUri);
        }

        // Default redirect to root
        return Redirect("/");
    }
}

internal static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal principal, string type)
        => principal.Claims.FirstOrDefault(c => c.Type == type)?.Value;
}
