using Demo5_1.Web.Client.Pages;
using Demo5_1.Web.Components;
using Demo5_1.ServiceDefaults;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Components.Authorization;
using Demo5_1.Web.Client.Services;
using Microsoft.Identity.Abstractions;
using Demo5_1.Web.Services;
using Demo5_1.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Authentication Configuration
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();
builder.Services.AddScoped<IApiTokenProvider, HybridApiTokenProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "MicrosoftEntra";
    })
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
            var tokenProvider = services.GetRequiredService<IApiTokenProvider>();
            try
            {
                var token = await tokenProvider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    transformContext.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
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

app.MapStaticAssets();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // For signin-oidc

var accountApi = app.MapGroup("/account");

accountApi.MapPost("/login-handler", async ([FromForm] string Email, [FromForm] string Password, [FromQuery] string? returnUrl, IHttpClientFactory clientFactory, HttpContext httpContext) =>
{
    var request = new LoginRequest { Email = Email, Password = Password };
    var client = clientFactory.CreateClient();
    client.BaseAddress = new Uri("http://apiservice");
    
    var response = await client.PostAsJsonAsync("/api/identity/token", request);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Unauthorized();
    }

    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
    if (tokenResponse == null) return Results.Unauthorized();

    // Create claims principal for the Cookie
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, request.Email),
        new Claim(ClaimTypes.NameIdentifier, request.Email),
        new Claim(ClaimTypes.Email, request.Email),
        new Claim("api_access_token", tokenResponse.AccessToken) // Store for YARP
    };

    var identity = new ClaimsIdentity(claims, "Cookies");
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync("Cookies", principal);

    // Call Provisioning on API (so the backend knows the user)
    var provisionClient = clientFactory.CreateClient();
    provisionClient.BaseAddress = new Uri("http://apiservice");
    provisionClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
    await provisionClient.PostAsync("/api/identity/provision", null);

    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

accountApi.MapPost("/logout", async (string? returnUrl, HttpContext httpContext) =>
{
    await httpContext.SignOutAsync("Cookies");
    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Demo5_1.Web.Client._Imports).Assembly);

app.MapReverseProxy();

app.Run();
