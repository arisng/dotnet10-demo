using Demo5_1.Web.Client.Pages;
using Demo5_1.Web.Components;
using Demo5_1.ServiceDefaults;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Components.Authorization;
using Demo5_1.Web.Client.Services;
using Microsoft.Identity.Abstractions;
using Demo5_1.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Authentication Configuration
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "MicrosoftEntra";
    })
    .AddCookie("Cookies")
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"), openIdConnectScheme: "MicrosoftEntra")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("ApiService", options => 
    {
        options.BaseUrl = "http://apiservice";
        options.Scopes = builder.Configuration.GetSection("ApiService:Scopes").Get<string[]>();
    })
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("weather.read", policy => policy.RequireClaim("permission", "weather.read"))
    .AddPolicy("weather.write", policy => policy.RequireClaim("permission", "weather.write"))
    .AddPolicy("users.read", policy => policy.RequireClaim("permission", "users.read"))
    .AddPolicy("users.write", policy => policy.RequireClaim("permission", "users.write"))
    .AddPolicy("users.delete", policy => policy.RequireClaim("permission", "users.delete"))
    .AddPolicy("reports.view", policy => policy.RequireClaim("permission", "reports.view"))
    .AddPolicy("reports.export", policy => policy.RequireClaim("permission", "reports.export"));

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Add Token to Proxy Requests
        builderContext.AddRequestTransform(async transformContext =>
        {
            var services = transformContext.HttpContext.RequestServices;
            var tokenAcquisition = services.GetRequiredService<ITokenAcquisition>();
            try
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var scopes = configuration.GetSection("ApiService:Scopes").Get<string[]>();
                var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                transformContext.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            catch (Exception ex)
            {
                // If token cannot be acquired (e.g. not logged in), do nothing or fail?
                // YARP will forward without token, API will reject (401).
                Console.WriteLine($"Token acquisition failed: {ex.Message}");
            }
        });
    });

// Register Client Services for Server Prerendering
builder.Services.AddHttpClient<IWeatherService, ClientWeatherService>(client => 
    client.BaseAddress = new Uri("http://apiservice"));
builder.Services.AddHttpClient<IUserService, ClientUserService>(client => 
    client.BaseAddress = new Uri("http://apiservice"));
builder.Services.AddHttpClient<IReportService, ClientReportService>(client => 
    client.BaseAddress = new Uri("http://apiservice"));
// IDownstreamWeatherService is removed/ignored.

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // For signin-oidc

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Demo5_1.Web.Client._Imports).Assembly);

app.MapReverseProxy();

app.Run();
