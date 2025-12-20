using Demo4.EntraIntegration.Shared.Models;

namespace Demo4.EntraIntegration.Client.Services;

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

public interface IGraphService
{
    Task<UserProfile?> GetUserProfileAsync();
    Task<byte[]?> GetUserPhotoAsync();
}
