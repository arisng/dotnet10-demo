using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace SaaS.Frontend.Services;

public sealed class GraphService(
    //IDownstreamApi downstreamApi
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ITokenAcquisition tokenAcquisition) : IGraphService
{
    public async Task<GraphUserProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        //using var response = await downstreamApi.CallApiForUserAsync(
        //    "MicrosoftGraph",
        //    options =>
        //    {
        //        options.HttpMethod = HttpMethod.Get.Method;
        //        // With BaseUrl set to https://graph.microsoft.com/v1.0/ (note trailing slash),
        //        // keep RelativePath as a relative URL so it appends under /v1.0/.
        //        options.RelativePath = "me?$select=id,displayName,mail,userPrincipalName";
        //    },
        //    cancellationToken: cancellationToken);

        var scopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>();
        if (scopes is null || scopes.Length == 0)
        {
            var scopesValue = configuration["DownstreamApis:MicrosoftGraph:Scopes"];
            scopes = scopesValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        var httpClient = httpClientFactory.CreateClient("MicrosoftGraph");

        using var request = new HttpRequestMessage(HttpMethod.Get, "me?$select=id,displayName,mail,userPrincipalName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GraphUserProfile>(cancellationToken: cancellationToken);
    }
}
