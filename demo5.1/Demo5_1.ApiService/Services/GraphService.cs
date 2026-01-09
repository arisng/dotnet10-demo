using Microsoft.Identity.Abstractions;

namespace Demo5_1.ApiService.Services;

/// <summary>
/// Implementation of Microsoft Graph API service using IDownstreamApi
/// Calls Graph API on behalf of the authenticated user (OBO flow)
/// </summary>
public class GraphService : IGraphService
{
    private readonly IDownstreamApi _downstreamApi;
    private readonly ILogger<GraphService> _logger;

    public GraphService(IDownstreamApi downstreamApi, ILogger<GraphService> logger)
    {
        _downstreamApi = downstreamApi;
        _logger = logger;
    }

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        try
        {
            var result = await _downstreamApi.GetForUserAsync<UserProfile>(
                "MicrosoftGraph",
                options =>
                {
                    options.RelativePath = "me";
                });

            _logger.LogInformation("Successfully fetched user profile from Microsoft Graph");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user profile from Microsoft Graph");
            return null;
        }
    }

    public async Task<byte[]?> GetUserPhotoAsync()
    {
        try
        {
            using var response = await _downstreamApi.GetForUserAsync<HttpResponseMessage>(
                "MicrosoftGraph",
                options =>
                {
                    options.RelativePath = "me/photo/$value";
                });

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var photoBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Successfully fetched user photo from Microsoft Graph ({Size} bytes)", photoBytes.Length);
                return photoBytes;
            }

            _logger.LogWarning("User photo not available (Status: {StatusCode})", response?.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user photo from Microsoft Graph");
            return null;
        }
    }
}
