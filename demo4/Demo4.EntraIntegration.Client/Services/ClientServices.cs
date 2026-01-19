using System.Net.Http.Json;
using Demo4.EntraIntegration.Shared.Models;

namespace Demo4.EntraIntegration.Client.Services;

public class ClientWeatherService(HttpClient http) : IWeatherService
{
    public async Task<WeatherForecast[]> GetForecastAsync()
    {
        return await http.GetFromJsonAsync<WeatherForecast[]>("/api/weather") ?? [];
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

public class ClientGraphService(HttpClient http) : IGraphService
{
    public async Task<UserProfile?> GetUserProfileAsync()
    {
        using var response = await http.GetAsync("/api/graph/profile");

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden
            or System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>();
    }

    public async Task<byte[]?> GetUserPhotoAsync()
    {
        using var response = await http.GetAsync("/api/graph/profile/photo");

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden
            or System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task SyncUserProfileToLocalAsync(string userId)
    {
        // Client-side implementation could call a server endpoint if needed.
        // For now, syncing is handled on the server during provisioning.
        await Task.CompletedTask;
    }
}
