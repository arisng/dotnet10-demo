using Microsoft.Identity.Web;
using SaaS.Shared;
using System.Net.Http.Headers;

namespace SaaS.Frontend.Services;

public sealed class ServerWeatherForecaster(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ITokenAcquisition tokenAcquisition) : IWeatherForecaster
{
    public async Task<IReadOnlyList<WeatherForecast>> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        var scopes = configuration.GetSection("DownstreamApis:WeatherApi:Scopes").Get<string[]>();
        if (scopes is null || scopes.Length == 0)
        {
            var scopesValue = configuration["DownstreamApis:WeatherApi:Scopes"];
            scopes = scopesValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        var httpClient = httpClientFactory.CreateClient("WeatherApi");

        using var request = new HttpRequestMessage(HttpMethod.Get, "weather-forecast");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<WeatherForecast>>(cancellationToken: cancellationToken)
            ?? [];
    }
}
