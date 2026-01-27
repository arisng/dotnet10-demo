Below is a **.NET 10** sample solution using 
- **OpenIddict** for my IdP implementation
- **ASP.NET Core Identity** for local username/password
- **Microsoft Entra ID** as an external provider
- **Blazor Web App BFF with YARP** orchestrated with **Aspire**. 
It implements a **single local RBAC** that emits **`permission` claims** into access tokens regardless of login method.
RBAC is role-based (role → permission), with optional user-level overrides.
**Requirement:** `permission` claims must be present in the **BFF auth state**. This requires the IdP to return `permission` in **UserInfo** (or ID token) and the BFF to explicitly map that claim.

This demo intentionally keeps the **DProcess.\*** namespace/project naming (it does **not** follow the repo’s usual naming conventions).
It is a **fresh implementation** that **inherits the README narrative from demo4.1**, but introduces a **dedicated IdP project** that demo4.1 does not include.

---

# Research references (authoritative sources)

- `demo4.2/.docs/reference/research-01-openiddict-identity-passkeys.md` (OpenIddict + Identity passkeys)
- `demo4.2/.docs/reference/research-02-entra-external-oidc.md` (Entra external login via OIDC)
- `demo4.2/.docs/reference/research-03-blazor-bff-yarp-oidc.md` (Blazor BFF + YARP + OIDC)
- `demo4.2/.docs/reference/research-04-openiddict-claim-destinations.md` (claim destinations for `permission`)
- `demo4.2/.docs/reference/research-05-api-jwt-validation.md` (API JWT validation)

---

# Solution layout

```
demo4.2/
  DProcess.AppHost/                 (Aspire host)
  DProcess.ServiceDefaults/         (Aspire defaults)
  DProcess.Idp/                     (Identity + OpenIddict server + Entra external login + UI)
  DProcess.Bff/                     (Blazor Web App + OIDC client + YARP)
  DProcess.Api/                     (Protected API with permission-claim policies)
  DProcess.Shared/                  (Shared DTOs/constants, permission names)
```

## Project correlation (runtime flow)
```
Browser
  └─> DProcess.Bff (Blazor UI + cookie auth)
        ├─ OIDC challenge → DProcess.Idp (OpenIddict + Identity + Entra)
        └─ /api/* proxy → DProcess.Api (permission policies)

DProcess.Api
  └─ validates access tokens from DProcess.Idp

DProcess.Idp
  └─ emits permission claims into access tokens + ID token/UserInfo (role → permission)

DProcess.Shared
  └─ referenced by Idp/Bff/Api for shared models and permission constants
```

---

# Request flows (all diagrams)

See: `demo4.2/.docs/reference/flows.md`

---

# Solution file format

Use **`.slnx`** for the solution file (not `.sln`). If your installed SDK does not support `--format slnx`, create `.sln` and convert to `.slnx` using the IDE tooling before continuing.

---

# 0) Packages you’ll need

Use current stable versions available for .NET 10 timeframe (preview/RC may vary). Add these packages:

## 0.1 Shared project (DProcess.Shared)
Create `DProcess.Shared` to hold:
- Permission name constants (aligned with Demo3: `weather.read`, `users.write`, etc.)
- Shared DTOs and contracts used by BFF/Api/IdP

## Idp
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Sqlite` (or `SqlServer`)
- `OpenIddict.AspNetCore`
- `OpenIddict.EntityFrameworkCore`
- `OpenIddict.Validation.AspNetCore`
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`

## Bff
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`
- `Yarp.ReverseProxy`

## Api
- `Microsoft.AspNetCore.Authentication.JwtBearer`

## AppHost/ServiceDefaults
- Aspire defaults templates (from `dotnet new aspire`)

---

# 1) IdP project (DProcess.Idp)

## 1.1 Data model for RBAC (role → permission)

Baseline: **Demo3.BffRbac** (DRY). Reuse the same schema and EF Core configuration from:
- `demo3/Demo3.BffRbac/Data/ApplicationDbContext.cs`
- `demo3/Demo3.BffRbac/Data/RolePermission.cs`

For demo4.2, port the same types into `DProcess.Idp` and keep the **role → permission** relationship.
We intentionally **do not** model per-user overrides here (no `UserPermission`).
Permission names must align with Demo3 (e.g., `weather.read`, `weather.write`, `users.read`, `users.write`, `users.delete`, `reports.view`, `reports.export`), and should be centralized in `DProcess.Shared` as constants.

---

## 1.2 Permission service (role → permission)

Baseline: **Demo3.BffRbac** (DRY). Reuse the same logic from:
- `demo3/Demo3.BffRbac/Services/PermissionService.cs`

For demo4.2, move the service into `DProcess.Idp.Security` and update namespaces/db context.
The service should compute permissions from **role assignments** only.

---

## 1.3 IdP program setup (Blazor Web App + Identity + Entra external + OpenIddict)

**UI mode:** IdP is **Interactive Server only** (no WASM render mode).

### `Program.cs`
```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using DProcess.Idp.Data;
using DProcess.Idp.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Use Sqlite for sample simplicity.
    options.UseSqlite(builder.Configuration.GetConnectionString("db") ?? "Data Source=idp.db");

    // Register OpenIddict entity sets.
    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        // Required for passkey support.
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<PermissionService>();

// Important: Identity uses its own cookie; OpenIddict will also issue tokens.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect("Entra", options =>
{
    var tenantId = builder.Configuration["Entra:TenantId"];
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

    options.ClientId = builder.Configuration["Entra:ClientId"]!;
    options.ClientSecret = builder.Configuration["Entra:ClientSecret"]!;
    options.CallbackPath = "/signin-entra";
    options.ResponseType = "code";

    // For external login, we only need the external identity to link/provision a local user.
    options.SaveTokens = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Ensure we get stable identifiers:
    options.TokenValidationParameters.NameClaimType = "name";
});

// OpenIddict server (your local IdP)
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo");

        options.AllowAuthorizationCodeFlow();
        // Required if you want long-lived sessions with refresh tokens.
        // options.AllowRefreshTokenFlow();
        options.RequireProofKeyForCodeExchange();

        // For development only. Replace with real certs in production.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // ASP.NET Core host integration.
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableStatusCodePagesIntegration();

        // Scopes that your BFF/API may request.
        options.RegisterScopes("openid", "profile", "email", "api" /*, "offline_access"*/);
    })
    .AddValidation(options =>
    {
        // Let this app validate its own tokens (useful if you add internal endpoints later).
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHostedService<OpenIddictSeeder>();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
// Required for passkey endpoints and /Account/* routes.
app.MapAdditionalIdentityEndpoints();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/Account/Login"));

app.Run();
```
Research: `demo4.2/.docs/reference/research-01-openiddict-identity-passkeys.md`

---

## 1.4 Seed OpenIddict client for the BFF

### `OpenIddictSeeder.cs`
```csharp
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DProcess.Idp;

public sealed class OpenIddictSeeder(IServiceProvider sp) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = sp.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // BFF client registration
        const string clientId = "bff";
        var existing = await appManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientSecret = "bff-secret",
                DisplayName = "Blazor BFF",
                // First-party client: skip consent UI for this sample.
                ConsentType = ConsentTypes.Implicit,

                RedirectUris =
                {
                    new Uri("https://localhost:7181/signin-oidc")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:7181/signout-callback-oidc")
                },

                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Userinfo,

                    Permissions.GrantTypes.AuthorizationCode,
                    // Add refresh tokens if using offline_access.
                    // Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,

                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.OpenId,
                    "scp:api"
                    // If using refresh tokens:
                    // Permissions.Scopes.OfflineAccess
                },

                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

---

## 1.5 IdP UI: Blazor Identity components (preferred over Razor Pages)

Use the **Blazor Identity components** pattern from demo4 as the baseline instead of Razor Pages.
These are the standard scaffolding outputs for Blazor Identity UI and align with passkeys + external logins:

**Important:** keep the IdP UI **Interactive Server only** (no WASM render mode).

- `demo4/Demo4.EntraIntegration/Components/Account/Pages/Login.razor`
- `demo4/Demo4.EntraIntegration/Components/Account/Pages/ExternalLogin.razor`
- `demo4/Demo4.EntraIntegration/Components/Account/Shared/ExternalLoginPicker.razor`
- `demo4/Demo4.EntraIntegration/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
- `demo4/Demo4.EntraIntegration/Components/Account/IdentityRedirectManager.cs`

Port these into `DProcess.Idp/Components/Account/*` and keep routing under `/Account/*`.
The login page already supports **local account**, **external provider**, and **passkey** login flows.
This aligns with the standard **Blazor Identity scaffolding** for “Individual Accounts,” so it makes sense to treat the IdP UI as scaffold-generated rather than custom-built.

Research: `demo4.2/.docs/reference/research-02-entra-external-oidc.md`

---

## 1.6 External login callback (Blazor components)

The external login flow is handled by:
- `ExternalLogin.razor` (UI + callback handling)
- `/Account/PerformExternalLogin` and `/Account/ExternalLogin` endpoints from `IdentityComponentsEndpointRouteBuilderExtensions.cs`

No Razor Pages are required for external login in this demo4.2 plan.

---

## 1.7 OpenIddict authorization endpoint logic + permission claims issuance

To keep the sample minimal and clear, implement OpenIddict endpoints using a controller.
This plan assumes a **first-party BFF client** with **implicit consent** (see seeder). If you switch to explicit consent, add a consent page and honor `prompt=consent` / `prompt=login` behaviors.
Research: `demo4.2/.docs/reference/research-04-openiddict-claim-destinations.md`

    **Responsibility note:** permission claims are issued by the **IdP** in access tokens **and** ID tokens/UserInfo (Option A).  
    The BFF should treat tokens as read-only; it does not mint or enrich access tokens.

### `Controllers/AuthorizationController.cs`
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using DProcess.Idp.Data;
using DProcess.Idp.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Linq;

namespace DProcess.Idp.Controllers;

[ApiController]
public class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    PermissionService permissionService) : Controller
{
    [HttpGet("~/connect/authorize"), Authorize]
    public async Task<IActionResult> AuthorizeEndpoint()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                     ?? throw new InvalidOperationException("OpenIddict request missing.");

        var user = await userManager.GetUserAsync(User)
                   ?? throw new InvalidOperationException("User not found.");

        var permissions = await permissionService.GetPermissionsAsync(user.Id);

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, user.Id));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? ""));
        identity.AddClaim(new Claim(Claims.Name, user.UserName ?? user.Email ?? user.Id));

        // Your unified RBAC: permission claims always come from local DB.
        foreach (var p in permissions)
            identity.AddClaim(new Claim("permission", p));

        // Scope + resources
        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(request.GetScopes());
        principal.SetResources("api");

        // Ensure which claims go to which tokens
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(claim.Type switch
            {
                // Option A: permissions flow to access token + id token (and userinfo if enabled).
                "permission" => new[] { Destinations.AccessToken, Destinations.IdToken, Destinations.UserInfo },
                Claims.Email => new[] { Destinations.IdToken, Destinations.AccessToken },
                Claims.Name => new[] { Destinations.IdToken, Destinations.AccessToken },
                Claims.Subject => new[] { Destinations.IdToken, Destinations.AccessToken },
                _ => new[] { Destinations.AccessToken }
            });
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo"), Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult UserInfo()
    {
        var permissions = User.FindAll("permission").Select(c => c.Value).ToArray();
        return Ok(new
        {
            sub = User.GetClaim(Claims.Subject),
            name = User.GetClaim(Claims.Name),
            email = User.GetClaim(Claims.Email),
            // Ensure BFF can load permission claims via UserInfo.
            permission = permissions
        });
    }
}

file static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal principal, string type)
        => principal.Claims.FirstOrDefault(c => c.Type == type)?.Value;
}
```

### Register controllers in `Program.cs`
Add (already shown in the IdP `Program.cs` snippet):
```csharp
builder.Services.AddControllers();
...
app.MapControllers();
```

---

## 1.8 IdP configuration (`appsettings.Development.json`)
```json
{
  "ConnectionStrings": {
    "db": "Data Source=idp.db"
  },
  "Entra": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_IDP_APP_REG_CLIENT_ID",
    "ClientSecret": "YOUR_IDP_APP_REG_CLIENT_SECRET"
  }
}
```

> In Entra, register an app **for the IdP** as an OIDC client to Entra. Redirect URI should include: `https://localhost:****/signin-entra` (the IdP’s dev HTTPS port).

---

# 2) BFF project (DProcess.Bff) — Blazor Web App + OIDC + YARP
Research: `demo4.2/.docs/reference/research-03-blazor-bff-yarp-oidc.md`

## 2.A Authorization policy setup (Blazor [Authorize] attributes)
Baseline: **Demo3.BffRbac**. Inherit the policy registration used for Blazor `[Authorize]` attributes from:
- `demo3/Demo3.BffRbac/Program.cs`

Reference block (from Demo3):
```csharp
// Authorization services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

// Register policies for Blazor [Authorize] attributes
builder.Services.AddAuthorizationBuilder() // Prefer AddAuthorizationBuilder instead of AddAuthorization
    .AddPolicy("weather.read", policy => policy.AddRequirements(new PermissionRequirement("weather.read")))
    .AddPolicy("weather.write", policy => policy.AddRequirements(new PermissionRequirement("weather.write")))
    .AddPolicy("users.read", policy => policy.AddRequirements(new PermissionRequirement("users.read")))
    .AddPolicy("users.write", policy => policy.AddRequirements(new PermissionRequirement("users.write")))
    .AddPolicy("users.delete", policy => policy.AddRequirements(new PermissionRequirement("users.delete")))
    .AddPolicy("reports.view", policy => policy.AddRequirements(new PermissionRequirement("reports.view")))
    .AddPolicy("reports.export", policy => policy.AddRequirements(new PermissionRequirement("reports.export")));

// Register custom handlers
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

In DProcess.Bff (Option A), **omit** the `IPermissionService` and `IClaimsTransformation` lines (IdP supplies `permission` claims). Keep policy registration + handler, and update the policy names to your DProcess permissions.

### 2.0 How projects connect (BFF perspective)
```
Browser
  → DProcess.Bff (/login, UI, cookie)
      → (OIDC) DProcess.Idp (authorize/token)
      → (YARP) DProcess.Api (/api/*) with access token
```

### 2.0.1 NavMenu login/logout links
Use simple links to the BFF endpoints:
```razor
<AuthorizeView>
    <Authorized>
        <a href="/logout">Logout</a>
    </Authorized>
    <NotAuthorized>
        <a href="/login">Login</a>
    </NotAuthorized>
</AuthorizeView>
```

### 2.0.2 ClaimsPrincipal in InteractiveAuto
In **InteractiveAuto**, the authenticated user is established on the **server**:
- SSR and SignalR phases use `HttpContext.User` (cookie from OIDC).
- The BFF remains the auth source of truth; the WASM client does not store tokens.
- If the WASM UI needs auth state, it should consume the **server-provided** authentication state via `AuthenticationStateProvider`.
With **Option A**, the BFF expects `permission` claims to arrive via the **ID token/UserInfo** from the IdP, so no `PermissionClaimsTransformation` is required in the BFF.
**Important:** the IdP **must** return `permission` in UserInfo, and the BFF must map that claim into the auth principal (see 2.1).
See `demo4.2/.docs/reference/flows.md` (ClaimsPrincipal construction diagram).
The SSR auth state is serialized and transferred to the WASM client by `PersistingServerAuthenticationStateProvider` (reference Demo4.EntraIntegration.Authorization.PersistingServerAuthenticationStateProvider).
- Register this in `Program.cs`: 
```csharp
// Register the persisting provider to pass state to WASM
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();
```

## 2.1 Program.cs

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-bff";
    // Refresh tokens on long-lived sessions (see 2.1.1).
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Idp:Authority"]!;
    options.ClientId = builder.Configuration["Idp:ClientId"]!;
    options.ClientSecret = builder.Configuration["Idp:ClientSecret"]!;

    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;

    options.SaveTokens = true; // needed to forward access token via YARP
    options.GetClaimsFromUserInfoEndpoint = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("api");
    // Optional if using refresh tokens:
    // options.Scope.Add("offline_access");

    // Helps avoid claim mapping surprises
    options.MapInboundClaims = false;

    // Map permission claims from UserInfo into the auth state.
    // If permission is an array, use OnUserInformationReceived to add multiple claims.
    options.Events = new OpenIdConnectEvents
    {
        OnUserInformationReceived = context =>
        {
            if (context.User.TryGetProperty("permission", out var permValue))
            {
                if (permValue.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var p in permValue.EnumerateArray())
                    {
                        var value = p.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            context.Principal?.AddIdentity(new ClaimsIdentity(new[]
                            {
                                new Claim("permission", value)
                            }));
                    }
                }
                else if (permValue.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var value = permValue.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        context.Principal?.AddIdentity(new ClaimsIdentity(new[]
                        {
                            new Claim("permission", value)
                        }));
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// YARP forwarder: attach access token from BFF auth session
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var token = await context.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            context.Request.Headers.Authorization = $"Bearer {token}";

        await next();
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.MapGet("/login", (HttpContext ctx) =>
{
    return Results.Challenge(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" },
        new[] { OpenIdConnectDefaults.AuthenticationScheme });
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();
```

## 2.1.1 Access token refresh (required for long-lived sessions)

The current BFF design forwards the stored access token via YARP. Without refresh handling, long-lived sessions will break once the access token expires. Pick **one** of these approaches:

**Option A (recommended for this demo): cookie validation refresh**
- Request `offline_access` scope in the BFF.
- Enable refresh tokens in the IdP (`AllowRefreshTokenFlow` + `offline_access` scope).
- On cookie validation, detect near-expiry access tokens, call the token endpoint with the refresh token, and update the cookie’s stored tokens.

**Option B:** add an access token management library and centralize refresh logic.

### Minimal refresh implementation (Option A)

Requires the `IdentityModel` package (`IdentityModel.Client` helpers).

```csharp
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

builder.Services.AddHttpClient();

builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async ctx =>
            {
                var expiresAt = ctx.Properties.GetTokenValue("expires_at");
                if (!DateTimeOffset.TryParse(expiresAt, out var expires))
                    return;

                // Refresh if token expires within 5 minutes.
                if (expires > DateTimeOffset.UtcNow.AddMinutes(5))
                    return;

                var refreshToken = ctx.Properties.GetTokenValue("refresh_token");
                if (string.IsNullOrEmpty(refreshToken))
                    return;

                var oidc = ctx.HttpContext.RequestServices
                    .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                    .Get(OpenIdConnectDefaults.AuthenticationScheme);

                var config = await oidc.ConfigurationManager!.GetConfigurationAsync(ctx.HttpContext.RequestAborted);
                var client = ctx.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();

                var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = config.TokenEndpoint,
                    ClientId = oidc.ClientId,
                    ClientSecret = oidc.ClientSecret,
                    RefreshToken = refreshToken
                }, ctx.HttpContext.RequestAborted);

                if (tokenResponse.IsError)
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync();
                    return;
                }

                ctx.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                ctx.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken ?? refreshToken);
                ctx.Properties.UpdateTokenValue("expires_at",
                    DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o"));

                ctx.ShouldRenew = true;
            }
        };
    });
```

---

## 2.2 `appsettings.Development.json`
```json
{
  "Idp": {
    "Authority": "https://localhost:7241",
    "ClientId": "bff",
    "ClientSecret": "bff-secret"
  },
  "ReverseProxy": {
    "Routes": {
      "api": {
        "ClusterId": "api",
        "Match": {
          "Path": "/api/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "api": {
        "Destinations": {
          "d1": {
            "Address": "https://localhost:7261/"
          }
        }
      }
    }
  }
}
```

## 2.3 Minimal UI hook
In your Blazor UI, link to:
- `/login`
- `/logout`

---

# 3) API project (DProcess.Api) — permission-claim authorization
Research: `demo4.2/.docs/reference/research-05-api-jwt-validation.md`

## 3.A Authorization building blocks (reuse Demo3.BffRbac)
Import the same authorization helpers from demo3:
- `demo3/Demo3.BffRbac/Authorization/AuthorizationExtensions.cs`
- `demo3/Demo3.BffRbac/Authorization/PermissionAuthorizationHandler.cs`
- `demo3/Demo3.BffRbac/Authorization/PermissionRequirement.cs`

Use the same `RequirePermission("...")` extension when mapping minimal APIs in `DProcess.Api`.
Register `PermissionAuthorizationHandler` in `Program.cs` (see 3.1).

## 3.1 Program.cs (single issuer)
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using DProcess.Api.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Idp:Authority"]!;
        options.RequireHttpsMetadata = true;

        // OpenIddict uses standard JWT validation; ensure audience/resource aligns with IdP issuance.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = "api",
            ValidateAudience = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/weather", () => Results.Ok(new[] { "Sunny", "Cloudy" }))
   .RequirePermission("weather.read");

app.MapPost("/api/weather", () => Results.Ok(new { updated = true }))
   .RequirePermission("weather.write");

app.Run();
```

## 3.2 Program.cs (multi-issuer for OBO path)

If you enable **Path A** (Entra tokens for Graph), the API must validate **two issuers**. Use **separate authentication schemes** and bind them to **separate endpoint groups**:

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("OpenIddict", options =>
    {
        options.Authority = builder.Configuration["Idp:Authority"]!;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = "api",
            ValidateAudience = true
        };
    })
    .AddJwtBearer("Entra", options =>
    {
        var tenantId = builder.Configuration["Entra:TenantId"]!;
        var apiClientId = builder.Configuration["Entra:ApiClientId"]!;
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = $"api://{apiClientId}",
            ValidateAudience = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OpenIddictApi", policy =>
        policy.AddAuthenticationSchemes("OpenIddict").RequireAuthenticatedUser())
    .AddPolicy("EntraGraphApi", policy =>
        policy.AddAuthenticationSchemes("Entra").RequireAuthenticatedUser());

// OpenIddict endpoints: local RBAC
var localApi = app.MapGroup("/api")
    .RequireAuthorization("OpenIddictApi");

localApi.MapGet("/weather", () => Results.Ok(new[] { "Sunny", "Cloudy" }))
    .RequirePermission("weather.read");

// Entra endpoints: Graph path
var graphApi = app.MapGroup("/api/graph")
    .RequireAuthorization("EntraGraphApi");

graphApi.MapGet("/me", () => Results.Ok());
```

## 3.3 RBAC consistency for Entra tokens (choose one)

**Option A (keeps “single local RBAC”):** enrich Entra-authenticated principals with local permissions in the API before authorization (e.g., via `IClaimsTransformation` or a custom authorization handler that queries the IdP/DB by Entra `oid`).

**Option B (simpler):** split endpoint policy: OpenIddict-protected endpoints use local permission policies; Entra-protected Graph endpoints use Entra scopes only. If you choose this, update the “single local RBAC” claim accordingly.

### Minimal enrichment example (Option A)

```csharp
public sealed class EntraPermissionClaimsTransformation(
    IPermissionLookup permissionLookup) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only enrich Entra-authenticated identities.
        if (!principal.Identities.Any(i => i.AuthenticationType == "Entra"))
            return principal;

        var oid = principal.FindFirstValue("oid");
        if (string.IsNullOrWhiteSpace(oid))
            return principal;

        var permissions = await permissionLookup.GetPermissionsForEntraOidAsync(oid);
        var id = new ClaimsIdentity();
        foreach (var p in permissions)
            id.AddClaim(new Claim("permission", p));

        principal.AddIdentity(id);
        return principal;
    }
}

// Register:
builder.Services.AddScoped<IClaimsTransformation, EntraPermissionClaimsTransformation>();
```

## 3.2 `appsettings.Development.json`
```json
{
  "Idp": {
    "Authority": "https://localhost:7241"
  }
}
```

---

# 4) Aspire AppHost (DProcess.AppHost)

In AppHost, wire up the three services. Exact code depends on the Aspire template version, but conceptually:

### `Program.cs` (conceptual)
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.DProcess_Idp>("idp")
    .WithExternalHttpEndpoints();

var api = builder.AddProject<Projects.DProcess_Api>("api")
    .WithReference(idp);

var bff = builder.AddProject<Projects.DProcess_Bff>("bff")
    .WithReference(idp)
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

---

# 5) Initial permissions seeding (so you can test quickly)

Inherit the seeding approach from **Demo3.BffRbac**:
- `demo3/Demo3.BffRbac/Data/DbSeeder.cs`

For demo4.2, port that seeder into `DProcess.Idp` and keep the same role/permission matrix:
- Permissions: `weather.read`, `weather.write`, `users.read`, `users.write`, `users.delete`, `reports.view`, `reports.export`
- Roles: `Admin`, `Manager`, `User`
- Users: `admin@local.app`, `manager@local.app`, `user@local.app`

If you want, I’ll provide the adapted seeding code in the next message.

---

# 6) How this meets your requirements

- **Login page with 2 options:** implemented in IdP UI; easy to add more providers later.
- **Local Identity + Entra ID:** local username/password via Identity; Entra via external OIDC.
- **BFF with YARP and Aspire:** BFF is a classic OIDC client + YARP forwarder; orchestrated by Aspire.
- **Single local RBAC with permission claims:** permissions are computed in IdP from local DB and emitted into access tokens **and** ID token/UserInfo for BFF policy checks.
- **Passkey support:** Identity uses `IdentitySchemaVersions.Version3` and exposes the Identity endpoints.

---

# 7) OBO flow (On-Behalf-Of)
Research: `demo4.2/.docs/reference/research-06-obo-flow.md`

### Current setup
This plan **does not implement OBO**. The BFF proxies the IdP-issued access token to `DProcess.Api`.

### OBO feasibility in this architecture (Microsoft Graph only)
OBO to **Microsoft Graph** requires the **incoming access token** to be issued by **Entra**. Since demo4.2 issues tokens from **OpenIddict**, those tokens **cannot** be used directly for Graph OBO.

### Path A (selected): Entra authority for downstream Graph access
We will keep OpenIddict for the IdP, but **switch the downstream path to Entra** so that `DProcess.Api` can perform OBO for Graph.

**Flow (high level):**
```
Browser → DProcess.Bff (login)
  ├─ Local auth via OpenIddict (IdP cookie + local access token)
  └─ Entra OIDC (secondary) for Graph-enabled access

DProcess.Bff → DProcess.Api (Bearer token issued by Entra)
DProcess.Api → Microsoft Graph (OBO using the incoming Entra access token)
```

**What changes in practice:**
- **BFF adds Entra OIDC** (in addition to OpenIddict) for the **Graph‑enabled path**.  
  The BFF obtains an **Entra access token** and calls `DProcess.Api` with it (e.g., via YARP transform or a dedicated Graph path like `/api/graph/*`).
- **DProcess.Api switches to Microsoft.Identity.Web** for the Graph path:  
  `AddMicrosoftIdentityWebApi(...)` + `EnableTokenAcquisitionToCallDownstreamApi()` + `AddDownstreamApi("Graph", ...)`.
- **OBO happens in DProcess.Api** (server‑to‑server) using the incoming Entra access token.

**Security boundaries:**
- OpenIddict remains the **local IdP** for app auth + permission claims.
- Entra becomes the **authority** only for the **Graph‑enabled downstream path**.

**Scope of OBO in demo4.2:**
- OBO is **only** used for Microsoft Graph.  
- Other APIs continue to accept OpenIddict tokens with permission claims.

### Required clarification: multi-issuer handling
If Path A is enabled, the API must explicitly handle **two token issuers** (OpenIddict and Entra). See **3.2** for the multi-scheme configuration and split endpoint policy.

### Required clarification: RBAC consistency
Entra tokens do **not** carry the local `permission` claims. You must either:
- **Enrich** Entra principals with local permissions before authorization (keeps “single local RBAC”), or
- **Split** policies: local RBAC for OpenIddict endpoints, Entra scopes for Graph endpoints (simpler, but update the “single local RBAC” statement).

**Non‑goals:**
- No RFC 8693 token‑exchange bridge in demo4.2.

### Consent vs token (why the first consent isn’t enough)
- **Consent grants permission** for a specific **client app** to request Graph tokens; it does **not** produce a reusable Graph access token.
- Graph access tokens are **short‑lived** and **audience‑specific**. They must be issued by Entra **for the client app that will call Graph**.
- In this design, the **IdP’s Entra app registration** (used for external login) is a **different client** from the **BFF/Api’s Entra app registration** (used for Graph/OBO), so consent does not automatically apply across them.

### Entra app registrations (two-app demo setup)
We use **two** Entra app registrations for Path A:

**App 1: IdP external login (DProcess.Idp.External)**  
Purpose: federate user login into the local IdP.

1) Microsoft Entra admin center → **App registrations** → **New registration**  
2) Name: `DProcess.Idp.External`  
   - Supported account types: **Single tenant**  
3) After creation, record **Tenant ID** and **Client ID**  
4) **Authentication** → **Add a platform** → **Web**  
   - Redirect URI: `https://localhost:7241/signin-entra`  
5) **Certificates & secrets** → **New client secret**  
6) Use these values in `DProcess.Idp` `appsettings.Development.json` under `Entra:*`

**App 2: Graph OBO client + API (DProcess.Graph.OBO)**  
Purpose: BFF obtains Entra tokens for the API; API performs OBO to Microsoft Graph.

1) Microsoft Entra admin center → **App registrations** → **New registration**  
2) Name: `DProcess.Graph.OBO`  
   - Supported account types: **Single tenant**  
3) Record **Tenant ID** and **Client ID**  
4) **Authentication** → **Add a platform** → **Web**  
   - Redirect URI: `https://localhost:7181/signin-oidc` (BFF)  
5) **Expose an API**  
   - Application ID URI: `api://<client-id>`  
   - Add scope: `access_as_user` (used by BFF to call `DProcess.Api`)  
6) **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated**  
   - Minimum for demo: `User.Read`  
   - Grant admin consent  
7) **Certificates & secrets** → **New client secret**  
8) Optional (recommended for OBO): set `accessTokenAcceptedVersion` = `2` in the manifest  
9) Configure **BFF** to request scopes:  
   - `api://<client-id>/access_as_user`  
   - `User.Read`  
10) Configure **API** with Microsoft.Identity.Web for OBO and Graph:
    - `AddMicrosoftIdentityWebApi(...)`  
    - `EnableTokenAcquisitionToCallDownstreamApi()`  
    - `AddMicrosoftGraph(...)`

> Note: This combines BFF and API into a single Entra app registration for demo simplicity. If you prefer strict separation, split App 2 into **one web app** (BFF) + **one web API** (Api) and grant delegated permissions accordingly.

---

# Two important notes before you run it

## A) Ports and redirect URIs
You must align:
- IdP HTTPS port (e.g. `7241`)
- BFF HTTPS port (e.g. `7181`)
- Entra app registration redirect URI for IdP: `https://localhost:7241/signin-entra`
- OpenIddict client redirect URI for BFF: `https://localhost:7181/signin-oidc`

## B) Token endpoint handling
No controller action is required for `/connect/token`. OpenIddict handles it directly via the ASP.NET Core host integration.

---

# Tell me what you want next (pick one)

1. **Full runnable repo-level code** (every file including `App.razor`, layouts, launchSettings, migrations, seed data)
2. **Database migrations + seeding** (admin user + permissions + assign perms)
3. **Multi-tenant-safe external login keying** (issuer/tenant binding for Entra)
4. **Add “link/unlink external accounts” UI** (so users can connect Entra later)
