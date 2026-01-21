using SaaS.Shared;
using System.Net;
using System.Net.Http.Json;

namespace SaaS.Frontend.Client.Services;

public sealed class ClientGraphProfileService(HttpClient httpClient) : IGraphProfileService
{
    public async Task<GraphUserProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("/api/graph/me", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GraphUserProfile>(cancellationToken: cancellationToken);
    }
}
