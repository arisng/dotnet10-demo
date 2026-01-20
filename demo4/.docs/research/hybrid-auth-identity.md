# Entra ID Authentication in Blazor Web Apps (.NET 10)

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

# Hybrid Identity Scenarios

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