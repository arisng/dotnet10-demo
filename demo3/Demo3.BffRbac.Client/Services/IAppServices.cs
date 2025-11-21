using Demo3.BffRbac.Client.Models;

namespace Demo3.BffRbac.Client.Services;

public interface IWeatherService
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
