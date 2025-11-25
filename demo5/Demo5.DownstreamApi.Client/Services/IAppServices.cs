using Demo5.DownstreamApi.Shared.Models;

namespace Demo5.DownstreamApi.Client.Services;

public interface IWeatherService
{
    Task<WeatherForecast[]> GetForecastAsync();
}

public interface IDownstreamWeatherService
{
    Task<WeatherForecast[]> GetForecastAsync();
}

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto input);
    Task DeleteUserAsync(string id);
}

public interface IReportService
{
    Task<List<ReportDto>> GetReportsAsync();
    Task<byte[]> ExportReportsAsync();
}
