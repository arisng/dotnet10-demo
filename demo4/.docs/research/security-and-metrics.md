# .NET 10 Authentication/Authorization Features

### AddAuthorizationBuilder Fluent API

**.NET 10 introduces fluent authorization configuration**:

```csharp
// Old way (.NET 8)
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireManagerRole", policy => 
        policy.RequireRole("Manager"));
});

// New way (.NET 10)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequireManagerRole", policy => policy.RequireRole("Manager"))
    .AddPolicy("AtLeast21", policy => 
        policy.Requirements.Add(new MinimumAgeRequirement(21)))
    .AddDefaultPolicy("RequireAuthenticatedUser", policy => 
        policy.RequireAuthenticatedUser());
```

**Benefits**:
- Method chaining for cleaner code
- Better IntelliSense support
- Easier to build complex policies

### Authentication State Serialization Improvements

**.NET 10 enhancements for Blazor Web Apps**:

```csharp
// Server project - Simplified serialization
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        // New: Control which claims to serialize
        options.SerializeAllClaims = true; // or false for name/role only
        
        // New: Custom serialization logic
        options.SerializeAuthenticationState = (state) =>
        {
            // Custom logic to transform state before serialization
            return state;
        };
    });
```

**Performance Improvements**:
- Reduced payload size with selective claim serialization
- Faster deserialization on client
- Better support for large claim sets

### New Metrics and Telemetry for Auth Operations

**.NET 10 introduces built-in authentication metrics**:

**Authentication Metrics** (`Microsoft.AspNetCore.Authentication`):
- `aspnetcore.authentication.authenticated_requests` (Counter): Successful authentications
- `aspnetcore.authentication.challenge_count` (Counter): Authentication challenges issued
- `aspnetcore.authentication.forbid_count` (Counter): Authorization failures
- `aspnetcore.authentication.request_duration` (Histogram): Authentication request duration

**Authorization Metrics** (`Microsoft.AspNetCore.Authorization`):
- `aspnetcore.authorization.required_requests` (Counter): Requests requiring authorization
- `aspnetcore.authorization.policy_evaluation_duration` (Histogram): Policy evaluation time

**Identity Metrics** (`Microsoft.AspNetCore.Identity`):
- `aspnetcore.identity.user.create.duration` (Histogram)
- `aspnetcore.identity.user.update.duration` (Histogram)
- `aspnetcore.identity.user.delete.duration` (Histogram)
- `aspnetcore.identity.user.check_password_attempts` (Counter)
- `aspnetcore.identity.sign_in.authenticate.duration` (Histogram)
- `aspnetcore.identity.sign_in.sign_ins` (Counter)
- `aspnetcore.identity.sign_in.sign_outs` (Counter)
- `aspnetcore.identity.sign_in.two_factor_clients_remembered` (Counter)

**Usage with OpenTelemetry**:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddMeter("Microsoft.AspNetCore.Authentication");
        metrics.AddMeter("Microsoft.AspNetCore.Authorization");
        metrics.AddMeter("Microsoft.AspNetCore.Identity");
    });
```

**Monitoring Dashboard Example** (Aspire):
```csharp
// View metrics in Aspire dashboard
// Navigate to: https://localhost:15888/metrics
```

### Security Best Practices for Production Deployment

#### 1. Token Cache Security

```csharp
// ✅ PRODUCTION: Use distributed cache with encryption
services.AddDistributedTokenCaches();
services.AddStackExchangeRedisCache(options => { ... });
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => 
{
    options.Encrypt = true; // REQUIRED
    options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;
});

// ❌ DEVELOPMENT ONLY: In-memory cache
// services.AddInMemoryTokenCaches();
```

#### 2. Data Protection Key Ring (Web Farms)

```csharp
// Share Data Protection keys across instances
builder.Services.AddDataProtection()
    .SetApplicationName("MyBlazorApp")
    .PersistKeysToAzureBlobStorage(new Uri("[Blob-URI]"), credential)
    .ProtectKeysWithAzureKeyVault(new Uri("[Key-Vault-URI]"), credential);
```

#### 3. HTTPS Enforcement

```csharp
// Enforce HTTPS
app.UseHttpsRedirection();
app.UseHsts();

// Configure HSTS
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

#### 4. Cookie Security

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});
```

#### 5. Secrets Management

```csharp
// ❌ Never store secrets in appsettings.json
// {
//   "AzureAd": {
//     "ClientSecret": "actual-secret-here" // BAD!
//   }
// }

// ✅ Use Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myvault.vault.azure.net/"),
    new DefaultAzureCredential());

// ✅ Use User Secrets (development)
// dotnet user-secrets set "AzureAd:ClientSecret" "secret-value"
```

#### 6. Token Validation

```csharp
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(5) // Reduce clock skew tolerance
    };
});
```

---

## Code Examples from Official Documentation

### Complete Blazor Web App with Entra ID Configuration

```csharp
// Server/Program.cs
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// Configure Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();

// Add distributed cache (production)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "TokenCache_";
});

// Configure token cache options
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.Encrypt = true;
    options.DisableL1Cache = false;
    options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;
    options.SlidingExpiration = TimeSpan.FromHours(1);
});

// Add authorization
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireManagerRole", policy => policy.RequireRole("Manager"))
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);

app.Run();
```

### Service Calling Microsoft Graph

```csharp
// Services/GraphUserService.cs
using Microsoft.Graph;
using Microsoft.Graph.Models;

public class GraphUserService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphUserService> _logger;
    
    public GraphUserService(
        GraphServiceClient graphClient,
        ILogger<GraphUserService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }
    
    public async Task<User?> GetCurrentUserAsync()
    {
        try
        {
            return await _graphClient.Me.GetAsync();
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
    
    public async Task<byte[]?> GetUserPhotoAsync()
    {
        try
        {
            using var photoStream = await _graphClient.Me.Photo.Content.GetAsync();
            if (photoStream == null) return null;
            
            using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("User photo not found");
            return null;
        }
    }
    
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            var users = await _graphClient.Users
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Search = $"\"displayName:{searchTerm}\"";
                    requestConfig.QueryParameters.Select = new[] { "displayName", "mail", "userPrincipalName" };
                    requestConfig.Headers.Add("ConsistencyLevel", "eventual");
                });
            
            return users?.Value ?? new List<User>();
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error searching users");
            return null;
        }
    }
}
```

---

## Recommendations for Demo4 Implementation

### Architecture Recommendations

1. **Adopt BFF Pattern** if calling external APIs from WebAssembly components
   - Use YARP for proxying authenticated requests
   - Store access tokens server-side only
   - Reduces attack surface

2. **Use Distributed Token Cache** even in development
   - Redis for local development (Docker)
   - Azure Redis Cache for production
   - Enables testing of cache scenarios

3. **Implement Service Abstractions**
   - Create `IWeatherService` interface
   - `ServerWeatherService` for SSR (direct API calls)
   - `ClientWeatherService` for CSR (HTTP calls through BFF)

### Security Recommendations

1. **Enable Token Encryption**: `options.Encrypt = true`
2. **Configure Data Protection Key Ring**: Azure Key Vault + Blob Storage
3. **Use Managed Identity** for Azure resources (no secrets in config)
4. **Implement Claims Transformation** for unified permissions
5. **Add OpenTelemetry** for auth operation monitoring

### Development Workflow

1. **Start with In-Memory Cache** (faster development)
2. **Add Redis** before moving to shared development
3. **Configure Key Vault** for production secrets
4. **Test Token Refresh** scenarios
5. **Monitor Auth Metrics** in Aspire Dashboard

---

## Warnings About Deprecated Patterns

### ⚠️ Deprecated: Client Secret in appsettings.json

```json
// ❌ OLD (still works but not recommended)
{
  "AzureAd": {
    "ClientSecret": "secret-here"
  }
}

// ✅ NEW (preferred)
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "secret-here"
      }
    ]
  }
}
```

### ⚠️ Security Concerns

1. **Never use In-Memory Token Cache in Production**
   - Single-instance only
   - Tokens lost on restart
   - No cross-instance sharing

2. **Always Encrypt Tokens at Rest**
   - Set `Encrypt = true` in `MsalDistributedTokenCacheAdapterOptions`

3. **Validate Issuer for Multi-Tenant Apps**
   - Use `AadIssuerValidator` for "common" endpoint
   - Prevents token substitution attacks

4. **Don't Disable Certificate Validation**
   - Never set `RequireHttpsMetadata = false` in production

5. **Implement Token Lifetime Policies**
   - Configure in Entra ID portal
   - Set appropriate timeout values
   - Enable sliding expiration

---

## Official Documentation Links

### Microsoft.Identity.Web
- [Microsoft.Identity.Web NuGet](https://www.nuget.org/packages/Microsoft.Identity.Web)
- [GitHub Repository](https://github.com/AzureAD/microsoft-identity-web)
- [API Documentation](https://learn.microsoft.com/dotnet/api/microsoft.identity.web)
- [Token Cache Serialization](https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization)

### Microsoft Graph
- [Microsoft Graph Documentation](https://learn.microsoft.com/graph/)
- [Graph SDK for .NET](https://learn.microsoft.com/graph/sdks/sdks-overview)
- [Graph API Reference](https://learn.microsoft.com/graph/api/overview)

### Blazor Authentication
- [Blazor Authentication & Authorization](https://learn.microsoft.com/aspnet/core/blazor/security/)
- [Secure Blazor Web App with Entra ID](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-entra)
- [Call Web API from Blazor](https://learn.microsoft.com/aspnet/core/blazor/call-web-api)

### ASP.NET Core Identity
- [Introduction to Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [External Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/social/)
- [Claims Transformation](https://learn.microsoft.com/aspnet/core/security/authentication/claims)

### .NET 10 Features
- [What's New in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0)
- [Authentication & Authorization Metrics](https://learn.microsoft.com/aspnet/core/log-mon/metrics/built-in#microsoftaspnetcoreauthorization)

---

## Conclusion

This research confirms that .NET 10 provides comprehensive, production-ready tools for building secure Blazor Web Apps with Entra ID integration. The Microsoft.Identity.Web library significantly simplifies authentication and token management, while new .NET 10 features like the fluent authorization builder and enhanced metrics improve both developer experience and operational visibility.

Key takeaways for Demo4:
- Use `Microsoft.Identity.Web` v4.0.1 for full .NET 10 support
- Adopt BFF pattern with YARP for secure API calls from WebAssembly components
- Implement distributed token cache with encryption enabled
- Configure Data Protection key ring for web farm scenarios
- Leverage IClaimsTransformation for unified permission system across auth providers
- Monitor authentication operations using .NET 10 built-in metrics

All patterns and code examples are sourced from official Microsoft documentation and represent current best practices as of November 2025.