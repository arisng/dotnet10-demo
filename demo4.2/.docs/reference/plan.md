Below is a **.NET 10** sample solution using 
- **OpenIddict** for my IdP implementation
- **ASP.NET Core Identity** for local username/password
- **Microsoft Entra ID** as an external provider
- **Blazor Web App BFF with YARP** orchestrated with **Aspire**. 
It implements a **single local RBAC** that emits **`permission` claims** into access tokens regardless of login method.
RBAC is role-based (role → permission), with optional user-level overrides.

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
src/
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
    .AddInteractiveWebAssemblyComponents()
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
        options.RegisterScopes("openid", "profile", "email", "api");
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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();
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
                    Permissions.ResponseTypes.Code,

                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.OpenId,
                    "scp:api"
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
        return Ok(new
        {
            sub = User.GetClaim(Claims.Subject),
            name = User.GetClaim(Claims.Name),
            email = User.GetClaim(Claims.Email)
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("weather.read", policy => policy.AddRequirements(new PermissionRequirement("weather.read")));
    options.AddPolicy("weather.write", policy => policy.AddRequirements(new PermissionRequirement("weather.write")));
    options.AddPolicy("users.read", policy => policy.AddRequirements(new PermissionRequirement("users.read")));
    options.AddPolicy("users.write", policy => policy.AddRequirements(new PermissionRequirement("users.write")));
    options.AddPolicy("users.delete", policy => policy.AddRequirements(new PermissionRequirement("users.delete")));
    options.AddPolicy("reports.view", policy => policy.AddRequirements(new PermissionRequirement("reports.view")));
    options.AddPolicy("reports.export", policy => policy.AddRequirements(new PermissionRequirement("reports.export")));
});

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

## 2.1 Program.cs

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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

    // Helps avoid claim mapping surprises
    options.MapInboundClaims = false;
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

## 3.1 Program.cs
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

This plan **does not implement OBO**. It only proxies the IdP-issued access token to the API via the BFF.

If OBO is required later, add a dedicated downstream API integration that exchanges the user token for a new token (e.g., via RFC 8693 token exchange or Microsoft Identity Web for Entra-based OBO). That requires new configuration in IdP/BFF and is out of scope for this demo4.2 plan.

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
