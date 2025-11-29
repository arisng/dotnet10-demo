# Research: BFF-to-API Architecture Patterns in .NET 10 - 2025-11-25

## Context

**Requested by:** User (Research-Agent mode)  
**Target:** demo5 (BFF with Downstream API pattern)  
**Goal:** Clarify architectural patterns for internal vs external API categorization, CORS necessity in server-to-server calls, and OAuth scope vs RBAC permission naming alignment

**Current Demo5 Architecture:**
- BFF server (port 7210): Blazor WebAssembly + Cookie auth + ASP.NET Core Identity
- ProtectedApi (port 7220): Separate downstream API with JWT Bearer auth
- Two downstream APIs registered: "DownstreamApi" (Microsoft Graph) and "ProtectedApi" (internal)
- ProtectedApi has CORS enabled for `https://localhost:7210`
- Naming mismatch: Entra scope `Forecast.Read` vs BFF permission `weather.read`

---

## Key Findings

### 1. Internal vs External API Categorization ✅

**Source:** Microsoft Docs - Backends for Frontends Pattern  
**URL:** https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends

#### Recommendation: Clear Categorization is Industry Best Practice

**Naming Convention Pattern:**
```
External APIs (SaaS/Third-party):
- "MicrosoftGraph", "DownstreamApi", "GraphBeta"
- Use vendor/service name for clarity

Internal APIs (Owned services):
- "{Domain}Api", "ProtectedApi", "Internal{Feature}Api"
- Use domain/feature name to indicate ownership
```

**Microsoft Identity Web Pattern:**
```json
{
  "DownstreamApi": {          // Generic name for Graph/external
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  },
  "GraphBeta": {              // Specific external service
    "BaseUrl": "https://graph.microsoft.com/beta",
    "Scopes": "user.read"
  },
  "ProtectedApi": {           // Internal owned API
    "BaseUrl": "https://localhost:7220",
    "Scopes": ["api://[CLIENT-ID]/Forecast.Read"]
  }
}
```

**Source:** Microsoft.Identity.Web documentation  
**Pattern:** Multiple downstream API registration is explicitly supported via `.AddDownstreamApi(name, config)`

#### Architectural Decision Record (ADR)

**Decision:** Distinguish internal vs external APIs through naming and configuration structure

**Rationale:**
1. **Security boundaries:** Internal APIs may have different trust assumptions than external SaaS
2. **Lifecycle management:** Internal APIs can be versioned/deployed independently
3. **Token acquisition:** External APIs use vendor-provided scopes; internal APIs use custom app ID URIs
4. **Monitoring/observability:** Different telemetry requirements for internal vs external dependencies

**Recommended Naming for Demo5:**
```json
{
  "MicrosoftGraph": {         // External SaaS API
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["User.Read"]
  },
  "WeatherApi": {             // Internal domain API (better than "ProtectedApi")
    "BaseUrl": "https://localhost:7220",
    "Scopes": ["api://[CLIENT-ID]/Forecast.Read"]
  }
}
```

**Code Pattern:**
```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(...)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("MicrosoftGraph", cfg.GetSection("MicrosoftGraph"))  // External
    .AddDownstreamApi("WeatherApi", cfg.GetSection("WeatherApi"))          // Internal
    .AddInMemoryTokenCaches();
```

---

### 2. CORS for BFF-to-Internal API Communication 🔒

**Source:** MDN Web Docs - Cross-Origin Resource Sharing (CORS)  
**URL:** https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS

#### Key Finding: CORS is NOT required for server-to-server HTTP calls

**CORS Technical Definition:**
- CORS is a **browser security mechanism**
- Enforced by the **browser's same-origin policy**
- Applies ONLY to requests initiated by **JavaScript running in a browser** (fetch/XMLHttpRequest)
- Does NOT apply to server-to-server HTTP requests (HttpClient in .NET)

**From MDN Documentation:**
> "CORS is an HTTP-header based mechanism that allows a server to indicate any origins (domain, scheme, or port) other than its own from which a **browser** should permit loading resources."

**What requests use CORS:**
- Invocations of `fetch()` or `XMLHttpRequest` from browser JavaScript
- Web Fonts loaded via CSS `@font-face`
- WebGL textures
- Images/video drawn to canvas

**What does NOT use CORS:**
- Server-side HTTP requests (HttpClient, HttpWebRequest, RestSharp, etc.)
- Non-browser clients (mobile apps, desktop apps, background services)

#### Architectural Analysis for Demo5

**Current Architecture:**
```
Browser (WASM) → BFF Server (7210) → ProtectedApi (7220)
   [Cookie]          [OBO Bearer]        [JWT Bearer]
```

**CORS Necessity by Request Type:**

1. **Browser → BFF Server (7210):**
   - Same origin: `https://localhost:7210` → `https://localhost:7210`
   - **CORS: NOT NEEDED** (same-origin policy satisfied)

2. **BFF Server → ProtectedApi (7220):**
   - Server-to-server: .NET HttpClient call with OBO token
   - **CORS: NOT NEEDED** (CORS is browser-only mechanism)
   - The request is made by the BFF server, not the browser

3. **If browser directly called ProtectedApi (NOT in demo5):**
   - Cross-origin: `https://localhost:7210` → `https://localhost:7220`
   - **CORS: REQUIRED** (different ports = different origin)

#### Security Implications

**Recommendation: REMOVE CORS from ProtectedApi in demo5**

**Why remove CORS:**
1. **Security principle:** ProtectedApi is an internal API that should NEVER be called directly from browsers
2. **Attack surface reduction:** Removing CORS headers prevents accidental browser-direct access
3. **Clear architectural intent:** No CORS = server-only API, enforces BFF pattern
4. **Defense in depth:** Even if JWT validation fails, CORS absence adds a layer of protection

**When to keep CORS on internal APIs:**
- **Hybrid architecture:** When the same API serves both BFF and browser clients (NOT recommended)
- **Development/debugging:** Temporarily for testing (disable in production)
- **Reverse proxy scenarios:** When YARP/API Gateway forwards browser requests

**Code Change for Demo5 ProtectedApi:**
```diff
// Remove CORS configuration
- builder.Services.AddCors(options =>
- {
-     options.AddPolicy("AllowBlazorApp", policy =>
-     {
-         policy.WithOrigins("https://localhost:7210")
-               .AllowAnyHeader()
-               .AllowAnyMethod();
-     });
- });

  var app = builder.Build();
  app.UseHttpsRedirection();
- app.UseCors("AllowBlazorApp");  // Remove this
  app.UseAuthentication();
  app.UseAuthorization();
```

**From Microsoft Docs - Enable CORS in ASP.NET Core:**
> "Browser security prevents a web page from making requests to a different domain than the one that served the web page. This restriction is called the same-origin policy."

**Key Quote:**
> "CORS is **not** a security feature, CORS **relaxes** security. An API is not safer by allowing CORS."

---

### 3. OAuth Scope vs RBAC Permission Naming Alignment ⚠️

**Source:** Microsoft Identity Platform - Scopes and Permissions  
**URL:** https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc

#### Key Finding: OAuth Scopes and RBAC Permissions are SEPARATE concerns

**Architectural Layers:**
```
┌─────────────────────────────────────┐
│  OAuth 2.0 / OIDC Layer             │  ← Entra ID scopes (Forecast.Read)
│  - Token acquisition                │
│  - Consent                           │
│  - Resource server authorization    │
└─────────────────────────────────────┘
           ↓ Access Token
┌─────────────────────────────────────┐
│  Application RBAC Layer             │  ← BFF permissions (weather.read)
│  - Fine-grained permissions         │
│  - Role-to-permission mapping       │
│  - Business logic authorization     │
└─────────────────────────────────────┘
```

#### OAuth Scope Definition (Entra ID)

**Purpose:** Authorize the CLIENT APPLICATION to access an API on behalf of a user  
**Defined in:** Entra ID App Registration → "Expose an API"  
**Format:** `api://{client-id}/{scope-name}` or `{app-id-uri}/{scope-name}`  
**Example:** `api://12345678-1234-1234-1234-123456789012/Forecast.Read`

**Characteristics:**
- Coarse-grained (API-level access)
- Consent-driven (user must consent)
- Appears in JWT access token `scp` claim
- Validated by downstream API (ProtectedApi)

**From Microsoft Docs:**
> "In OAuth 2.0, these types of permission sets are called *scopes*. They're also often referred to as *permissions*. In the Microsoft identity platform, a permission is represented as a string value."

**Naming Convention (from Microsoft Graph):**
```
Resource.Operation.Constraint
Examples:
- User.Read
- Mail.ReadWrite
- Directory.ReadWrite.All
- Calendars.Read
```

**Typical format:** PascalCase with dot notation, focused on **data/resource access**

#### Application RBAC Permission Definition (BFF)

**Purpose:** Authorize specific OPERATIONS within the application based on user roles  
**Defined in:** Application database (RolePermission table, app manifest)  
**Format:** Lowercase dot notation (convention varies)  
**Example:** `weather.read`, `users.delete`, `reports.export`

**Characteristics:**
- Fine-grained (operation-level access)
- Role-based (admin, manager, user)
- Managed internally by application
- Added as custom claims via IClaimsTransformation

**From Demo5 Code:**
```csharp
// BFF Authorization
.AddPolicy("weather.read", policy => 
    policy.AddRequirements(new PermissionRequirement("weather.read")))

// API endpoint
app.MapGet("/api/weather", [Authorize] () => { ... })
    .RequirePermission("weather.read");
```

**Naming Convention (from demo3-5):**
```
domain.operation
Examples:
- weather.read
- weather.write
- users.delete
- reports.export
```

**Typical format:** lowercase dot notation, focused on **business operations**

#### Relationship Analysis

**Question: Should scope names match permission names?**

**Answer: NO - They serve different purposes and operate at different layers**

**Comparison Matrix:**

| Aspect | OAuth Scope (Entra) | RBAC Permission (BFF) |
|--------|---------------------|----------------------|
| **Purpose** | API access consent | Operation authorization |
| **Granularity** | Coarse (API-level) | Fine (operation-level) |
| **Defined by** | API owner in Entra | Application developer |
| **Enforced by** | ProtectedApi JWT validation | BFF PermissionAuthorizationHandler |
| **User consent** | Required | Not required |
| **Naming** | `Resource.Action` (PascalCase) | `domain.operation` (lowercase) |
| **Examples** | `Forecast.Read`, `User.ReadWrite.All` | `weather.read`, `users.delete` |
| **Token claim** | `scp` or `scope` in JWT | Custom `permission` claims |
| **Lifecycle** | Managed in Entra portal | Managed in app database |

#### Real-World Example from Demo5

**Scenario:** Admin wants to view weather forecast

**Step 1: OAuth Scope Check (ProtectedApi)**
```csharp
// ProtectedApi validates JWT token
var scopeClaim = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
if (scopeClaim == null || !scopeClaim.Value.Contains("Forecast.Read"))
{
    return Results.Forbid();  // OAuth scope missing
}
```
✅ **Validates:** "Does this application/user have consent to access the Weather API?"

**Step 2: RBAC Permission Check (BFF)**
```csharp
// BFF validates user has weather.read permission
[Authorize(Policy = "weather.read")]
public class Weather : ComponentBase { }
```
✅ **Validates:** "Does this user's role grant them permission to read weather?"

**Both checks must pass:**
1. OAuth scope (`Forecast.Read`) → Proves BFF app has consent to call ProtectedApi
2. RBAC permission (`weather.read`) → Proves user's role allows weather viewing

#### Naming Recommendations

**For Entra ID Scopes (ProtectedApi):**
```
Format: Resource.Action[.Constraint]
Use PascalCase to align with Microsoft conventions

Examples:
✅ Forecast.Read
✅ Forecast.Write
✅ Weather.ReadWrite.All
✅ Report.Export

❌ weather.read (lowercase - confusing with RBAC)
❌ read_forecast (snake_case - non-standard)
```

**For BFF RBAC Permissions:**
```
Format: domain.operation
Use lowercase to distinguish from OAuth scopes

Examples:
✅ weather.read
✅ weather.write
✅ users.delete
✅ reports.export

❌ Weather.Read (PascalCase - confusing with OAuth)
❌ weather-read (kebab-case - uncommon for permissions)
```

#### Risks of Misalignment

**Current Demo5 Situation:**
- OAuth scope: `Forecast.Read` (ProtectedApi expects this)
- BFF permission: `weather.read` (BFF checks this)

**Risk Assessment: ✅ LOW RISK - This is CORRECT design**

**Why separation is good:**
1. **Flexibility:** Can map multiple BFF permissions to one OAuth scope
   - `weather.read` + `weather.write` → `Forecast.ReadWrite` scope
2. **Migration:** Can change internal permission names without updating Entra
3. **Multi-API:** Same BFF permission can call multiple downstream APIs
4. **Business logic:** BFF permissions reflect business operations, not API contracts

**When alignment causes problems:**
```csharp
// ❌ BAD: Tight coupling between OAuth and RBAC
if (user.HasScope("Forecast.Read"))  // Leaking OAuth concepts into app logic
{
    // Violates separation of concerns
}

// ✅ GOOD: Clear separation
if (user.HasPermission("weather.read"))  // Business domain language
{
    await downstreamApi.CallApiAsync("WeatherApi", ...);  // OAuth handled internally
}
```

#### Configuration Best Practice

**Demo5 Recommended Structure:**

**appsettings.json:**
```json
{
  "WeatherApi": {
    "BaseUrl": "https://localhost:7220",
    "Scopes": ["api://[CLIENT-ID]/Forecast.Read"]  // OAuth scope for API access
  }
}
```

**Program.cs (BFF):**
```csharp
// OAuth scope configuration (external concern)
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(...)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("WeatherApi", cfg.GetSection("WeatherApi"))
    .AddInMemoryTokenCaches();

// RBAC permission policies (internal concern)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("weather.read", policy => 
        policy.AddRequirements(new PermissionRequirement("weather.read")))
    .AddPolicy("weather.write", policy => 
        policy.AddRequirements(new PermissionRequirement("weather.write")));
```

**ProtectedApi Program.cs:**
```csharp
// Validate OAuth scope (external contract)
app.MapGet("/weather", [Authorize] (HttpContext httpContext) =>
{
    var scopeClaim = httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
    if (scopeClaim == null || !scopeClaim.Value.Contains("Forecast.Read"))
    {
        return Results.Forbid();
    }
    // ... return data
});
```

**Component (Blazor):**
```razor
@* RBAC permission check (internal concern) *@
@attribute [Authorize(Policy = "weather.read")]

<h3>Weather Forecast</h3>
@* Business logic using domain language *@
```

---

## Summary Recommendations for Demo5

### Question 1: Internal vs External API Categorization

✅ **Recommendation:** Rename "DownstreamApi" → "MicrosoftGraph" and "ProtectedApi" → "WeatherApi"

**Changes:**
```diff
// appsettings.json
- "DownstreamApi": {
+ "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["User.Read"]
  },
- "ProtectedApi": {
+ "WeatherApi": {
    "BaseUrl": "https://localhost:7220",
    "Scopes": ["api://[CLIENT-ID]/Forecast.Read"]
  }
```

```diff
// Program.cs
- .AddDownstreamApi("DownstreamApi", cfg.GetSection("DownstreamApi"))
- .AddDownstreamApi("ProtectedApi", cfg.GetSection("ProtectedApi"))
+ .AddDownstreamApi("MicrosoftGraph", cfg.GetSection("MicrosoftGraph"))
+ .AddDownstreamApi("WeatherApi", cfg.GetSection("WeatherApi"))
```

**Rationale:** Clear semantic separation between external SaaS (MicrosoftGraph) and internal domain API (WeatherApi)

---

### Question 2: CORS Necessity

✅ **Recommendation:** REMOVE CORS from ProtectedApi/WeatherApi

**Changes:**
```diff
// Demo5.ProtectedApi/Program.cs
- builder.Services.AddCors(options =>
- {
-     options.AddPolicy("AllowBlazorApp", policy =>
-     {
-         policy.WithOrigins("https://localhost:7210")
-               .AllowAnyHeader()
-               .AllowAnyMethod();
-     });
- });

  var app = builder.Build();
  app.UseHttpsRedirection();
- app.UseCors("AllowBlazorApp");
  app.UseAuthentication();
  app.UseAuthorization();

- .RequireCors("AllowBlazorApp");  // Remove from endpoints
```

**Rationale:**
1. CORS is NOT needed for server-to-server HTTP calls (BFF → ProtectedApi)
2. Removing CORS prevents accidental browser-direct access to internal API
3. Enforces true BFF pattern where browser only talks to BFF, never directly to downstream APIs

**Security Impact:** ✅ POSITIVE - Reduces attack surface, enforces architectural boundaries

---

### Question 3: OAuth Scope vs RBAC Permission Naming

✅ **Recommendation:** KEEP current separation (`Forecast.Read` ≠ `weather.read`)

**Rationale:**
1. OAuth scopes are external contracts (Entra ID managed)
2. RBAC permissions are internal business logic (app managed)
3. Separation allows flexibility in permission-to-scope mapping
4. Follows Microsoft Identity Web best practices

**No changes required** - Current implementation is correct!

**Optional Enhancement:** Add mapping documentation
```csharp
// WeatherService.cs - Document the mapping
public class ServerWeatherService : IWeatherService
{
    /// <summary>
    /// Fetches weather forecast from WeatherApi
    /// Requires:
    ///   - BFF RBAC permission: "weather.read"
    ///   - OAuth scope: "Forecast.Read" (handled by Microsoft.Identity.Web)
    /// </summary>
    public async Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        // OAuth scope acquisition happens automatically via IDownstreamApi
        return await _downstreamApi.CallApiForUserAsync<WeatherForecast[]>(
            "WeatherApi", 
            options => options.RelativePath = "/weather");
    }
}
```

---

## References

### Official Documentation

1. **BFF Pattern:** https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends
2. **CORS in ASP.NET Core:** https://learn.microsoft.com/en-us/aspnet/core/security/cors
3. **OAuth Scopes:** https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc
4. **OBO Flow:** https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow
5. **MSAL.NET OBO:** https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow
6. **Microsoft.Identity.Web:** https://github.com/azuread/microsoft-identity-web

### W3C/IETF Standards

7. **CORS Specification:** https://fetch.spec.whatwg.org/#http-cors-protocol
8. **OAuth 2.0 Framework:** https://datatracker.ietf.org/doc/html/rfc6749
9. **JWT Access Tokens:** https://www.rfc-editor.org/rfc/rfc9068

### MDN Web Docs

10. **CORS:** https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS

### Architecture Patterns

11. **Microservices Naming:** https://stackoverflow.com/questions/62951664/microservices-naming-convention
12. **API Gateway vs BFF:** https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/direct-client-to-microservice-communication-versus-the-api-gateway-pattern

---

## Testing Strategy

### Test 1: Verify CORS Removal

```bash
# From browser console (should fail with CORS error)
fetch('https://localhost:7220/weather')
  .then(r => r.json())
  .then(console.log)
  .catch(console.error);

# Expected: CORS error (proves browser can't access directly)
```

### Test 2: Verify BFF-to-API Call Works

```csharp
// ServerWeatherService should still work (server-to-server)
var weather = await _downstreamApi.CallApiForUserAsync<WeatherForecast[]>(
    "WeatherApi", 
    options => options.RelativePath = "/weather");
// Expected: ✅ Success (CORS not needed for HttpClient)
```

### Test 3: Verify Scope Validation

```bash
# Call with invalid OAuth scope (should fail at ProtectedApi)
# Expected: 403 Forbidden from scope validation
```

### Test 4: Verify RBAC Permission

```csharp
// User without "weather.read" permission (should fail at BFF)
// Expected: 403 Forbidden from PermissionAuthorizationHandler
```

---

## Implementation Checklist

- [ ] Rename downstream API configurations (DownstreamApi → MicrosoftGraph, ProtectedApi → WeatherApi)
- [ ] Update Program.cs downstream API registrations
- [ ] Remove CORS configuration from ProtectedApi/WeatherApi
- [ ] Update GraphService to use "MicrosoftGraph" name
- [ ] Update ServerWeatherService to use "WeatherApi" name
- [ ] Test browser-direct call to WeatherApi (should fail with network error, not CORS)
- [ ] Test BFF → WeatherApi flow (should succeed)
- [ ] Verify OAuth scope validation works
- [ ] Verify RBAC permission checks work
- [ ] Update README.md with architecture clarifications

---

## Conclusion

The research findings validate that demo5's current OAuth scope vs RBAC permission separation is **architecturally correct**. The two key improvements needed are:

1. **Naming clarity:** Rename APIs to clearly distinguish external (MicrosoftGraph) from internal (WeatherApi)
2. **CORS removal:** Eliminate unnecessary CORS configuration from internal API to enforce BFF pattern

These changes will improve security posture, architectural clarity, and align with .NET 10 / Microsoft Identity Web best practices while maintaining the intentional separation between OAuth authorization (token acquisition) and application-level RBAC (business logic authorization).
