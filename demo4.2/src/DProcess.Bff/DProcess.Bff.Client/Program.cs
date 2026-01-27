using DProcess.Bff.Client.Services;
using DProcess.Shared.Permissions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore(options =>
{
	options.AddPolicy(PermissionNames.WeatherRead, policy => policy.RequireClaim("permission", PermissionNames.WeatherRead));
	options.AddPolicy(PermissionNames.WeatherWrite, policy => policy.RequireClaim("permission", PermissionNames.WeatherWrite));
	options.AddPolicy(PermissionNames.UsersRead, policy => policy.RequireClaim("permission", PermissionNames.UsersRead));
	options.AddPolicy(PermissionNames.UsersWrite, policy => policy.RequireClaim("permission", PermissionNames.UsersWrite));
	options.AddPolicy(PermissionNames.UsersDelete, policy => policy.RequireClaim("permission", PermissionNames.UsersDelete));
	options.AddPolicy(PermissionNames.ReportsView, policy => policy.RequireClaim("permission", PermissionNames.ReportsView));
	options.AddPolicy(PermissionNames.ReportsExport, policy => policy.RequireClaim("permission", PermissionNames.ReportsExport));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
