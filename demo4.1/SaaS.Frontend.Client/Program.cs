using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SaaS.Frontend.Client.Services;
using SaaS.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddScoped<IWeatherForecaster, ClientWeatherForecaster>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
