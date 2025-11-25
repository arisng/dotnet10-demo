using System.Security.Authentication;
using System.Security.Claims;
using Demo5.DownstreamApi.Authorization;
using Demo5.DownstreamApi.Shared.Models;
using Demo5.DownstreamApi.Client.Services;
using Demo5.DownstreamApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Demo5.DownstreamApi.Services;

public class ServerWeatherService : IWeatherService
{
    public Task<WeatherForecast[]> GetForecastAsync()
    {
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var result = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            })
            .ToArray();
        
        return Task.FromResult(result);
    }
}

public class ServerUserService(UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto { Id = user.Id, Email = user.Email!, Roles = roles });
        }
        return userDtos;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto input)
    {
        var user = new ApplicationUser { UserName = input.Email, Email = input.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded) 
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        
        await userManager.AddToRoleAsync(user, input.Role);
        return new UserDto { Id = user.Id, Email = user.Email!, Roles = [input.Role] };
    }

    public async Task DeleteUserAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) throw new KeyNotFoundException("User not found");
        
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

public class ServerReportService : IReportService
{
    public Task<List<ReportDto>> GetReportsAsync()
    {
        var result = Enumerable.Range(1, 5).Select(index => new ReportDto
        {
            Id = index,
            Title = $"Monthly Report {index}",
            Date = DateOnly.FromDateTime(DateTime.Now.AddMonths(-index)),
            Status = "Available"
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<byte[]> ExportReportsAsync()
    {
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes("Report Data..."));
    }
}
