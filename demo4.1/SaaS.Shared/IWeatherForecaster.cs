namespace SaaS.Shared;

public interface IWeatherForecaster
{
    Task<IReadOnlyList<WeatherForecast>> GetWeatherAsync(CancellationToken cancellationToken = default);
}

