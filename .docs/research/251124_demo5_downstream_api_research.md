# Research: .NET 10 Downstream API Patterns for Demo5

## Context
**Requested by:** Research-Agent  
**Target:** demo5  
**Goal:** Implement downstream API patterns using Bearer tokens, IDownstreamApi, and Entra ID configuration for microservice architecture extending demo4 (Microsoft Entra ID integration)

## Key Findings

### 1. AddMicrosoftIdentityWebApi Configuration (.NET 10)

**Purpose**: Configure standalone ASP.NET Core Minimal API to validate Bearer tokens from Microsoft Entra ID

**NuGet Packages Required**:
- `Microsoft.Identity.Web` v4.1.0 (latest stable, .NET 10 compatible)
- `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.0
- `Azure.Identity` (for managed identity scenarios)

**Configuration in appsettings.json**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "[your-domain].onmicrosoft.com",
    "TenantId": "[tenant-id]",
    "ClientId": "[api-application-client-id]",
    "Audience": "api://[api-application-client-id]"
  }
}
```

**Program.cs Setup Pattern**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add authentication with JWT Bearer for APIs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Optional: Enable token acquisition for calling downstream APIs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/weather", [Authorize] (HttpContext context) => 
{
    // Access user claims from validated token
    var userId = context.User.FindFirst("oid")?.Value;
    var scopes = context.User.FindFirst("scp")?.Value;
    
    return Results.Ok(new { temperature = 72, userId, scopes });
});

app.Run();
```

**Key Configuration Parameters**:
- `Audience`: Must match the API's Application ID URI (api://[client-id])
- `TenantId`: For single-tenant apps (multi-tenant uses common)
- `ClientId`: The API application's client ID

### 2. IDownstreamApi Usage (.NET 10)

**Purpose**: Call custom downstream APIs using On-Behalf-Of (OBO) flow with automatic token exchange

**Package**: `Microsoft.Identity.Web.DownstreamApi` v4.1.0

**Configuration for Custom API Scopes**:
```json
{
  "DownstreamApi": {
    "BaseUrl": "https://localhost:7220",
    "Scopes": ["api://[api-client-id]/Forecast.Read", "api://[api-client-id]/Weather.Write"]
  }
}
```

**Service Registration**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();
```

**Usage in Controllers/Services**:
```csharp
public class WeatherController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public WeatherController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast()
    {
        try
        {
            // Call downstream API with OBO token
            var forecast = await _downstreamApi.CallApiForUserAsync<WeatherForecast>(
                "DownstreamApi",
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = "api/forecast";
                });
                
            return Ok(forecast);
        }
        catch (MsalUiRequiredException ex)
        {
            // User needs to re-authenticate
            return Unauthorized(new { error = "Token expired", details = ex.Message });
        }
        catch (DownstreamApiException ex)
        {
            return StatusCode(502, new { error = "Downstream API error", details = ex.Message });
        }
    }
    
    [HttpPost("forecast")]
    public async Task<IActionResult> CreateForecast([FromBody] WeatherData data)
    {
        // POST with request body
        var result = await _downstreamApi.CallApiForUserAsync<WeatherData, ForecastResult>(
            "DownstreamApi",
            data,
            options =>
            {
                options.HttpMethod = HttpMethod.Post;
                options.RelativePath = "api/forecast";
            });
            
        return Created($"/forecast/{result.Id}", result);
    }
}
```

**OBO Flow Implementation Details**:
1. Client sends Bearer token to API
2. API validates token using `AddMicrosoftIdentityWebApi`
3. API calls `IDownstreamApi.CallApiForUserAsync()`
4. Microsoft.Identity.Web automatically exchanges user's token for downstream API token
5. Downstream API call made with exchanged token

**Error Handling Patterns**:
```csharp
try
{
    var result = await _downstreamApi.GetForUserAsync<WeatherData>("DownstreamApi");
    return Ok(result);
}
catch (MsalServiceException ex) when (ex.ErrorCode == "invalid_grant")
{
    return Unauthorized("Token invalid or expired");
}
catch (DownstreamApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
{
    return Forbid("Insufficient permissions for downstream API");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Downstream API call failed");
    return StatusCode(500, "Internal server error");
}
```

### 3. Entra ID App Configuration

**API Application Setup**:
1. Create new app registration in Azure Portal
2. Set Application ID URI: `api://[client-id]`
3. Expose API:
   - Add scope: `Forecast.Read` (Admin consent required: No)
   - Add scope: `Weather.Write` (Admin consent required: Yes)

**Client Application Configuration**:
1. In existing app registration (from demo4)
2. Add API permissions:
   - Select your API application
   - Add delegated permissions: `Forecast.Read`, `Weather.Write`
3. Grant admin consent for permissions

**Multi-tenant vs Single-tenant**:
- **Single-tenant**: Specify `TenantId` in config, users only from your tenant
- **Multi-tenant**: Use `"https://login.microsoftonline.com/common"`, validate issuer in code

**App ID URI Pattern**:
```
api://[client-id]  // Standard pattern
https://[domain]/[api-name]  // Alternative for multi-tenant
```

### 4. Bearer Token Validation

**Token Validation Parameters**:
```csharp
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudiences = new[] { "api://[api-client-id]" },
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});
```

**Accessing User Claims**:
```csharp
[HttpGet("profile")]
public IActionResult GetProfile()
{
    var claims = new
    {
        UserId = User.FindFirst("oid")?.Value,           // Object ID
        TenantId = User.FindFirst("tid")?.Value,         // Tenant ID
        Upn = User.FindFirst("preferred_username")?.Value, // User Principal Name
        Name = User.FindFirst("name")?.Value,            // Display Name
        Scopes = User.FindFirst("scp")?.Value?.Split(' ') // Scopes array
    };
    
    return Ok(claims);
}
```

**Delegated vs App-Only Permissions**:
- **Delegated**: User consents, token contains `scp` claim with allowed scopes
- **App-only**: Application permissions, token contains `roles` claim
- **Scope-based Authorization**:
```csharp
[Authorize]
[RequiredScope("Forecast.Read")]
public class WeatherController : ControllerBase
{
    // Only accessible if token contains Forecast.Read scope
}
```

### 5. Architecture Patterns

**BFF (Backend-for-Frontend) Pattern**:
- **Security**: Cookie-based authentication, implicit trust
- **When to Use**: Client-side components need to call external APIs
- **Pros**: No token exposure to client, centralized auth
- **Cons**: Server-side coupling, scalability concerns
- **Example**: `/api/weather` endpoint in demo3-4

**Downstream (Token-based) Pattern**:
- **Security**: Bearer token authentication, explicit trust
- **When to Use**: Microservice architectures, API-to-API calls
- **Pros**: Loose coupling, better scalability, explicit permissions
- **Cons**: Token management complexity, client must handle tokens
- **Example**: Standalone `WeatherApi` service in demo5

**Architecture Comparison Table**:

| Aspect | BFF (Cookie) | Downstream (Token) |
|--------|-------------|-------------------|
| **Trust Model** | Implicit (same domain) | Explicit (token validation) |
| **Client Complexity** | Low (automatic cookies) | High (token acquisition/storage) |
| **Server Complexity** | High (proxy logic) | Low (stateless validation) |
| **Scalability** | Limited (coupled to frontend) | High (independent services) |
| **Security** | Server-side token handling | Client token exposure |
| **Use Case** | Monolithic apps | Microservices |
| **Token Exposure** | None | Client-side |
| **CORS** | Not required | Required |

**Performance Considerations**:
- **BFF**: Additional network hop, server-side processing
- **Downstream**: Direct API calls, but token validation overhead
- **Caching**: Use distributed cache for tokens in production

### 6. Port and Project Structure

**Recommended Port Setup**:
```json
// Demo5.BffApp launchSettings.json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7210;http://localhost:5210",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

// Demo5.ProtectedApi launchSettings.json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7220;http://localhost:5220",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**CORS Configuration**:
```csharp
// In Protected API Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBff", policy =>
    {
        policy.WithOrigins("https://localhost:7210")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors("AllowBff");
```

**Development Certificate Setup**:
```bash
# Generate certificates for multiple ports
dotnet dev-certs https --trust
# Or specify ports
dotnet dev-certs https --trust -ep $env:USERPROFILE\.aspnet\https\demo5-bff.pfx -p [password]
dotnet dev-certs https --trust -ep $env:USERPROFILE\.aspnet\https\demo5-api.pfx -p [password]
```

**Project Structure**:
```
demo5/
├── Demo5.BffApp/           # Blazor Web App (extends demo4)
│   ├── Program.cs          # BFF API endpoints + Entra auth
│   └── appsettings.json    # AzureAd config
├── Demo5.ProtectedApi/     # Standalone API service
│   ├── Program.cs          # Bearer token validation
│   └── appsettings.json    # AzureAd config
└── Demo5.sln              # Solution with both projects
```

## Recommendations for Implementation

**Architecture Decision**:
Implement both patterns side-by-side to demonstrate trade-offs:
- Keep BFF pattern for existing `/api/weather` (backward compatibility)
- Add downstream pattern with new `Demo5.ProtectedApi` project
- Show OBO flow: Client → BFF → Protected API

**Code Changes Required**:
1. **New Project**: `Demo5.ProtectedApi` with Minimal API
2. **BFF App**: Add `IDownstreamApi` calls to protected API
3. **Entra Config**: Separate app registration for API
4. **CORS**: Configure cross-origin for API calls
5. **Testing**: Validate both cookie and token authentication

**Testing Strategy**:
- Unit tests for API controllers with mocked tokens
- Integration tests for OBO flow
- E2E tests for complete user journey
- Load testing for performance comparison

## Security Best Practices

### ✅ Required for Production
1. **HTTPS Only**: All API endpoints must use HTTPS
2. **Token Validation**: Always validate audience, issuer, and scopes
3. **CORS Policy**: Restrict origins to trusted domains
4. **Error Handling**: Don't leak sensitive information in errors
5. **Logging**: Log authentication failures without exposing tokens

### ⚠️ Common Pitfalls
1. **Missing Scope Validation**: Always check required scopes in controllers
2. **Token Replay**: Use short token lifetimes and proper validation
3. **CORS Misconfiguration**: Don't use `AllowAnyOrigin` in production
4. **Mixed Auth Schemes**: Keep BFF and downstream APIs separate
5. **Client Secret Exposure**: Never store secrets in client-side code

## References
- [Microsoft.Identity.Web Documentation](https://learn.microsoft.com/en-us/entra/identity-platform/msal-overview)
- [ASP.NET Core Web API with Azure AD](https://learn.microsoft.com/en-us/entra/identity-platform/web-api)
- [On-Behalf-Of Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)

---

**Research Date:** November 24, 2025  
**Framework:** .NET 10.0  
**Microsoft.Identity.Web Version:** 4.1.0