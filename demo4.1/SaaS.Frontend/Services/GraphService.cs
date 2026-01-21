using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using SaaS.Shared;
using System.Net.Http.Json;

namespace SaaS.Frontend.Services;

public sealed class GraphService(
    IDownstreamApi downstreamApi,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<GraphService> logger) : IGraphProfileService
{
    public async Task<GraphUserProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Graph profile requested but no authenticated user principal is available");
            return null;
        }

        try
        {
            var scopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>();
            if (scopes is null || scopes.Length == 0)
            {
                var scopesValue = configuration["DownstreamApis:MicrosoftGraph:Scopes"];
                scopes = scopesValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            }

            // Use IDownstreamApi so Microsoft.Identity.Web can:
            // - acquire the correct token for Graph
            // - (when possible) surface claims challenges as MicrosoftIdentityWebChallengeUserException
            return await downstreamApi.GetForUserAsync<GraphUserProfile>(
                "MicrosoftGraph",
                options =>
                {
                    options.RelativePath = "/me?$select=id,displayName,mail,userPrincipalName";
                    options.Scopes = scopes;
                    options.AcquireTokenOptions.AuthenticationOptionsName = OpenIdConnectDefaults.AuthenticationScheme;
                },
                user: user,
                cancellationToken: cancellationToken);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            // Let the API layer convert this to an interactive challenge.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user profile from Microsoft Graph");
            return null;
        }
    }
}
