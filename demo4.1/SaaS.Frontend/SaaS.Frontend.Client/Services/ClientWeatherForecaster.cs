using SaaS.Shared;
using System.Net.Http.Json;

namespace SaaS.Frontend.Client.Services;

public sealed class ClientWeatherForecaster(HttpClient httpClient) : IWeatherForecaster
{
    public async Task<IReadOnlyList<WeatherForecast>> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<WeatherForecast>>(
                   "/api/proxy/weather/weather-forecast",
                   cancellationToken)
               ?? [];
    }
}

