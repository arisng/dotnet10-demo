using Demo4.EntraIntegration.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("weather.read", policy => policy.RequireClaim("permission", "weather.read"));
    options.AddPolicy("weather.write", policy => policy.RequireClaim("permission", "weather.write"));
    options.AddPolicy("users.read", policy => policy.RequireClaim("permission", "users.read"));
    options.AddPolicy("users.write", policy => policy.RequireClaim("permission", "users.write"));
    options.AddPolicy("users.delete", policy => policy.RequireClaim("permission", "users.delete"));
    options.AddPolicy("reports.view", policy => policy.RequireClaim("permission", "reports.view"));
    options.AddPolicy("reports.export", policy => policy.RequireClaim("permission", "reports.export"));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Client Services
builder.Services.AddScoped<IWeatherService, ClientWeatherService>();
builder.Services.AddScoped<IUserService, ClientUserService>();
builder.Services.AddScoped<IReportService, ClientReportService>();

await builder.Build().RunAsync();
