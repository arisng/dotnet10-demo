# Microsoft.Identity.Web (Latest Version)

### Current Status & .NET 10 Compatibility

- **Package**: `Microsoft.Identity.Web` v4.0.1 (Latest Stable)
- **Full .NET 10 Compatibility**: ✅ Confirmed
- **Repository**: [AzureAD/microsoft-identity-web](https://github.com/AzureAD/microsoft-identity-web)

### Core Configuration Pattern

The standard configuration pattern for Blazor Web Apps:

```csharp
// In server project Program.cs
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches(); // or AddDistributedTokenCaches()
```

**Configuration in appsettings.json**:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "[Application-Client-ID]",
    "TenantId": "[Directory-Tenant-ID]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "[Client-Secret]"
      }
    ]
  }
}
```

### AddMicrosoftIdentityWebApp Configuration

**Key Parameters**:
- `configSectionName`: Default is "AzureAd"
- `openIdConnectScheme`: Default is "OpenIdConnect"
- `cookieScheme`: Default is "Cookies"
- `subscribeToOpenIdConnectMiddlewareDiagnosticsEvents`: Set to `true` for troubleshooting

**Advanced Configuration**:

```csharp
.AddMicrosoftIdentityWebApp(
    configureMicrosoftIdentityOptions: options => {
        options.Authority = "https://login.microsoftonline.com/{TenantId}";
        options.ClientId = "{ClientId}";
        options.CallbackPath = "/signin-oidc";
    },
    configureCookieAuthenticationOptions: options => {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
```

### EnableTokenAcquisitionToCallDownstreamApi

**Purpose**: Enables token acquisition for calling downstream APIs (e.g., Microsoft Graph, custom APIs)

**Usage Patterns**:

```csharp
// Pattern 1: With initial scopes
.EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })

// Pattern 2: Without initial scopes (incremental consent)
.EnableTokenAcquisitionToCallDownstreamApi()

// Pattern 3: With configuration
.EnableTokenAcquisitionToCallDownstreamApi(
    options => Configuration.Bind("AzureAd", options), 
    initialScopes)
```

**Exposes Services**:
- `IAuthorizationHeaderProvider`: For obtaining authorization headers
- `ITokenAcquisition`: For acquiring tokens programmatically

### Token Cache Options

#### In-Memory Token Cache (Development)

```csharp
.AddInMemoryTokenCaches()
```

**Use Case**: Local development, single-machine testing  
**Limitations**: Not suitable for production, data lost on restart

#### Distributed Token Caches (Production)

```csharp
.AddDistributedTokenCaches()

// Then choose implementation:
// 1. Distributed Memory Cache
services.AddDistributedMemoryCache();

// 2. Redis Cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "TokenCache";
});

// 3. SQL Server Cache
services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = Configuration["DistCache_ConnectionString"];
    options.SchemaName = "dbo";
    options.TableName = "TokenCache";
});
```

#### Token Cache Configuration Options

```csharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => 
{
    // Disable L1 cache for debugging (set to false for production)
    options.DisableL1Cache = false;
    
    // Set L1 cache size limit (default: 500 MB)
    options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;
    
    // Enable token encryption at rest (REQUIRED for production)
    options.Encrypt = true;
    
    // Token sliding expiration (default: 1 hour)
    options.SlidingExpiration = TimeSpan.FromHours(1);
});
```

**⚠️ Production Requirements**:
- Always use distributed token cache (Redis, SQL Server, Cosmos DB)
- Enable encryption (`Encrypt = true`)
- Configure shared Data Protection key ring for web farms

### Best Practices for Hybrid Authentication (Local + Entra ID)

**Scenario**: Supporting both local ASP.NET Core Identity and Entra ID external login

**Key Considerations**:

1. **Multiple Authentication Schemes**:

```csharp
services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"), 
    openIdConnectScheme: "AzureAd",
    cookieScheme: null); // Use default cookie scheme
```

2. **Account Linking**: Use `IClaimsTransformation` to map external claims to local identity
3. **Unified Permission System**: Implement custom authorization policies that work across both providers

### AddAuthenticationStateSerialization for Blazor WASM

**Purpose**: Serializes server-side authentication state for client-side Blazor WebAssembly components

**Server Project** (Program.cs):

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(); // Serialize auth state

// Optional: Include ALL claims (not just name/role)
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(
        options => options.SerializeAllClaims = true);
```

**Client Project** (.Client/Program.cs):

```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization(); // Deserialize auth state
```

**How It Works**:
- Uses `PersistentComponentState` to serialize authentication state into HTML comments
- Client-side reads and deserializes state from HTML
- Authentication state is **fixed** for WebAssembly app lifetime
- Requires full page reload for login/logout

**API Reference**:
- [AddAuthenticationStateSerialization](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.webassemblyrazorcomponentsbuilderextensions.addauthenticationstateserialization)
- [AddAuthenticationStateDeserialization](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.webassemblyauthenticationservicecollectionextensions.addauthenticationstatedeserialization)