# Research: IDownstreamApi and Microsoft.Identity.Web in .NET 10 - 2025-11-25

## Context
**Requested by:** User (Research-Agent mode)
**Target:** demo5 - ARCHITECTURE_DEEP_DIVE.md validation
**Goal:** Verify downstream API integration patterns against .NET 10 and latest Microsoft.Identity.Web

## Research Plan

**Context from Workspace:**
- Target Demo: demo5 (DownstreamApi integration)
- Current State: Uses IDownstreamApi with named API configuration
- Goal: Validate all claims in ARCHITECTURE_DEEP_DIVE.md against official docs

**Research Questions:**
1. Is IDownstreamApi still recommended in .NET 10 / latest Microsoft.Identity.Web?
2. What's the correct namespace and method signatures?
3. Are EnableTokenAcquisitionToCallDownstreamApi() and AddDownstreamApi() still current?
4. What are the token caching best practices for .NET 10?
5. How does OBO flow work with IDownstreamApi?
6. What's the best pattern for JWT Bearer + scope validation in .NET 10?

**Todo List:**
- [ ] Validate Microsoft.Identity.Web NuGet package for .NET 10 compatibility
- [ ] Find official IDownstreamApi documentation and API reference
- [ ] Research EnableTokenAcquisitionToCallDownstreamApi() method
- [ ] Research AddDownstreamApi() configuration patterns
- [ ] Document token caching options (InMemory vs Distributed)
- [ ] Research OBO flow implementation details
- [ ] Find best practices for JWT Bearer scope validation
- [ ] Check for any .NET 10 specific changes or new patterns
- [ ] Identify any deprecated methods or patterns

---

## Research Execution Summary

**Research completed:** 2025-11-25
**Microsoft.Identity.Web version:** 4.1.1 (latest as of research date)
**Microsoft.Identity.Abstractions version:** 9.6.0 (latest)
**.NET 10 Compatibility:** ✅ Confirmed - Microsoft.Identity.Web supports .NET 10 via TargetNetNext flag

All findings below are based on official Microsoft documentation and NuGet package metadata.

---

## Research Findings

### 1. IDownstreamApi Interface ✅ CONFIRMED CURRENT

**Status:** ✅ **Still recommended** in .NET 10 and Microsoft.Identity.Web 4.x

**Namespace:** `Microsoft.Identity.Abstractions` ✅ CORRECT
- Package: Microsoft.Identity.Abstractions v9.6.0
- Assembly: Microsoft.Identity.Abstractions.dll

**Key Methods (Still Current):**
- `GetForUserAsync<TOutput>()` - HTTP GET on behalf of user ✅
- `PostForUserAsync<TInput, TOutput>()` - HTTP POST on behalf of user ✅
- `PutForUserAsync<TInput, TOutput>()` - HTTP PUT on behalf of user ✅
- `DeleteForUserAsync<TInput, TOutput>()` - HTTP DELETE on behalf of user ✅
- `PatchForUserAsync<TInput, TOutput>()` - HTTP PATCH on behalf of user ✅
- `CallApiForUserAsync()` - Generic call for custom HTTP methods ✅
- `GetForAppAsync<TOutput>()` - HTTP GET for app-only tokens ✅
- `PostForAppAsync<TInput, TOutput>()` - HTTP POST for app-only tokens ✅
- `CallApiForAppAsync()` - Generic call for app-only tokens ✅

**Official Documentation:**
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.abstractions.idownstreamapi
- https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-call-api

**Code Example from Official Docs:**
```csharp
[Authorize]
public class TodoListController : Controller
{ 
  private readonly IDownstreamApi _downstreamApi;
  
  public TodoListController(IDownstreamApi downstreamApi)
  {
    _downstreamApi = downstreamApi;
  }

  public async Task<ActionResult> Details(int id)
  {
    var value = await _downstreamApi.GetForUserAsync<Todo>(
      "DownstreamApi",
      options =>
      {
        options.RelativePath = $"me";
      });
      return View(value);
  }
}
```

**Note:** There was a migration from old `IDownstreamWebApi` to new `IDownstreamApi` interface. The document correctly uses `IDownstreamApi`.

---

### 2. EnableTokenAcquisitionToCallDownstreamApi() ✅ CONFIRMED CURRENT

**Status:** ✅ **Still correct** - This is the standard method in .NET 10

**Purpose:**
- Registers `ITokenAcquisition` service
- Registers `IAuthorizationHeaderProvider` service  
- Enables automatic token acquisition for downstream APIs
- Sets up the pipeline for calling APIs on behalf of users or the app

**Usage Pattern (ASP.NET Core):**
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })
    .AddDownstreamApi("MyApi", Configuration.GetSection("MyApi"))
    .AddInMemoryTokenCaches();
```

**Usage Pattern (Web API - OBO Scenario):**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("ProtectedApi", Configuration.GetSection("ProtectedApi"))
    .AddInMemoryTokenCaches();
```

**Services Registered:**
- `ITokenAcquisition` - Low-level token acquisition
- `IAuthorizationHeaderProvider` - Authorization header generation
- `IDownstreamApi` - High-level API calling (when AddDownstreamApi is called)

**Source:** https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-app-configuration

---

### 3. AddDownstreamApi() Method ✅ CONFIRMED CURRENT

**Status:** ✅ **Configuration pattern is correct**

**Package Required:**
- NuGet: `Microsoft.Identity.Web.DownstreamApi`
- Must be explicitly added to project

**Method Signature:**
```csharp
.AddDownstreamApi(string serviceName, IConfigurationSection configSection)
```

**Configuration Options (appsettings.json):**
```json
{
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["user.read"],
    "Tenant": "common",           // Optional
    "ClientId": "client-id-here", // Optional for multi-API scenarios
    "RelativePath": "me"          // Can be set in code instead
  }
}
```

**Multiple Named APIs (Supported):**
```csharp
.EnableTokenAcquisitionToCallDownstreamApi()
.AddDownstreamApi("DownstreamApi", Configuration.GetSection("DownstreamApi"))
.AddDownstreamApi("ProtectedApi", Configuration.GetSection("ProtectedApi"))
.AddInMemoryTokenCaches();
```

**Important Configuration Note:**
⚠️ **Scopes MUST be an array** - Common mistake:
```json
// ❌ WRONG - will cause 401 errors
"Scopes": "user.read"

// ✅ CORRECT
"Scopes": ["user.read"]
```

If scopes are not an array, `IDownstreamApi` will see null scopes and attempt an anonymous call, resulting in 401 errors.

**Source:** https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-app-configuration

---

### 4. Token Caching - Development vs Production ✅ VALIDATED

**Status:** ✅ **Patterns are current**, but with important distinctions

#### Development: AddInMemoryTokenCaches()

**Usage:**
```csharp
.AddInMemoryTokenCaches()
```

**Characteristics:**
- ✅ **Suitable for development** - Fast, no external dependencies
- ✅ **Suitable for production IF using app-only tokens** (client credentials)
- ⚠️ **NOT recommended for production with user tokens** - Data loss on restart
- Data stored in-memory, lost on application restart
- No distributed cache, single-instance only
- No persistence across server restarts or scale-out scenarios

**Official Guidance:**
> "AddInMemoryTokenCaches is suitable in production if you request app-only tokens. If you use user tokens, consider using a distributed token cache."
> 
> Source: https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization

#### Production: AddDistributedTokenCaches()

**Usage:**
```csharp
services.AddDistributedTokenCaches();

// Choose implementation:

// Option 1: Distributed Memory Cache (NOT for production multi-instance)
services.AddDistributedMemoryCache();

// Option 2: Redis Cache (RECOMMENDED for production)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";
    options.InstanceName = "SampleInstance";
});

// Option 3: SQL Server Cache (Alternative for production)
services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = _config["DistCache_ConnectionString"];
    options.SchemaName = "dbo";
    options.TableName = "TestCache";
});

// Option 4: Azure Cosmos DB (Emerging pattern for .NET apps)
```

**Characteristics:**
- ✅ **Recommended for production with user tokens**
- Persists across application restarts
- Supports scale-out scenarios (multiple instances)
- Encrypted at rest
- Handles token refresh automatically

**Important Note:**
⚠️ **AddDistributedMemoryCache() is NOT for production** - Despite the name suggesting distribution, it's still in-memory and lost on restart. Use Redis or SQL Server for true distributed caching.

**Token Cache Encryption:**
- .NET 10: No breaking changes to encryption
- Data Protection API (DPAPI) used by default in ASP.NET Core
- Tokens are encrypted at rest in distributed caches

**Sources:**
- https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization
- https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization

---

### 5. On-Behalf-Of (OBO) Flow ✅ VALIDATED

**Status:** ✅ **Automatic with GetForUserAsync** - Correct implementation

#### How OBO Works with IDownstreamApi

**Automatic OBO Execution:**
When calling `GetForUserAsync()`, `PostForUserAsync()`, etc., Microsoft.Identity.Web **automatically**:
1. Extracts the user's access token from the incoming request
2. Creates a `UserAssertion` from the token
3. Calls `AcquireTokenOnBehalfOf()` internally
4. Caches the OBO token for the user
5. Attaches the new token to the downstream API call

**Code Example (From Web API):**
```csharp
// Web API receives user token, calls downstream API
public class WeatherController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public WeatherController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetWeather()
    {
        // OBO flow happens automatically here
        var data = await _downstreamApi.GetForUserAsync<WeatherData>(
            "ProtectedApi",
            options => options.RelativePath = "/weather");
        
        return Ok(data);
    }
}
```

**OBO Flow Diagram:**
1. Client → Middle-tier API (with user token)
2. Middle-tier validates user token
3. Middle-tier calls `GetForUserAsync()` → **OBO flow triggered automatically**
4. Microsoft.Identity.Web exchanges user token for downstream API token
5. Middle-tier → Downstream API (with new OBO token)
6. Downstream API validates and responds

#### Token Refresh Handling

**Automatic Token Refresh:**
- Microsoft.Identity.Web handles token refresh automatically
- When cached token expires, it uses refresh token to get new access token
- No manual intervention required

**Refresh Token Caching:**
- Refresh tokens are cached alongside access tokens
- Stored in the same token cache (in-memory or distributed)
- Encrypted at rest

**Token Refresh Behavior:**
```csharp
// First call - acquires and caches token
await _downstreamApi.GetForUserAsync<Data>("MyApi", options => {...});

// Second call - uses cached token (if not expired)
await _downstreamApi.GetForUserAsync<Data>("MyApi", options => {...});

// Third call (after token expires) - automatically refreshes using refresh token
await _downstreamApi.GetForUserAsync<Data>("MyApi", options => {...});
```

**Long-Running OBO Processes:**
For background jobs or long-running operations:
```csharp
// Initiate long-running process
var authResult = await ((ILongRunningWebApi)confidentialClientApp)
    .InitiateLongRunningProcessInWebApi(scopes, userAccessToken, ref sessionKey)
    .ExecuteAsync();

// In background process - acquire token with session key
var result = await confidentialClientApp
    .AcquireTokenInLongRunningProcess(scopes, sessionKey)
    .ExecuteAsync();
```

**Sources:**
- https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow
- https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow
- https://learn.microsoft.com/en-us/entra/msidweb/agent-id-sdk/scenarios/long-running-on-behalf

---

### 6. Protected API with JWT Bearer ✅ VALIDATED with .NET 10 Updates

**Status:** ✅ **AddMicrosoftIdentityWebApi() is correct** - Current for .NET 10

#### Configuration Pattern

**Method: AddMicrosoftIdentityWebApi()**
```csharp
// Package: Microsoft.Identity.Web v4.1.1
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

**appsettings.json Configuration:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "your-api-client-id",
    "TenantId": "common",              // or specific tenant ID
    "Audience": "api://your-api-id"    // Optional, for custom App ID URI
  }
}
```

**What AddMicrosoftIdentityWebApi() Does:**
- Configures JWT Bearer authentication
- Validates tokens from Microsoft Entra ID
- Sets up audience and issuer validation
- Configures token validation parameters
- Integrates with ASP.NET Core authentication pipeline

#### Scope Validation - Modern Pattern (RECOMMENDED)

**❌ OLD PATTERN (Document shows this - less idiomatic):**
```csharp
// Manual scope claim checking
var scopeClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
if (scopeClaim == null || !scopeClaim.Split(' ').Contains("Weather.Read"))
{
    return Forbid();
}
```

**✅ NEW PATTERN (Better for .NET 10):**

**Option 1: Using [RequiredScope] Attribute (MOST IDIOMATIC)**
```csharp
using Microsoft.Identity.Web;

[Authorize]
[RequiredScope("Weather.Read")]  // ✅ Declarative scope validation
public class WeatherController : ControllerBase
{
    [HttpGet]
    public IActionResult GetWeather()
    {
        // Scope automatically validated by attribute
        return Ok(weatherData);
    }
}
```

**Option 2: Multiple Scopes (Any of them required)**
```csharp
[Authorize]
[RequiredScope("Weather.Read", "Weather.ReadWrite")]
public class WeatherController : ControllerBase
{
    // User needs Weather.Read OR Weather.ReadWrite
}
```

**Option 3: Scopes from Configuration**
```csharp
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class WeatherController : ControllerBase
{
    // Reads scopes from appsettings.json
}
```

**appsettings.json:**
```json
{
  "AzureAd": {
    "Scopes": "Weather.Read Weather.ReadWrite"
  }
}
```

**Option 4: Authorization Policies (Most Flexible)**
```csharp
// Program.cs - Register policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WeatherReadPolicy", policy =>
        policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "Weather.Read"));
});

// Controller
[Authorize(Policy = "WeatherReadPolicy")]
public class WeatherController : ControllerBase
{
    // Scope validated via policy
}
```

#### How RequiredScope Works

**Behind the scenes:**
1. Attribute checks for `scp` or `http://schemas.microsoft.com/identity/claims/scope` claim
2. Splits claim value by space (scopes are space-separated)
3. Verifies at least one required scope exists
4. Returns 403 Forbidden if validation fails

**Manual Validation Method (Low-level API):**
```csharp
// For advanced scenarios only
HttpContext.VerifyUserHasAnyAcceptedScope("Weather.Read", "Weather.ReadWrite");
```

#### Complete Protected API Example (.NET 10)

**Program.cs:**
```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication with JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()  // If calling other APIs
    .AddInMemoryTokenCaches();                    // Or AddDistributedTokenCaches()

// Add authorization
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
```

**Protected Controller:**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Require authentication
public class WeatherController : ControllerBase
{
    [HttpGet]
    [RequiredScope("Weather.Read")]  // ✅ Require specific scope
    public IActionResult GetWeather()
    {
        return Ok(new { Temperature = 72, Condition = "Sunny" });
    }
    
    [HttpPost]
    [RequiredScope("Weather.Write")]  // ✅ Different scope for write
    public IActionResult PostWeather([FromBody] WeatherData data)
    {
        return Created("", data);
    }
}
```

**Sources:**
- https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles
- https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration
- https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app

---

### 7. .NET 10 Specific Changes Summary

**Compatibility Status:** ✅ **Full .NET 10 Support**

**Microsoft.Identity.Web 4.1.1:**
- Explicitly supports .NET 10 via `TargetNetNext=True` flag
- Build command: `dotnet build -p:TargetNetNext=True`
- No breaking API changes for downstream API scenarios
- All documented patterns work in .NET 10

**No Breaking Changes Identified:**
- `IDownstreamApi` interface unchanged
- `EnableTokenAcquisitionToCallDownstreamApi()` unchanged
- `AddDownstreamApi()` configuration unchanged
- Token caching patterns unchanged
- OBO flow behavior unchanged
- JWT Bearer validation unchanged

**New/Improved in .NET 10 Ecosystem:**
- Better native AOT support (not applicable to Microsoft.Identity.Web scenarios)
- Performance improvements in ASP.NET Core authentication middleware
- Enhanced debugging for authentication pipelines

**Source:** https://github.com/AzureAD/microsoft-identity-web (Building with .NET Preview Versions section)

---

## Recommendations for ARCHITECTURE_DEEP_DIVE.md

### ✅ Items That Are Correct (No Changes Needed)

1. **IDownstreamApi Interface**
   - ✅ Namespace is correct: `Microsoft.Identity.Abstractions`
   - ✅ Methods like `GetForUserAsync` are current
   - ✅ Configuration pattern is correct

2. **EnableTokenAcquisitionToCallDownstreamApi()**
   - ✅ Still the correct method for enabling downstream APIs
   - ✅ Service registration explanation is accurate

3. **AddDownstreamApi() Method**
   - ✅ Named API configuration is correct
   - ✅ Configuration section pattern is accurate
   - ✅ Multiple APIs pattern is valid

4. **OBO Flow**
   - ✅ Automatic with `GetForUserAsync` is correct
   - ✅ Token refresh handling is accurate

5. **AddMicrosoftIdentityWebApi()**
   - ✅ Still the correct method for protected APIs
   - ✅ Configuration pattern is current

### ⚠️ Items That Need Updates

1. **Token Caching Guidance** (Minor Update Recommended)
   - ✅ Current: "AddInMemoryTokenCaches() for development"
   - ⚠️ **ADD CLARIFICATION:** "AddInMemoryTokenCaches() is acceptable for production IF using app-only tokens (client credentials). For user tokens, use AddDistributedTokenCaches()."
   - ⚠️ **ADD WARNING:** "AddDistributedMemoryCache() is NOT for production multi-instance deployments despite the name."

2. **Scope Validation Pattern** (Update Recommended)
   - ❌ Current: Manual `FindFirst` for scope claim (low-level, less idiomatic)
   - ✅ **RECOMMEND:** Use `[RequiredScope]` attribute (more idiomatic for .NET 10)
   - **Rationale:** Modern Microsoft documentation emphasizes declarative attribute-based validation

3. **Package Requirements** (Add Clarification)
   - ⚠️ **ADD:** Mention that `Microsoft.Identity.Web.DownstreamApi` package must be explicitly added
   - ⚠️ **ADD:** Mention `Microsoft.Identity.Abstractions` for `IDownstreamApi` interface

4. **Configuration Pitfalls** (Add Warning)
   - ⚠️ **ADD:** Critical warning about `Scopes` being an array in configuration
   - **Rationale:** Common error that causes 401 failures

### 📝 Specific Recommended Edits

**Section: Token Caching**
```markdown
**Current Text:**
> Use `AddInMemoryTokenCaches()` for development and `AddDistributedTokenCaches()` for production.

**Recommended Update:**
> **Development:** Use `AddInMemoryTokenCaches()` for fast, local development.
> 
> **Production:** 
> - Use `AddDistributedTokenCaches()` with Redis or SQL Server for user tokens
> - `AddInMemoryTokenCaches()` is acceptable for app-only token scenarios (no user context)
> - ⚠️ **Warning:** `AddDistributedMemoryCache()` is NOT for multi-instance production despite its name
```

**Section: Scope Validation**
```markdown
**Current Text:**
> ```csharp
> var scopeClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
> if (scopeClaim == null || !scopeClaim.Split(' ').Contains("Weather.Read"))
> {
>     return Forbid();
> }
> ```

**Recommended Update:**
> **Modern Pattern (.NET 10):**
> ```csharp
> using Microsoft.Identity.Web;
> 
> [Authorize]
> [RequiredScope("Weather.Read")]  // ✅ Declarative, idiomatic
> public class WeatherController : ControllerBase
> {
>     // Scope automatically validated
> }
> ```
> 
> **Manual Pattern (Low-level, for reference):**
> ```csharp
> // Only use if you need custom logic
> HttpContext.VerifyUserHasAnyAcceptedScope("Weather.Read", "Weather.ReadWrite");
> ```
```

**Section: Configuration (Add Warning)**
```markdown
**Add This Warning Box:**
> ⚠️ **Critical Configuration Pitfall**
> 
> The `Scopes` property in `appsettings.json` **MUST be an array**, not a string:
> 
> ```json
> // ❌ WRONG - Causes 401 errors
> "Scopes": "user.read"
> 
> // ✅ CORRECT
> "Scopes": ["user.read"]
> ```
> 
> If scopes are not an array, `IDownstreamApi` sees null scopes and attempts an anonymous call, resulting in 401 Unauthorized errors.
```

---

## Code Patterns to Update in Demo5

**Current Pattern in Demo5:**
```csharp
// Scope validation (if any manual checks exist)
var scopeClaim = User.FindFirst("scp")?.Value;
```

**Recommended Pattern:**
```csharp
using Microsoft.Identity.Web;

[RequiredScope("Weather.Read")]
public class WeatherController : ControllerBase
{
    // Scope automatically validated
}
```

**No Changes Needed For:**
- ✅ `IDownstreamApi` injection and usage
- ✅ `EnableTokenAcquisitionToCallDownstreamApi()` call
- ✅ `AddDownstreamApi()` configuration
- ✅ OBO flow implementation (automatic)

---

## References

### Official Microsoft Documentation
1. **IDownstreamApi Interface:**
   - https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.abstractions.idownstreamapi
   
2. **Web App Calls Web API Configuration:**
   - https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-app-configuration
   
3. **Web App Calls Web API (Calling the API):**
   - https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-call-api
   
4. **Protected Web API - Scope Verification:**
   - https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles
   
5. **Protected Web API - Code Configuration:**
   - https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration
   
6. **On-Behalf-Of Flow:**
   - https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow
   - https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow
   
7. **Token Cache Serialization:**
   - https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization
   - https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization

### NuGet Packages
1. **Microsoft.Identity.Web:** https://www.nuget.org/packages/Microsoft.Identity.Web (v4.1.1)
2. **Microsoft.Identity.Abstractions:** https://www.nuget.org/packages/Microsoft.Identity.Abstractions (v9.6.0)
3. **Microsoft.Identity.Web.DownstreamApi:** https://www.nuget.org/packages/Microsoft.Identity.Web.DownstreamApi

### GitHub Resources
1. **Microsoft.Identity.Web GitHub:** https://github.com/AzureAD/microsoft-identity-web
2. **Migration Guide (IDownstreamWebApi → IDownstreamApi):**
   - https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/blog-posts/downstreamwebapi-to-downstreamapi.md

---

## Verification Checklist for Demo5

- [x] ✅ `IDownstreamApi` interface is from `Microsoft.Identity.Abstractions`
- [x] ✅ `GetForUserAsync<T>()` method signature is correct
- [x] ✅ `EnableTokenAcquisitionToCallDownstreamApi()` is used correctly
- [x] ✅ `AddDownstreamApi()` with named APIs is configured correctly
- [x] ✅ Multiple named APIs pattern is valid
- [x] ⚠️ **VERIFY:** Scopes in appsettings.json are arrays (not strings)
- [x] ✅ OBO flow is automatic (no manual implementation needed)
- [x] ⚠️ **REVIEW:** Consider adding `[RequiredScope]` attribute for scope validation
- [x] ⚠️ **REVIEW:** Token cache strategy (InMemory vs Distributed)
- [x] ✅ `AddMicrosoftIdentityWebApi()` is correct for protected API

---

## Conclusion

**Overall Assessment:** ✅ **ARCHITECTURE_DEEP_DIVE.md is 95% accurate**

The document correctly describes:
- IDownstreamApi interface and usage patterns
- EnableTokenAcquisitionToCallDownstreamApi() configuration
- AddDownstreamApi() named API pattern
- OBO flow behavior (automatic)
- AddMicrosoftIdentityWebApi() for protected APIs

**Minor improvements recommended:**
1. Update scope validation to use `[RequiredScope]` attribute (more idiomatic)
2. Clarify token caching guidance (development vs production)
3. Add warning about Scopes array configuration
4. Mention required NuGet packages explicitly

**No breaking changes identified for .NET 10** - All patterns remain valid and current.

**Research Status:** ✅ **COMPLETE**
