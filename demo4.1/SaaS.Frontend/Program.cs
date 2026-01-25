using SaaS.Frontend.Components;
using SaaS.Frontend.Components.Account;
using SaaS.Frontend.Data;
using SaaS.Frontend.Services;
using SaaS.ServiceDefaults;
using SaaS.Shared;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

const string entraCookieScheme = "EntraCookie";
const string hybridAuthScheme = "Hybrid";
const string identityCookieName = ".AspNetCore.Identity.Application";
var entraCookieName = $".AspNetCore.{entraCookieScheme}";

// Hybrid auth notes:
// - Local Identity endpoints live under /Account/* (wired by MapAdditionalIdentityEndpoints).
// - Entra sign-in/out uses Microsoft.Identity.Web endpoints under /MicrosoftIdentity/Account/*.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = hybridAuthScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme(hybridAuthScheme, "Hybrid authentication scheme", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Cookies.ContainsKey(identityCookieName))
            {
                return IdentityConstants.ApplicationScheme;
            }

            if (context.Request.Cookies.ContainsKey(entraCookieName))
            {
                return entraCookieScheme;
            }

            return IdentityConstants.ApplicationScheme;
        };
    })
    .AddIdentityCookies();

builder.Services.AddAuthentication()
    // MIW registers /MicrosoftIdentity/Account/* endpoints for Entra challenges and callbacks.
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
    },
    openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
    cookieScheme: entraCookieScheme)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("WeatherApi", builder.Configuration.GetSection("DownstreamApis:WeatherApi"))
    .AddDownstreamApi("MicrosoftGraph", builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph"))
    .AddInMemoryTokenCaches();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.SignInScheme = entraCookieScheme;
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IdentityRedirectManager>();

// Used to turn MIW token acquisition exceptions into interactive challenges.
builder.Services.AddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHttpClient(
    "WeatherApi",
    (sp, httpClient) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["DownstreamApis:WeatherApi:BaseUrl"] ?? "https+http://weatherapi";
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    });
builder.Services.AddHttpClient(
    "MicrosoftGraph",
    (sp, httpClient) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["DownstreamApis:MicrosoftGraph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    });

builder.Services.AddScoped<IWeatherForecaster, ServerWeatherForecaster>();
builder.Services.AddScoped<IGraphProfileService, GraphService>();

builder.Services.AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters())
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transforms =>
    {
        transforms.AddRequestTransform(async transformContext =>
        {
            var tokenAcquisition = transformContext.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();

            var scopes = builder.Configuration.GetSection("DownstreamApis:WeatherApi:Scopes").Get<string[]>();
            if (scopes is null || scopes.Length == 0)
            {
                var scopesValue = builder.Configuration["DownstreamApis:WeatherApi:Scopes"];
                scopes = scopesValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            }

            var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);

            transformContext.ProxyRequest.Headers.Remove("Cookie");
            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorPages();

var graphApi = app.MapGroup("/api/graph").RequireAuthorization();
graphApi.MapGet(
    "/me",
    async (IGraphProfileService graphService, MicrosoftIdentityConsentAndConditionalAccessHandler cca, CancellationToken cancellationToken) =>
    {
        try
        {
            var profile = await graphService.GetMyProfileAsync(cancellationToken);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            cca.HandleException(ex);
            return Results.Empty;
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    });

app.MapReverseProxy().RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SaaS.Frontend.Client._Imports).Assembly);

// Local Identity endpoints under /Account/* for login/register/manage/passkeys.
app.MapAdditionalIdentityEndpoints();

app.Run();

static IReadOnlyList<RouteConfig> GetRoutes() =>
    [
        new RouteConfig
        {
            RouteId = "weather-proxy",
            ClusterId = "weather-cluster",
            Match = new RouteMatch { Path = "/api/proxy/weather/{**catch-all}" },
            Transforms =
            [
                new Dictionary<string, string> { ["PathRemovePrefix"] = "/api/proxy/weather" },
            ],
        },
    ];

static IReadOnlyList<ClusterConfig> GetClusters() =>
    [
        new ClusterConfig
        {
            ClusterId = "weather-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend"] = new DestinationConfig { Address = "https+http://weatherapi/" },
            },
        },
    ];
