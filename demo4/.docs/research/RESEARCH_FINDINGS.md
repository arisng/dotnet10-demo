# Microsoft Entra ID & .NET 10 Authentication Research Findings

Research conducted: November 23, 2025  
Target: Demo4 - Blazor Web App with Entra ID Integration

---

## Executive Summary

This document contains comprehensive research findings from official Microsoft documentation covering Microsoft.Identity.Web, Microsoft Graph API integration, Entra ID authentication patterns, hybrid identity scenarios, and .NET 10 authentication/authorization features. All information is sourced from Microsoft Learn documentation and official code samples.

---

## 1. Microsoft.Identity.Web (Latest Version)

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

---

## 2. Microsoft Graph API Integration with ASP.NET Core

### IDownstreamApi Usage Patterns

**Package**: `Microsoft.Identity.Web.DownstreamApi`

**Configuration**:

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": [ "user.read", "user.readbasic.all" ]
  }
}
```

**Service Registration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", 
        builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();
```

**Usage in Controllers/Services**:

```csharp
public class UserController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public UserController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    public async Task<IActionResult> GetUserProfile()
    {
        // Call Graph API on behalf of user
        var user = await _downstreamApi.GetForUserAsync<User>(
            "DownstreamApi",
            options => options.RelativePath = "me"
        );
        
        return Ok(user);
    }
}
```

**Strongly-Typed Calls**:

```csharp
// GET request with typed response
var user = await _downstreamApi.CallApiForUserAsync<User>(
    "DownstreamApi",
    options =>
    {
        options.HttpMethod = HttpMethod.Get;
        options.RelativePath = "me";
    });

// POST request with body and response
var createdItem = await _downstreamApi.CallApiForUserAsync<InputModel, OutputModel>(
    "DownstreamApi",
    inputData,
    options =>
    {
        options.HttpMethod = HttpMethod.Post;
        options.RelativePath = "users";
    });
```

### Microsoft Graph SDK Integration

**Package**: `Microsoft.Identity.Web.GraphServiceClient`

**Configuration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();
```

**Usage**:

```csharp
public class GraphController : ControllerBase
{
    private readonly GraphServiceClient _graphClient;
    
    public GraphController(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }
    
    public async Task<IActionResult> GetMe()
    {
        var user = await _graphClient.Me
            .GetAsync();
        
        return Ok(user);
    }
    
    public async Task<IActionResult> GetPhoto()
    {
        try
        {
            using var photoStream = await _graphClient.Me
                .Photo
                .Content
                .GetAsync();
                
            return File(photoStream, "image/jpeg");
        }
        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("No photo available");
        }
    }
}
```

### On-Behalf-Of (OBO) Flow Implementation

**Scenario**: Web API needs to call downstream API on behalf of the user

**Web API Configuration**:

```csharp
// API Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("Graph", builder.Configuration.GetSection("GraphAPI"))
    .AddInMemoryTokenCaches();
```

**API Controller**:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserDataController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public UserDataController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        // OBO token exchange happens automatically
        var response = await _downstreamApi.CallApiForUserAsync(
            "Graph",
            options =>
            {
                options.RelativePath = "/me";
            });
            
        return Ok(await response.Content.ReadFromJsonAsync<User>());
    }
}
```

**How OBO Works**:
1. Client calls Web API with access token (Token A)
2. Web API validates Token A
3. Web API calls Microsoft Entra ID with Token A to request Token B for downstream API
4. Microsoft Entra ID validates Token A and issues Token B
5. Web API calls downstream API with Token B

**Key Requirements**:
- API must have `api://{clientId}/access_as_user` scope exposed
- Client must request delegated permission to the API
- API must request delegated permission to downstream API (e.g., Graph)

### Recommended Scopes for User Profile and Photo Access

**Microsoft Graph API Scopes**:

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": [
      "user.read",           // Read signed-in user profile
      "user.readbasic.all",  // Read all users' basic profiles
      "user.readwrite",      // Read and write signed-in user profile
      "profile",             // Read user's profile
      "email",               // Read user's email address
      "openid"               // Sign in and read user profile
    ]
  }
}
```

**For Photo Access**:
- `User.Read` (includes profile photo)
- `User.ReadBasic.All` (basic info only, no photo)

**Graph API Endpoints**:
- User profile: `GET /me`
- User photo: `GET /me/photo/$value`
- User photo metadata: `GET /me/photo`
- All users: `GET /users`

### Error Handling and Retry Policies

**Handling MFA/Conditional Access Requirements**:

```csharp
try
{
    var result = await _downstreamApi.CallApiForUserAsync(...);
}
catch (MsalUiRequiredException ex)
{
    // User needs to complete MFA or accept consent
    // Return 401 with WWW-Authenticate header containing claims challenge
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.Add("WWW-Authenticate", 
        $"Bearer claims=\"{ex.Claims}\", error=\"{ex.Message}\"");
    return;
}
```

**Client Handling Claims Challenge**:

```csharp
// Client receives 401 and initiates new auth flow with claims
var authResult = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
    scopes,
    claims: claimsFromWwwAuthenticate);
```

**Retry Policy for Transient Errors**:

```csharp
builder.Services.AddHttpClient("Graph", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
})
.AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

---

## 3. Entra ID Authentication in Blazor Web Apps (.NET 10)

### Cookie-Based Authentication for BFF Pattern

**Backend for Frontend (BFF) Pattern**: Server-side app proxies requests to APIs with user's access token

**Server Configuration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", 
        builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();

// Add YARP for proxying
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

**YARP Configuration** (appsettings.json):

```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "api": {
            "Address": "https://api.example.com/"
          }
        }
      }
    }
  }
}
```

**Add Authorization Header Transform**:

```csharp
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var tokenAcquisition = context.RequestServices
            .GetRequiredService<ITokenAcquisition>();
        
        var accessToken = await tokenAcquisition
            .GetAccessTokenForUserAsync(new[] { "api://xxx/.default" });
        
        context.Request.Headers.Authorization = $"Bearer {accessToken}";
        
        await next();
    });
});
```

### OpenID Connect Configuration Best Practices

**Full Configuration Example**:

```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        Configuration.Bind("AzureAd", options);
        
        // Set metadata address explicitly if needed
        options.MetadataAddress = 
            $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
        
        // Configure token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };
        
        // Disable inbound claim mapping for cleaner claim names
        options.MapInboundClaims = false;
        
        // Enable debugging events
        options.Events = new OpenIdConnectEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Redirect("/Error");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
```

**⚠️ Important Settings**:

1. **MapInboundClaims = false**: Prevents renaming claims (keep original JWT claim names)
2. **NameClaimType = "name"**: Use "name" claim instead of legacy SOAP claim
3. **RoleClaimType = "roles"**: Use "roles" claim for role-based authorization

### Redirect URIs and Callback Paths

**Configuration**:

```json
{
  "AzureAd": {
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "RemoteSignOutPath": "/signout-oidc"
  }
}
```

**App Registration (Entra Portal)**:
- Add redirect URI: `https://localhost:{port}/signin-oidc`
- Add post-logout redirect URI: `https://localhost:{port}/signout-callback-oidc`
- For production: Use actual domain instead of localhost

**⚠️ Note**: Port is not required for `localhost` addresses when using Microsoft Entra ID

### Claims Mapping from Entra ID to ASP.NET Core Identity

**Default Entra ID Claims**:
- `oid`: Object ID (unique user identifier)
- `tid`: Tenant ID
- `name`: User's display name
- `preferred_username`: User's email/UPN
- `roles`: Application roles assigned to user
- `groups`: Group memberships (if configured)

**Mapping to ASP.NET Core Identity**:

```csharp
public class ClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
{
    public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        
        // Map Entra ID claims to local claims
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        
        // Add roles
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        
        return new ClaimsPrincipal(identity);
    }
}
```

---

## 4. Hybrid Identity Scenarios

### Supporting Multiple Authentication Providers (Local + External)

**Challenge**: Allow users to sign in with local accounts OR Entra ID

**Solution**: Configure multiple authentication schemes

```csharp
services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddMicrosoftIdentityWebApp(
    Configuration.GetSection("AzureAd"),
    openIdConnectScheme: "AzureAd",
    cookieScheme: null) // Use shared cookie scheme
.AddGoogle(options =>
{
    options.ClientId = Configuration["Google:ClientId"];
    options.ClientSecret = Configuration["Google:ClientSecret"];
});
```

**Login Page Example**:

```razor
<h4>Use a local account to log in.</h4>
<form method="post" asp-page-handler="LocalLogin">
    <input asp-for="Input.Email" />
    <input asp-for="Input.Password" type="password" />
    <button type="submit">Log in</button>
</form>

<h4>Use an external account to log in.</h4>
<form method="post" asp-page-handler="ExternalLogin">
    <button type="submit" name="provider" value="AzureAd">
        Log in with Microsoft
    </button>
    <button type="submit" name="provider" value="Google">
        Log in with Google
    </button>
</form>
```

### IClaimsTransformation for Unified Permission System

**Purpose**: Transform/add claims after authentication to create unified authorization across providers

**Implementation**:

```csharp
public class CustomClaimsTransformation : IClaimsTransformation
{
    private readonly IUserRoleService _userRoleService;
    
    public CustomClaimsTransformation(IUserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }
    
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only run transformation once
        if (principal.HasClaim(c => c.Type == "transformed"))
            return principal;
        
        var identity = (ClaimsIdentity)principal.Identity;
        
        // Get user identifier (works for both local and external)
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("oid");
        
        // Load application-specific roles from database
        var appRoles = await _userRoleService.GetUserRolesAsync(userId);
        
        foreach (var role in appRoles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        
        // Add custom claims
        identity.AddClaim(new Claim("app_permission", "read:data"));
        identity.AddClaim(new Claim("transformed", "true"));
        
        return principal;
    }
}
```

**Registration**:

```csharp
builder.Services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();
```

**⚠️ Important**: `TransformAsync` can be called multiple times per request - always check if transformation already applied

### Account Linking Strategies

**Scenario**: User signs in with Entra ID but needs to link to existing local account

**Strategy 1: Link on First External Login**

```csharp
public async Task<IActionResult> OnPostExternalLoginCallbackAsync(
    string returnUrl = null, string remoteError = null)
{
    var info = await _signInManager.GetExternalLoginInfoAsync();
    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    
    // Check if user with this email already exists
    var existingUser = await _userManager.FindByEmailAsync(email);
    
    if (existingUser != null)
    {
        // Link external login to existing user
        var addLoginResult = await _userManager.AddLoginAsync(
            existingUser, info);
        
        if (addLoginResult.Succeeded)
        {
            await _signInManager.SignInAsync(existingUser, isPersistent: false);
            return LocalRedirect(returnUrl);
        }
    }
    
    // Create new user...
}
```

**Strategy 2: Manual Account Linking (Post-Login)**

```csharp
[Authorize]
public async Task<IActionResult> LinkExternalAccount(string provider)
{
    // Start external authentication
    var redirectUrl = Url.Action("LinkLoginCallback", "Account");
    var properties = _signInManager.ConfigureExternalAuthenticationProperties(
        provider, redirectUrl, _userManager.GetUserId(User));
    return Challenge(properties, provider);
}

public async Task<IActionResult> LinkLoginCallback()
{
    var info = await _signInManager.GetExternalLoginInfoAsync();
    var user = await _userManager.GetUserAsync(User);
    
    var result = await _userManager.AddLoginAsync(user, info);
    
    if (result.Succeeded)
    {
        return RedirectToAction("ManageExternalLogins");
    }
    
    return RedirectToAction("Error");
}
```

**Database Schema** (UserLogins table):

```csharp
public class ApplicationUser : IdentityUser
{
    public ICollection<IdentityUserLogin<string>> Logins { get; set; }
}

// Query linked accounts
var linkedAccounts = await _userManager.GetLoginsAsync(user);
```

### External Login Provider Configuration

**Configure in Program.cs**:

```csharp
// ASP.NET Core Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// External providers
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"),
        openIdConnectScheme: "AzureAd")
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"];
        options.ClientSecret = builder.Configuration["Google:ClientSecret"];
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Facebook:AppId"];
        options.AppSecret = builder.Configuration["Facebook:AppSecret"];
    });
```

**Configuration (appsettings.json)**:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "[Client-ID]",
    "TenantId": "[Tenant-ID]",
    "CallbackPath": "/signin-oidc"
  },
  "Google": {
    "ClientId": "[Google-Client-ID]",
    "ClientSecret": "[Google-Client-Secret]"
  },
  "Facebook": {
    "AppId": "[Facebook-App-ID]",
    "AppSecret": "[Facebook-App-Secret]"
  }
}
```

---

## 5. .NET 10 Authentication/Authorization Features

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
- `aspnetcore.authentication.sign_in_count` (Counter): User sign-ins
- `aspnetcore.authentication.sign_out_count` (Counter): User sign-outs
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
            return new List<User>();
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
