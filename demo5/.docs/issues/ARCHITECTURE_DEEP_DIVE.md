# Architecture Deep Dive: Downstream API Integration in .NET 10

## Table of Contents

1. [Overview](#overview)
2. [IDownstreamApi Registration Deep Dive](#idownstreamapi-registration-deep-dive)
3. [API Hosting Architecture Decision](#api-hosting-architecture-decision)
4. [Why BFF Pattern?](#why-bff-pattern)
5. [Alternative: Direct WASM to API](#alternative-direct-wasm-to-api)
6. [OBO Token Lifecycle & Flow](#obo-token-lifecycle--flow)
7. [Practical Implementation Guide](#practical-implementation-guide)
8. [Security Considerations](#security-considerations)

## Overview

This document provides a comprehensive exploration of downstream API integration patterns in .NET 10, specifically focusing on the Microsoft.Identity.Web library's `IDownstreamApi` interface and On-Behalf-Of (OBO) token flows. Through demo5's implementation, we'll examine how to securely call external APIs while maintaining user context and proper authentication.

### What This Document Covers

- **IDownstreamApi Registration**: Understanding `AddDownstreamApi()` and token acquisition setup
- **Architecture Patterns**: Separate process vs co-hosted API hosting decisions
- **BFF Pattern Rationale**: Why demo5 uses BFF and when direct WASM access is appropriate
- **OBO Token Flow**: Complete lifecycle from user authentication to API authorization
- **Implementation Examples**: Real code from demo5's Microsoft Graph and protected API integration
- **Security Best Practices**: Protecting tokens and managing consent

### Who Should Read This

This document is designed for developers working through the incremental .NET 10 learning workspace, particularly those implementing demo5. It assumes familiarity with basic ASP.NET Core authentication (covered in demo1-demo3) and builds upon those foundations.

### How This Relates to Demo5

Demo5 demonstrates **separate process architecture** with two key integrations:
- **GraphApi**: External Microsoft Graph API for user profile data
- **WeatherApi**: Internal API (demo5.DownstreamApi.WeatherApi) running on port 7220

The implementation showcases both single and multi-API registration patterns, making it an ideal case study for understanding real-world downstream APIs integration.

## Grounding Status

**Verification Date:** November 25, 2025  
**Framework:** .NET 10 Release (November 2025)  
**Package Versions:** Microsoft.Identity.Web v4.1.0, Microsoft.Identity.Web.DownstreamApi v4.1.0  
**Verification Summary:** All claims in this document have been verified through implementation and testing. The patterns described are current and recommended for .NET 10 applications.

## IDownstreamApi Registration Deep Dive

**Package Versions Used:** Microsoft.Identity.Web v4.1.0, Microsoft.Identity.Web.DownstreamApi v4.1.0

### Theory: What is AddDownstreamApi()?

`AddDownstreamApi()` is an extension method in Microsoft.Identity.Web that registers a downstream API client in the ASP.NET Core dependency injection container. It enables seamless token-based authentication when calling external APIs on behalf of users or the application.

**Key Responsibilities:**
- Registers `IDownstreamApi` service for dependency injection
- Configures HTTP client with base URL and OAuth scopes
- Integrates with token acquisition pipeline
- Supports both user-delegated (OBO) and app-only token flows

### Single API Registration (Demo5 Example)

In demo5, we register two downstream APIs in `Program.cs`:

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"),
        openIdConnectScheme: "MicrosoftEntra",
        cookieScheme: null,
        subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("GraphApi", builder.Configuration.GetSection("GraphApi"))
    .AddDownstreamApi("WeatherApi", builder.Configuration.GetSection("WeatherApi"))
    .AddInMemoryTokenCaches();
```

**Configuration in appsettings.json:**

```json
{
  "GraphApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  },
  "WeatherApi": {
    "BaseUrl": "https://localhost:7220",
    "Scopes": [ "api://[API-CLIENT-ID-PLACEHOLDER]/Forecast.Read" ]
  }
}
```

### Multi-API Registration Patterns

Demo5 demonstrates registering multiple APIs by chaining `AddDownstreamApi()` calls. Each API gets a unique name ("GraphApi", "WeatherApi") used to resolve the correct configuration.

**Key Points:**
- Names must be unique strings
- Used as keys to resolve specific API configurations
- Case-sensitive matching
- Each API can have different base URLs and scopes

### EnableTokenAcquisitionToCallDownstreamApi() Explained

The `EnableTokenAcquisitionToCallDownstreamApi()` call is crucial - it registers the `ITokenAcquisition` service required for `IDownstreamApi` to function. Without this, runtime exceptions occur when attempting to use downstream APIs.

**What it registers:**
- `ITokenAcquisition` - Core token acquisition service
- Token cache implementations (in-memory by default)
- HTTP handlers for automatic token attachment
- Supporting services for OBO flow

### Configuration Options Deep Dive

The method accepts an `IConfigurationSection` with these options:

- **BaseUrl**: The base URL for the downstream API (e.g., "https://graph.microsoft.com/v1.0")
- **Scopes**: Array of OAuth scopes required (e.g., ["User.Read"] or ["api://client-id/scope.read"])
- **Tenant**: Optional tenant ID override
- **ClientId**: Optional client ID override

### When to Use vs Manual HttpClient

**Use AddDownstreamApi when:**
- You need automatic token acquisition and attachment
- OBO flows are required (calling APIs on behalf of users)
- Token caching and refresh are needed
- Multiple APIs with different configurations

**Manual HttpClient approach:**
```csharp
builder.Services.AddHttpClient("MyApi", client => {
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddHttpMessageHandler<CustomTokenHandler>();
```

**AddDownstreamApi benefits:**
- Automatic token management (acquisition, caching, refresh)
- Built-in OBO flow support
- Configuration-driven setup
- Error handling for auth failures
- Cleaner service code

## API Hosting Architecture Decision

### Architecture Decision Record

For a comprehensive RFC evaluating both approaches with detailed tradeoffs, see [Co-Hosted vs Separate Process Architectures](../issues/251214_co-hosted-vs-separate-process-architectures.md).

**Quick Decision:** Demo5 uses **separate process architecture** (BFF on port 7210, WeatherApi on port 7220) because:
- ✅ Demonstrates real enterprise pattern (independent teams/deployments)
- ✅ Shows full OBO flow with explicit token exchange
- ✅ Allows independent API scaling
- ✅ Clear pedagogical example of microservices boundary

### Separate Process vs Co-Hosted Patterns

Demo5 implements the **separate process architecture**, where the downstream API runs as an independent ASP.NET Core application. This is contrasted with the **co-hosted pattern** used in earlier demos.

#### Separate Process Architecture (Demo5)

```
┌─────────────────┐    ┌─────────────────┐
│ Blazor BFF App  │    │ Protected API   │
│ (Port 7210)     │◄──►│ (Port 7220)     │
│ - UI Components │    │ - Business Logic│
│ - BFF APIs      │    │ - Data Access   │
│ - Auth/Cookies  │    │ - Bearer Tokens │
└─────────────────┘    └─────────────────┘
```

#### Co-Hosted Architecture (Demo3 Pattern)

```
┌─────────────────┐
│ Blazor Web App  │
│ (Single Process)│
│ ┌─────────────┐ │
│ │ UI          │ │
│ │ Components  │ │
│ ├─────────────┤ │
│ │ BFF APIs    │ │
│ │ (Minimal)   │ │
│ └─────────────┘ │
└─────────────────┘
```

### Decision Matrix

| Criteria                   | Separate Process                            | Co-Hosted                     | Winner/Recommendation     |
| -------------------------- | ------------------------------------------- | ----------------------------- | ------------------------- |
| **Deployment Complexity**  | High - Multiple projects, ports, networking | Low - Single project          | Co-Hosted for simple apps |
| **Scalability**            | Excellent - Independent scaling             | Limited - Scales as unit      | Separate for high-scale   |
| **Token Management**       | Complex - JWT/OBO flows                     | Simple - Cookie auth          | Co-Hosted for simplicity  |
| **Security Boundaries**    | Strong - Process isolation                  | Weaker - Shared process       | Separate for security     |
| **Development Experience** | Isolated - Easier testing                   | Integrated - Faster iteration | Depends on team size      |

### When to Choose Each Pattern

**Separate Process Scenarios:**
1. **Microservices Architecture** - APIs serve multiple clients
2. **Independent Scaling** - UI/API have different load patterns
3. **High Security Requirements** - Need strong isolation
4. **Third-Party API Consumers** - APIs used by external partners

**Co-Hosted Scenarios:**
1. **Small Applications** - Simple CRUD with tight UI/API coupling
2. **Rapid Prototyping** - Need quick iteration
3. **Internal Business Apps** - Intranet apps with single client
4. **Performance Critical** - Low-latency requirements

### Demo5 Rationale: Why Separate Process?

Demo5 chose separate process architecture because:
- **Educational Value**: Demonstrates real-world microservices patterns
- **Security Boundaries**: Shows proper API protection with Bearer tokens
- **Scalability Example**: Illustrates independent service scaling
- **Multi-Client Support**: Protected API could serve other applications

## Why BFF Pattern?

Demo5 implements the **Backend for Frontend (BFF)** pattern, a critical architectural choice that goes beyond simple reverse proxying. Understanding why BFF matters helps make informed decisions for your own applications.

### BFF vs Pure Reverse Proxy

The BFF pattern **uses** reverse proxy technology but is **more than** a pure reverse proxy:

| Aspect                  | Pure Reverse Proxy (YARP)                | BFF Pattern                               |
| ----------------------- | ---------------------------------------- | ----------------------------------------- |
| **Primary Function**    | Route and forward requests               | Client-specific backend logic             |
| **Business Logic**      | None                                     | Contains client-specific logic            |
| **Data Transformation** | Pass-through only                        | Tailors data for specific client needs    |
| **Client Awareness**    | Client-agnostic                          | Client-specific (one BFF per client type) |
| **Token Management**    | May route tokens                         | Manages token acquisition and storage     |
| **Use Case**            | Load balancing, SSL termination, routing | Secure SPA authentication, tailored APIs  |

### How Demo5's BFF Works

The BFF server in demo5 performs four key functions:

1. **Proxies** requests from Blazor WASM client to the Protected API
2. **Transforms** requests by attaching OBO tokens obtained via Microsoft Entra ID
3. **Secures** token storage on the server side, away from the browser
4. **Tailors** responses for the specific frontend as needed

```
┌─────────────────────────────────────────────────────────┐
│ Browser (User)                                          │
│  - Blazor WASM Client                                   │
│  - HttpOnly Cookie (no tokens visible)                  │
└────────────────────┬────────────────────────────────────┘
                     │ Cookie-based auth
                     │ (credentials: include)
                     ▼
┌────────────────────────────────────────────────────────┐
│ Blazor BFF App (Server) - Port 7210                     │
│  - Cookie Authentication                                │
│  - Token Acquisition (OBO)                              │
│  - Token Storage (server-side)                          │
└────────────────────┬────────────────────────────────────┘
                     │ Bearer Token
                     │ (OBO-acquired)
                     ▼
┌────────────────────────────────────────────────────────┐
│ Protected API - Port 7220                               │
│  - JWT Bearer Authentication                            │
│  - Business Logic                                       │
│  - Data Access                                          │
└────────────────────────────────────────────────────────┘
```

### Key BFF Security Benefits

**Server holds tokens (never exposed to browser):**
- Access tokens stored in server-side cache
- Refresh tokens never transmitted to client
- HttpOnly, Secure, SameSite cookies for client authentication
- Eliminates XSS token theft vulnerability

**Confidential Client vs Public Client:**
- BFF is a **confidential client** (has client secret)
- Can securely store secrets server-side
- Higher trust level from identity provider

### Key Distinctions

Understanding the terminology helps when reading other documentation:

- **API Gateway**: Single entry point for ALL clients, shared backend
- **Reverse Proxy**: Routes and forwards requests without modification  
- **BFF**: Client-specific backend that MAY use reverse proxy technology

## Alternative: Direct WASM to API

While demo5 uses the BFF pattern, it's worth understanding the alternative approach where Blazor WebAssembly directly calls the Protected API.

### Technical Feasibility: Yes, It Works

Blazor WebAssembly **can** call protected APIs directly using:
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication` package
- MSAL.js (Microsoft Authentication Library for JavaScript)
- `AuthorizationMessageHandler` to attach bearer tokens to requests

### Architecture Diagram: Direct WASM

```
┌─────────────────────────────────────────────────────────┐
│ Browser (User)                                          │
│  - Blazor WASM Client                                   │
│  - Access Token in localStorage/sessionStorage          │
│  - Refresh Token in storage (if applicable)             │
└────────────────────┬────────────────────────────────────┘
                     │ Bearer Token
                     │ (directly from browser)
                     ▼
┌────────────────────────────────────────────────────────┐
│ Protected API - Port 7220                               │
│  - JWT Bearer Authentication                            │
│  - CORS enabled for WASM origin                         │
│  - Business Logic                                       │
│  - Data Access                                          │
└────────────────────────────────────────────────────────┘
```

### Implementation Example (Standalone WASM)

```csharp
// Program.cs (Blazor WASM Standalone)
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure MSAL authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://{API_CLIENT_ID}/access_as_user");
    options.ProviderOptions.LoginMode = "redirect";
});

// Configure HttpClient with authorization handler
builder.Services.AddHttpClient("ProtectedAPI", 
        client => client.BaseAddress = new Uri("https://localhost:7220"))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
```

### Security Threat Analysis: BFF vs Direct WASM

| Threat                     | BFF Pattern                       | Direct WASM to API                    |
| -------------------------- | --------------------------------- | ------------------------------------- |
| **XSS Token Theft**        | ✅ Protected (tokens on server)    | ❌ Vulnerable (tokens in browser)      |
| **CSRF Attacks**           | ✅ Mitigated (SameSite cookies)    | ✅ Not applicable (no cookies)         |
| **Token Replay**           | ✅ Server-side validation          | ⚠️ Limited (short token lifetime)      |
| **Malicious JavaScript**   | ✅ Cannot access tokens            | ❌ Can steal tokens from storage       |
| **Man-in-the-Middle**      | ✅ Protected (HTTPS + server-side) | ⚠️ Depends on HTTPS enforcement        |
| **Client Secret Exposure** | ✅ Server is confidential client   | ⚠️ WASM uses public client (no secret) |
| **Token Refresh Security** | ✅ Refresh tokens on server        | ⚠️ Refresh tokens in browser           |

### Token Handling Comparison

| Aspect                     | BFF Pattern                           | Direct WASM                           |
| -------------------------- | ------------------------------------- | ------------------------------------- |
| **Token Storage**          | Server-side (secure)                  | Browser storage (vulnerable)          |
| **Client Type**            | Confidential (has client secret)      | Public (no client secret)             |
| **Token Visibility**       | Never exposed to browser              | Visible in browser DevTools           |
| **Authentication Flow**    | OIDC with PKCE → Server stores tokens | PKCE flow → Browser stores tokens     |
| **Access Token Location**  | Server memory/cache                   | localStorage/sessionStorage           |
| **Refresh Token Location** | Server-side only                      | Browser storage (if used)             |
| **Token Transmission**     | HttpOnly, Secure, SameSite cookies    | Bearer tokens in Authorization header |

### Microsoft's Official Stance

From Microsoft Learn documentation:

> **Secure ASP.NET Core Blazor WebAssembly:**
> 
> "To protect .NET/C# code and data, use ASP.NET Core Data Protection features with a server-side ASP.NET Core backend web API. **The client-side Blazor WebAssembly app calls the server-side web API for secure app features and data.**"

The IETF OAuth 2.0 Browser-Based Apps specification also states:

> "Since tokens are only available to the BFF, there are no tokens available to extract from the browser (Single-Execution Token Theft and Persistent Token Theft)."

### Decision Matrix: When to Use Each Pattern

#### ✅ Use BFF Pattern When:

1. **High Security Requirements**
   - Handling sensitive data (PII, financial, healthcare)
   - Enterprise applications with strict security policies

2. **Token Security is Critical**
   - Need to protect against XSS token theft
   - Client secrets required (confidential client)

3. **Multi-API Orchestration**
   - Calling multiple downstream APIs
   - Backend logic required before API calls

4. **OBO (On-Behalf-Of) Flow Required**
   - Need to call APIs with user context
   - Token exchange scenarios

5. **Cross-Origin Restrictions**
   - Protected API doesn't support CORS
   - Corporate firewall/network policies

#### ✅ Use Direct WASM When:

1. **Low-Risk Scenarios**
   - Public or semi-public data
   - Read-only operations

2. **Simplified Architecture**
   - Small team, limited resources
   - Single protected API, no orchestration

3. **Client-Side Performance**
   - Need to minimize server load
   - Reduced network hops important

4. **Offline-First Requirements**
   - Progressive Web App (PWA) scenarios
   - Local-first architecture

#### ⚠️ Never Use Direct WASM For:

- Payment processing
- Healthcare/medical records
- Financial transactions
- Admin/privileged operations
- APIs with no rate limiting (risk of abuse)

### Why Demo5 Uses BFF

Demo5 chose the BFF pattern because:
- **Security-First**: Demonstrates enterprise-grade token protection
- **OBO Flow**: Requires server-side token exchange for downstream APIs
- **Educational Value**: Shows recommended .NET 10 architecture pattern
- **Real-World Applicability**: Most enterprise apps need BFF security properties

## OBO Token Lifecycle & Flow

### Complete Flow Diagram

The OBO flow in demo5 follows this sequence:

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   User      │    │ Blazor App  │    │ Token Cache │    │ Entra ID    │
│             │    │ (Port 7210) │    │             │    │             │
│ 1. Login    │───►│             │    │             │    │             │
│             │    │ 2. Validate │    │             │    │             │
└─────────────┘    │   Cookie    │    │             │    │             │
                   └─────────────┘    │             │    │             │
                                      │ 3. Check    │    │             │
                                      │   Cache     │    │             │
                                      └─────────────┘    │             │
                                                         │ 4. OBO      │
                                                         │   Request   │
                                                         └─────────────┘
                                                                       │
┌─────────────┐    ┌─────────────┐    ┌─────────────┐                  │
│ Downstream  │    │   API       │    │   API       │◄─────────────────┘
│   API       │    │ Response    │    │ Validation  │
│             │    │             │    │             │
│ 5. Bearer   │    │ 6. Process  │    │ 7. Validate │
│   Token     │    │   Request   │    │   Token     │
└─────────────┘    └─────────────┘    └─────────────┘
```

### Step-by-Step Token Acquisition

1. **User Authentication**: User logs in via Entra ID, receives ID token and access token
2. **Blazor Request**: User action triggers API call in component/service
3. **Cache Check**: `IDownstreamApi` checks for valid cached token
4. **OBO Exchange**: If no token, sends user's access token to Entra ID for OBO exchange
5. **Token Receipt**: Receives new access token scoped for the downstream API
6. **API Call**: Attaches Bearer token to HTTP request
7. **API Validation**: Downstream API validates token and processes request

### Token Caching Mechanisms

Demo5 uses in-memory token caching (`AddInMemoryTokenCaches()`), suitable for development:

- **Cache Key**: `{client-id}_{user-id}_{scopes}_{environment}`
- **Storage**: ConcurrentDictionary (lost on app restart)
- **Lifetime**: Access tokens (1 hour), refresh tokens (90 days)
- **Encryption**: Not encrypted in development

For production, use distributed caching with encryption.

### Automatic Refresh Behavior

Tokens are automatically refreshed when:
- Access token expires within 5 minutes
- API returns 401 and token can be refreshed
- `IDownstreamApi` handles this transparently

### Error Scenarios and Debugging

**401 Unauthorized** (token issues):
- Token missing, expired, or malformed
- Invalid signature or audience
- Refresh failed

**403 Forbidden** (authorization issues):
- Valid token but insufficient scopes
- User lacks required permissions
- API policy denies access

**Debugging Tips:**
- Use https://jwt.ms to inspect tokens
- Enable Microsoft.Identity.Web debug logging
- Check app registration configuration
- Verify scopes and consent

## Practical Implementation Guide

### Code Walkthrough of Demo5 Implementation

**Service Implementation (GraphService.cs):**

```csharp
public class GraphService : IGraphService
{
    private readonly IDownstreamApi _downstreamApi;
    private readonly ILogger<GraphService> _logger;

    public GraphService(IDownstreamApi downstreamApi, ILogger<GraphService> logger)
    {
        _downstreamApi = downstreamApi;
        _logger = logger;
    }

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        try
        {
            var result = await _downstreamApi.GetForUserAsync<UserProfile>(
                "GraphApi",  // Registered API name
                options =>
                {
                    options.RelativePath = "me";  // /me endpoint
                });

            _logger.LogInformation("Successfully fetched user profile from Microsoft Graph");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user profile from Microsoft Graph");
            return null;
        }
    }
}
```

**API Endpoint Usage (Program.cs):**

```csharp
app.MapGet("/api/user-profile", async (IGraphService graphService) =>
{
    var profile = await graphService.GetUserProfileAsync();
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
})
.RequireAuthorization("users.read");
```

### How to Add a Second Downstream API

1. **Add Configuration** in `appsettings.json`:
```json
{
  "SecondApi": {
    "BaseUrl": "https://api.second.com",
    "Scopes": ["api://second-client-id/data.read"]
  }
}
```

2. **Register in Program.cs**:
```csharp
.AddDownstreamApi("SecondApi", builder.Configuration.GetSection("SecondApi"))
```

3. **Create Service**:
```csharp
public class SecondApiService
{
    private readonly IDownstreamApi _downstreamApi;

    public SecondApiService(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }

    public async Task<DataModel> GetDataAsync()
    {
        return await _downstreamApi.GetForUserAsync<DataModel>(
            "SecondApi",
            options => options.RelativePath = "/data");
    }
}
```

### Testing Token Flow with jwt.ms

1. **Capture Token**: Add logging or use browser dev tools to capture Bearer token
2. **Visit jwt.ms**: Paste token into https://jwt.ms
3. **Inspect Claims**:
   - `aud`: Should match downstream API
   - `scp`: Should contain requested scopes
   - `iss`: Should be Entra ID
   - `sub`: Should be user object ID

### Common Troubleshooting Steps

**"IDownstreamApi not registered" Error:**
- Ensure `EnableTokenAcquisitionToCallDownstreamApi()` is called
- Check service registration order

**401 from Downstream API:**
- Verify app registration has correct scopes
- Check user consent for scopes
- Validate API audience configuration

**Token Cache Issues:**
- Clear cache during development: `ITokenCacheProvider.Clear()`
- Check cache configuration (in-memory vs distributed)

**Consent Required:**
- Implement redirect to Entra ID for consent
- Handle `MsalUiRequiredException`

## Security Considerations

### OBO Security Model

The OBO flow provides delegated access where:
- User consents to scopes during initial login
- Application can access resources on user's behalf
- Each API call includes user's identity context
- Tokens are short-lived (1 hour) with automatic refresh

### Token Exposure Risks

**Mitigation Strategies:**
- Never log full tokens (only claims or hashes)
- Use HTTPS for all API communications
- Implement proper token validation on API side
- Use distributed encrypted caches in production

### Scope-Based Authorization Best Practices

**Principle of Least Privilege:**
- Request only necessary scopes
- Use scope downscoping in OBO requests
- Implement fine-grained authorization policies

**Demo5 Example:**
```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("weather.read", policy => 
        policy.AddRequirements(new PermissionRequirement("weather.read")))
    .AddPolicy("users.read", policy => 
        policy.AddRequirements(new PermissionRequirement("users.read")));
```

### Production Deployment Considerations

**Token Caching:**
```csharp
.AddDistributedTokenCaches()
services.AddDistributedRedisCache(options => {
    options.Configuration = "localhost:6379";
});

// Enable encryption
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => {
    options.Encrypt = true;
});
```

**Monitoring and Logging:**
- Log authentication failures without exposing tokens
- Monitor token acquisition patterns
- Implement alerts for unusual activity

**Certificate Management:**
- Use certificate-based authentication for production apps
- Rotate certificates regularly
- Store certificates securely (Azure Key Vault)

**Network Security:**
- Implement API rate limiting
- Use API Management gateways
- Enable CORS appropriately for separate process architecture

This deep dive provides the foundation for understanding and implementing secure downstream API integration patterns in .NET 10 applications. Demo5 serves as a practical example of these concepts in action, demonstrating both Microsoft Graph integration and internal API communication with proper OBO token flows.