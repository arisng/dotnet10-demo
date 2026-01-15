# Demo4 Implementation Guidance

Based on official Microsoft documentation research (see `RESEARCH_FINDINGS.md`)

---

## Critical Updates Required for Demo4

### 1. Token Cache Configuration (REQUIRED FOR PRODUCTION)

**Current README mentions**: `AddInMemoryTokenCaches()`

**Research Finding**: ⚠️ **In-memory cache is development-only**

**Update Required**:

```csharp
// Development: In-memory (acceptable)
.AddInMemoryTokenCaches()

// Production: Distributed cache with encryption (REQUIRED)
.AddDistributedTokenCaches();

// Choose cache implementation:
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration["Redis:ConnectionString"];
    options.InstanceName = "TokenCache_";
});

// CRITICAL: Enable encryption
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.Encrypt = true; // REQUIRED for production
    options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;
    options.SlidingExpiration = TimeSpan.FromHours(1);
});
```

### 2. Microsoft.Identity.Web Version

**Current**: v4.1.0 ✅ (Latest stable, .NET 10 compatible)

**Confirmed**: No updates needed - using latest version

### 3. AddAuthenticationStateSerialization Enhancement

**Current README shows**: Basic usage

**Research Finding**: .NET 10 adds `SerializeAllClaims` option

**Enhanced Implementation**:

```csharp
// Server: Serialize authentication state
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        // NEW in .NET 10: Control claim serialization
        options.SerializeAllClaims = true; // Include permission claims
    });

// Client: Deserialize authentication state
builder.Services.AddAuthenticationStateDeserialization();
```

### 4. Authorization Builder Pattern (.NET 10)

**Current demo3 uses**: `services.AddAuthorization(options => ...)`

**Research Finding**: .NET 10 introduces fluent `AddAuthorizationBuilder()`

**Update for demo4**:

```csharp
// Old way (demo3)
services.AddAuthorization(options =>
{
    options.AddPolicy("weather.read", policy => 
        policy.RequireClaim("permission", "weather.read"));
});

// New way (demo4 - .NET 10 pattern)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("weather.read", policy => 
        policy.RequireClaim("permission", "weather.read"))
    .AddPolicy("weather.write", policy => 
        policy.RequireClaim("permission", "weather.write"))
    .AddPolicy("users.read", policy => 
        policy.RequireClaim("permission", "users.read"));
```

### 5. OpenID Connect Configuration Best Practice

**Research Finding**: Disable inbound claim mapping for cleaner claim names

**Add to demo4 configuration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        Configuration.Bind("AzureAd", options);
        
        // NEW: Disable inbound claim mapping (recommended)
        options.MapInboundClaims = false;
        
        // Configure token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "roles"
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", 
        builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();
```

### 6. Client Secret Configuration Format

**Current README shows**:

```json
{
  "AzureAd": {
    "ClientSecret": "YOUR-CLIENT-SECRET"
  }
}
```

**Research Finding**: New format preferred (but old format still works)

**Recommended Update**:

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "YOUR-CLIENT-SECRET"
      }
    ]
  }
}
```

### 7. Data Protection Key Ring (Web Farms)

**Not in current README**

**Research Finding**: CRITICAL for production deployments with multiple instances

**Add to demo7 (Production Hardening)**:

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("Demo4.EntraIntegration")
    .PersistKeysToAzureBlobStorage(new Uri("[Blob-URI]"), credential)
    .ProtectKeysWithAzureKeyVault(new Uri("[Key-Vault-URI]"), credential);
```

### 8. Cookie Security Configuration

**Add to demo4 for production-ready baseline**:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Strict breaks OIDC
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});
```

---

## Enhanced ApplicationUser Model

**Based on research findings**, extend beyond README's basic model:

```csharp
public class ApplicationUser : IdentityUser
{
    // Basic Entra ID mapping (README)
    public string? ExternalAuthenticationProvider { get; set; }
    public string? EntraObjectId { get; set; }
    public string? DisplayName { get; set; }
    public string? JobTitle { get; set; }
    
    // ENHANCED: Additional Graph data
    public string? Department { get; set; }
    public string? OfficeLocation { get; set; }
    public string? MobilePhone { get; set; }
    public DateTime? LastGraphSync { get; set; }
    
    // Navigation properties
    public ICollection<IdentityUserLogin<string>> Logins { get; set; }
}
```

---

## Microsoft Graph Service Implementation

**Complete implementation based on official SDK patterns**:

```csharp
// Services/IGraphService.cs
public interface IGraphService
{
    Task<UserProfile?> GetUserProfileAsync();
    Task<byte[]?> GetUserPhotoAsync();
    Task SyncUserProfileToLocalAsync(string userId);
}

// Services/GraphService.cs
public class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GraphService> _logger;
    
    public GraphService(
        GraphServiceClient graphClient,
        UserManager<ApplicationUser> userManager,
        ILogger<GraphService> logger)
    {
        _graphClient = graphClient;
        _userManager = userManager;
        _logger = logger;
    }
    
    public async Task<UserProfile?> GetUserProfileAsync()
    {
        try
        {
            var user = await _graphClient.Me.GetAsync();
            
            if (user == null) return null;
            
            return new UserProfile
            {
                DisplayName = user.DisplayName,
                Email = user.Mail ?? user.UserPrincipalName,
                JobTitle = user.JobTitle,
                Department = user.Department,
                OfficeLocation = user.OfficeLocation,
                MobilePhone = user.MobilePhone
            };
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error fetching user profile from Graph API");
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
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error fetching user photo from Graph API");
            return null;
        }
    }
    
    public async Task SyncUserProfileToLocalAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.ExternalAuthenticationProvider != "Entra") return;
        
        var profile = await GetUserProfileAsync();
        if (profile == null) return;
        
        user.DisplayName = profile.DisplayName;
        user.JobTitle = profile.JobTitle;
        user.Department = profile.Department;
        user.OfficeLocation = profile.OfficeLocation;
        user.MobilePhone = profile.MobilePhone;
        user.LastGraphSync = DateTime.UtcNow;
        
        await _userManager.UpdateAsync(user);
    }
}

// Shared/Models/UserProfile.cs
public class UserProfile
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? OfficeLocation { get; set; }
    public string? MobilePhone { get; set; }
}
```

---

## Enhanced PermissionClaimsTransformation

**Complete implementation with Entra user creation**:

```csharp
public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionClaimsTransformation> _logger;
    
    public PermissionClaimsTransformation(
        IServiceProvider serviceProvider,
        ILogger<PermissionClaimsTransformation> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Prevent duplicate transformations
        if (principal.HasClaim(c => c.Type == "permission_transformed"))
            return principal;
        
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var permissionService = scope.ServiceProvider
            .GetRequiredService<IPermissionService>();
        
        var identity = (ClaimsIdentity)principal.Identity!;
        
        // Detect authentication source
        var oid = principal.FindFirstValue("oid");
        var isEntraUser = !string.IsNullOrEmpty(oid);
        
        ApplicationUser? user = null;
        
        if (isEntraUser)
        {
            // Find or create Entra user
            user = await userManager.Users
                .FirstOrDefaultAsync(u => u.EntraObjectId == oid);
            
            if (user == null)
            {
                user = await CreateEntraUserAsync(userManager, principal);
            }
        }
        else
        {
            // Local user
            var userId = userManager.GetUserId(principal);
            if (userId != null)
            {
                user = await userManager.FindByIdAsync(userId);
            }
        }
        
        if (user != null)
        {
            // Load permissions
            var permissions = await permissionService
                .GetUserPermissionsAsync(user.Id);
            
            foreach (var permission in permissions)
            {
                identity.AddClaim(new Claim("permission", permission));
            }
        }
        
        // Mark transformation complete
        identity.AddClaim(new Claim("permission_transformed", "true"));
        
        return principal;
    }
    
    private async Task<ApplicationUser> CreateEntraUserAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
    {
        var oid = principal.FindFirstValue("oid");
        var email = principal.FindFirstValue("preferred_username") 
            ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name");
        
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Entra ID validates email
            EntraObjectId = oid,
            ExternalAuthenticationProvider = "Entra",
            DisplayName = name
        };
        
        var result = await userManager.CreateAsync(user);
        
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create Entra user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(
                "Failed to create user from Entra ID");
        }
        
        _logger.LogInformation(
            "Created new Entra user: {Email} (OID: {Oid})", email, oid);
        
        return user;
    }
}
```

---

## Metrics & Telemetry (.NET 10)

**Add to Program.cs for comprehensive auth monitoring**:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        
        // NEW in .NET 10: Authentication & Authorization metrics
        metrics.AddMeter("Microsoft.AspNetCore.Authentication");
        metrics.AddMeter("Microsoft.AspNetCore.Authorization");
        metrics.AddMeter("Microsoft.AspNetCore.Identity");
        
        // Export to console (development) or OTLP (production)
        metrics.AddConsoleExporter();
    });
```

**Available Metrics**:
- `aspnetcore.authentication.authenticated_requests`
- `aspnetcore.authentication.challenge_count`
- `aspnetcore.authentication.sign_in_count`
- `aspnetcore.authorization.policy_evaluation_duration`
- `aspnetcore.identity.sign_in.sign_ins`

---

## Migration Script for ApplicationUser Changes

**Create migration for demo4**:

```powershell
cd demo4/Demo4.EntraIntegration
dotnet ef migrations add AddEntraIntegrationFields
```

**Migration content**:

```csharp
public partial class AddEntraIntegrationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalAuthenticationProvider",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EntraObjectId",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DisplayName",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "JobTitle",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);
            
        migrationBuilder.AddColumn<string>(
            name: "Department",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);
            
        migrationBuilder.AddColumn<DateTime>(
            name: "LastGraphSync",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        // Index for Entra Object ID lookups
        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_EntraObjectId",
            table: "AspNetUsers",
            column: "EntraObjectId",
            unique: true,
            filter: "EntraObjectId IS NOT NULL");
    }
}
```

---

## Updated appsettings.json

**Complete configuration with all research findings**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Identity.Web": "Information"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "YOUR-CLIENT-SECRET"
      }
    ]
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  },
  "MsalDistributedTokenCache": {
    "Encrypt": false,
    "DisableL1Cache": false,
    "L1CacheSizeLimit": 524288000,
    "SlidingExpiration": "01:00:00"
  }
}
```

---

## Testing Checklist

Based on research findings, comprehensive testing should cover:

### Authentication Tests
- ✅ Local passkey user sign-in
- ✅ Entra ID user sign-in
- ✅ First-time Entra user creation
- ✅ Returning Entra user sign-in
- ✅ Sign-out (both providers)
- ✅ Session timeout handling

### Authorization Tests
- ✅ Permission claims present for local users
- ✅ Permission claims present for Entra users
- ✅ API endpoints enforce correct permissions
- ✅ Unauthorized access returns 403
- ✅ Unauthenticated access returns 401

### Graph API Tests
- ✅ Fetch user profile (displayName, jobTitle, etc.)
- ✅ Fetch user photo (handle missing photo)
- ✅ Handle Graph API errors gracefully
- ✅ Token refresh on expiration

### State Serialization Tests
- ✅ Claims propagate to WASM components
- ✅ AuthorizeView works in WASM
- ✅ Permission claims visible in AuthStateProbe
- ✅ Photo displays in WASM component

### Metrics Tests
- ✅ Authentication events logged
- ✅ Authorization policy evaluation tracked
- ✅ Sign-in/sign-out counters increment
- ✅ Graph API call latency measured

---

## Production Deployment Checklist (Demo7 Preview)

1. ✅ Replace in-memory token cache with Redis/SQL Server
2. ✅ Enable token encryption (`Encrypt = true`)
3. ✅ Configure Data Protection key ring (Azure Key Vault + Blob Storage)
4. ✅ Move secrets to Azure Key Vault
5. ✅ Enable HTTPS/HSTS with proper configuration
6. ✅ Configure cookie security settings
7. ✅ Set up OpenTelemetry with OTLP exporter
8. ✅ Configure Application Insights
9. ✅ Update Entra app registration redirect URIs for production
10. ✅ Grant admin consent for API permissions

---

## Summary

This guidance document provides production-ready implementation patterns based on official Microsoft documentation. Key improvements over the initial README:

1. **Token cache security** with encryption and distributed storage
2. **.NET 10 fluent authorization builder** pattern
3. **Enhanced claim serialization** with `SerializeAllClaims`
4. **Complete Graph Service** with error handling
5. **Robust claims transformation** with Entra user creation
6. **Built-in metrics** for auth/authz operations
7. **Production checklist** for demo7

All patterns are sourced from official Microsoft Learn documentation and represent current best practices for .NET 10 Blazor Web Apps with Entra ID integration.
