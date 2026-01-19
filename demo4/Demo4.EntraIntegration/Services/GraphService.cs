using Demo4.EntraIntegration.Client.Services;
using Demo4.EntraIntegration.Data;
using Demo4.EntraIntegration.Shared.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Configuration;
using System.Security.Claims;

namespace Demo4.EntraIntegration.Services;

/// <summary>
/// Implementation of Microsoft Graph API service using IDownstreamApi
/// Calls Graph API on behalf of the authenticated user (OBO flow)
/// </summary>
public class GraphService : IGraphService
{
    private readonly IDownstreamApi _downstreamApi;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphService> _logger;

    private const string EntraAuthenticationScheme = "MicrosoftEntra";

    public GraphService(
        IDownstreamApi downstreamApi, 
        IHttpContextAccessor httpContextAccessor, 
        UserManager<ApplicationUser> userManager,
        ILogger<GraphService> logger, 
        IConfiguration configuration)
    {
        _downstreamApi = downstreamApi;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Graph profile requested but no authenticated user principal is available");
                return null;
            }

            _logger.LogInformation(
                "Graph token context: authType={AuthType}, oid={Oid}, tid={Tid}, uid={Uid}, utid={Utid}, msal_account_id={MsalAccountId}, preferred_username={PreferredUsername}, login_hint={LoginHint}",
                user.Identity?.AuthenticationType,
                user.FindFirst("oid")?.Value,
                user.FindFirst("tid")?.Value,
                user.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value,
                user.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value,
                user.FindFirst("msal_account_id")?.Value ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/msal_account_id")?.Value,
                user.FindFirst(ClaimConstants.PreferredUserName)?.Value,
                user.GetLoginHint());

            var scopes = _configuration.GetSection("DownstreamApi:Scopes").Get<string[]>();
            if (scopes is null || scopes.Length == 0)
            {
                var scopesValue = _configuration["DownstreamApi:Scopes"];
                scopes = scopesValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            }

            var result = await _downstreamApi.GetForUserAsync<UserProfile>(
                "DownstreamApi",
                options =>
                {
                    options.RelativePath = "/me";
                    options.Scopes = scopes;
                    options.AcquireTokenOptions.AuthenticationOptionsName = EntraAuthenticationScheme;
                },
                user: user);

            _logger.LogInformation("Successfully fetched user profile from Microsoft Graph");
            return result;
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            // Let the API layer convert this to a challenge response.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user profile from Microsoft Graph");
            return null;
        }
    }

    public async Task SyncUserProfileToLocalAsync(string userId)
    {
        _logger.LogInformation("Syncing Graph profile for user ID: {UserId}", userId);

        var dbUser = await _userManager.FindByIdAsync(userId);
        if (dbUser == null)
        {
            _logger.LogWarning("Cannot sync profile: user with ID {UserId} not found in database", userId);
            return;
        }

        var profile = await GetUserProfileAsync();
        if (profile == null)
        {
            _logger.LogWarning("Failed to retrieve profile from Graph for user {Email}", dbUser.Email);
            return;
        }

        _logger.LogInformation("Updating local user record with Graph data for {Email}", dbUser.Email);

        dbUser.DisplayName = profile.DisplayName ?? dbUser.DisplayName;
        dbUser.JobTitle = profile.JobTitle;
        dbUser.Department = profile.Department;
        dbUser.OfficeLocation = profile.OfficeLocation;
        dbUser.MobilePhone = profile.MobilePhone;
        dbUser.LastGraphSync = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(dbUser);
        if (result.Succeeded)
        {
            _logger.LogInformation("Successfully synchronized Graph profile for user {Email}", dbUser.Email);
        }
        else
        {
            _logger.LogError("Failed to update user profile in database: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<byte[]?> GetUserPhotoAsync()
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Graph photo requested but no authenticated user principal is available");
                return null;
            }

            _logger.LogInformation(
                "Graph token context (photo): authType={AuthType}, oid={Oid}, tid={Tid}, uid={Uid}, utid={Utid}, msal_account_id={MsalAccountId}, preferred_username={PreferredUsername}, login_hint={LoginHint}",
                user.Identity?.AuthenticationType,
                user.FindFirst("oid")?.Value,
                user.FindFirst("tid")?.Value,
                user.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value,
                user.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value,
                user.FindFirst("msal_account_id")?.Value ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/msal_account_id")?.Value,
                user.FindFirst(ClaimConstants.PreferredUserName)?.Value,
                user.GetLoginHint());

            using var response = await _downstreamApi.GetForUserAsync<HttpResponseMessage>(
                "DownstreamApi",
                options =>
                {
                    options.RelativePath = "me/photo/$value";
                    options.AcquireTokenOptions.AuthenticationOptionsName = EntraAuthenticationScheme;
                },
                user: user);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var photoBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Successfully fetched user photo from Microsoft Graph ({Size} bytes)", photoBytes.Length);
                return photoBytes;
            }

            _logger.LogWarning("User photo not available (Status: {StatusCode})", response?.StatusCode);
            return null;
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            // Let the API layer convert this to a challenge response.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user photo from Microsoft Graph");
            return null;
        }
    }
}
