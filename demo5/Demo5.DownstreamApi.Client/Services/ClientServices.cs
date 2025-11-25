using System.Net.Http.Json;
using Demo5.DownstreamApi.Shared.Models;

namespace Demo5.DownstreamApi.Client.Services;

public class ClientWeatherService(HttpClient http) : IWeatherService
{
    public async Task<WeatherForecast[]> GetForecastAsync()
    {
        return await http.GetFromJsonAsync<WeatherForecast[]>("/api/weather") ?? [];
    }
}

public class ClientDownstreamWeatherService(HttpClient http) : IDownstreamWeatherService
{
    public async Task<WeatherForecast[]> GetForecastAsync()
    {
        return await http.GetFromJsonAsync<WeatherForecast[]>("/api/downstream-weather") ?? [];
    }
}

public class ClientUserService(HttpClient http) : IUserService
{
    public async Task<List<UserDto>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("/api/users") ?? [];
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto input)
    {
        var response = await http.PostAsJsonAsync("/api/users", input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>() 
            ?? throw new InvalidOperationException("Failed to deserialize user.");
    }

    public async Task DeleteUserAsync(string id)
    {
        var response = await http.DeleteAsync($"/api/users/{id}");
        response.EnsureSuccessStatusCode();
    }
}

public class ClientReportService(HttpClient http) : IReportService
{
    public async Task<List<ReportDto>> GetReportsAsync()
    {
        return await http.GetFromJsonAsync<List<ReportDto>>("/api/reports") ?? [];
    }

    public async Task<byte[]> ExportReportsAsync()
    {
        return await http.GetByteArrayAsync("/api/reports/export");
    }
}
