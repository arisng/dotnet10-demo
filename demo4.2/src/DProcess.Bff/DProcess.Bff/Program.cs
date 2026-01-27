using DProcess.Bff.Authorization;
using DProcess.Bff.Client.Pages;
using DProcess.Bff.Components;
using DProcess.Shared.Permissions;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Options;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configure Static Web Assets for non-Development environments
// This enables serving .client project assets in Staging/Production when running locally
if (!builder.Environment.IsDevelopment())
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-dprocess-bff";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var expiresAt = context.Properties.GetTokenValue("expires_at");
                if (!DateTimeOffset.TryParse(expiresAt, out var expires))
                {
                    return;
                }

                if (expires > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    return;
                }

                var refreshToken = context.Properties.GetTokenValue("refresh_token");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return;
                }

                var oidc = context.HttpContext.RequestServices
                    .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                    .Get(OpenIdConnectDefaults.AuthenticationScheme);

                var config = await oidc.ConfigurationManager!.GetConfigurationAsync(context.HttpContext.RequestAborted);
                var client = context.HttpContext.RequestServices
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient();

                var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = config.TokenEndpoint ?? string.Empty,
                    ClientId = oidc.ClientId ?? string.Empty,
                    ClientSecret = oidc.ClientSecret ?? string.Empty,
                    RefreshToken = refreshToken
                }, context.HttpContext.RequestAborted);

                if (tokenResponse.IsError)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                    return;
                }

                context.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                context.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken ?? refreshToken);
                context.Properties.UpdateTokenValue("expires_at",
                    DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o"));

                context.ShouldRenew = true;
            }
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Idp:Authority"]!;
        options.ClientId = builder.Configuration["Idp:ClientId"]!;
        options.ClientSecret = builder.Configuration["Idp:ClientSecret"]!;

        options.ResponseType = "code";
        options.UsePkce = true;

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");

        options.Events = new OpenIdConnectEvents
        {
            OnUserInformationReceived = context =>
            {
                if (context.User.RootElement.TryGetProperty("permission", out var permValue))
                {
                    if (permValue.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var permission in permValue.EnumerateArray())
                        {
                            var value = permission.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                context.Principal?.AddIdentity(new ClaimsIdentity(new[]
                                {
                                    new Claim("permission", value)
                                }));
                            }
                        }
                    }
                    else if (permValue.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var value = permValue.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            context.Principal?.AddIdentity(new ClaimsIdentity(new[]
                            {
                                new Claim("permission", value)
                            }));
                        }
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PermissionNames.WeatherRead, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.WeatherRead)))
    .AddPolicy(PermissionNames.WeatherWrite, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.WeatherWrite)))
    .AddPolicy(PermissionNames.UsersRead, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersRead)))
    .AddPolicy(PermissionNames.UsersWrite, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersWrite)))
    .AddPolicy(PermissionNames.UsersDelete, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersDelete)))
    .AddPolicy(PermissionNames.ReportsView, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.ReportsView)))
    .AddPolicy(PermissionNames.ReportsExport, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.ReportsExport)));

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapReverseProxy(proxyPipeline =>
    {
        proxyPipeline.Use(async (context, next) =>
        {
            var token = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers.Authorization = $"Bearer {token}";
            }

            await next();
        });
    })
    .RequireAuthorization();

app.MapGet("/login", (HttpContext context, string? returnUrl = null) =>
{
    var redirectUri = "/";

    if (!string.IsNullOrWhiteSpace(returnUrl)
        && Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        redirectUri = returnUrl;
    }

    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirectUri },
        new[] { OpenIdConnectDefaults.AuthenticationScheme });
});

app.MapGet("/logout", async (HttpContext context) =>
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        return Results.Redirect("/");
    });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(DProcess.Bff.Client._Imports).Assembly);

app.Run();
