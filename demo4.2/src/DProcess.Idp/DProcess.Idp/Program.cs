using DProcess.Idp.Components;
using DProcess.Idp.Components.Account;
using DProcess.Idp.Data;
using DProcess.Idp;
using DProcess.Idp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddControllers();

builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("db")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=Data/app.db;Cache=Shared";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.UseOpenIddict();
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure application cookie for auto-redirect to login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Configure default authentication schemes for [Authorize] attributes
builder.Services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

// Add external authentication provider (Entra ID)
// Use same DefaultScheme to prevent overriding the defaults set above
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddOpenIdConnect("Entra", options =>
    {
        var tenantId = builder.Configuration["Entra:TenantId"] ?? throw new InvalidOperationException("Entra tenant is not configured.");
        var clientId = builder.Configuration["Entra:ClientId"] ?? throw new InvalidOperationException("Entra client ID is not configured.");
        var clientSecret = builder.Configuration["Entra:ClientSecret"] ?? throw new InvalidOperationException("Entra client secret is not configured.");

        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.CallbackPath = "/signin-entra";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.SaveTokens = false;
        options.Scope.Clear();
        options.Scope.Add(OpenIdConnectScope.OpenId);
        options.Scope.Add(OpenIdConnectScope.Profile);
        options.Scope.Add(OpenIdConnectScope.Email);
        options.TokenValidationParameters.NameClaimType = ClaimTypes.Name;
    });

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo")
               .SetLogoutEndpointUris("/connect/endsession");

        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();
        options.RequireProofKeyForCodeExchange();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableLogoutEndpointPassthrough()
               .EnableStatusCodePagesIntegration();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess,
            "api");
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHostedService<OpenIddictSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePages(context =>
{
    var path = context.HttpContext.Request.Path;
    if (!path.StartsWithSegments("/connect"))
    {
        context.HttpContext.Response.Redirect($"/not-found?code={context.HttpContext.Response.StatusCode}");
    }
    return Task.CompletedTask;
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapAdditionalIdentityEndpoints();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await DbSeeder.SeedDataAsync(app.Services);

app.Run();
