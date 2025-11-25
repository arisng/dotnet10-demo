# Research: YARP Integration into BFF Architecture - November 25, 2025

## Context

**Requested by:** User (Research-Agent mode)  
**Target:** Future demos (out of scope for demo5)  
**Goal:** Understand how YARP (Yet Another Reverse Proxy) could integrate with the existing BFF (Backend for Frontend) pattern implemented in demo5

**Current Architecture (demo5):**
- **Blazor BFF App** (Port 7210): Cookie authentication, OBO token acquisition, calls downstream APIs
- **Protected API** (Port 7220): JWT Bearer authentication, business logic

## Research Questions Addressed

1. ✅ Can we migrate Authentication and Authorization from BFF to YARP?
2. ✅ Should we migrate auth from BFF to YARP?
3. ✅ How would demo5's architecture change with YARP?

---

## Key Findings

### 1. YARP Authentication/Authorization Capabilities ✅

**Source:** [Microsoft Learn - YARP Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/authn-authz?view=aspnetcore-10.0)

**Package:** `Yarp.ReverseProxy` v2.3.0 (latest stable for .NET 10)

#### What YARP Natively Supports

YARP **leverages ASP.NET Core's authentication and authorization middleware** rather than implementing its own auth system. This is a critical architectural distinction.

**✅ YARP CAN:**
- Apply ASP.NET Core authorization policies to routes (`AuthorizationPolicy` per route)
- Enforce authentication requirements before proxying requests
- Flow credentials (cookies, bearer tokens, API keys) to destination servers
- Use ASP.NET Core middleware for OIDC, OAuth2, JWT Bearer, Cookie authentication
- Apply custom transforms to modify authentication headers

**❌ YARP CANNOT (natively):**
- Perform OBO (On-Behalf-Of) token exchange
- Acquire new access tokens from identity providers
- Manage token caching and refresh
- Handle complex authentication flows (that's ASP.NET Core middleware's job)
- Replace Microsoft.Identity.Web functionality

#### Authentication Flow in YARP

```csharp
// YARP uses standard ASP.NET Core auth middleware
app.UseAuthentication();  // Authenticate incoming requests
app.UseAuthorization();   // Apply authorization policies
app.MapReverseProxy();    // Proxy authorized requests
```

**Configuration Example:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "protected-route": {
        "ClusterId": "backend-api",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/{**catch-all}" }
      }
    }
  }
}
```

### 2. YARP vs BFF: Architectural Roles

**Source:** Multiple (Microsoft Learn, Sam Newman BFF pattern, industry articles)

#### YARP is NOT a BFF Replacement

YARP and BFF serve different architectural purposes:

| Capability                 | YARP (Reverse Proxy)                | BFF (Backend for Frontend)                |
| -------------------------- | ----------------------------------- | ----------------------------------------- |
| **Primary Role**           | Request forwarding & routing        | Frontend-specific backend logic           |
| **Auth Handling**          | Policy enforcement, credential flow | Token acquisition, OBO flow, user context |
| **Token Management**       | Passes existing tokens              | Acquires, caches, refreshes tokens        |
| **Business Logic**         | None (pure proxy)                   | Data aggregation, transformation          |
| **OAuth 2.0 Flows**        | No (requires middleware)            | Yes (via Microsoft.Identity.Web)          |
| **OBO Token Exchange**     | No                                  | Yes (core BFF feature)                    |
| **Request Transformation** | Yes (headers, paths, etc.)          | Yes (full control)                        |
| **Load Balancing**         | Yes                                 | No                                        |
| **Service Discovery**      | Yes                                 | No                                        |

**Key Insight:** YARP is a **routing & transformation layer**, not an **authentication orchestration layer**.

### 3. Limitations of YARP for Authentication

**Source:** [YARP Authentication Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/authn-authz?view=aspnetcore-10.0#flowing-credentials)

#### Connection-Based Auth Not Supported
- **Windows, Negotiate, NTLM, Kerberos:** Cannot be proxied to downstream services
- Must be converted to another form (e.g., JWT) using custom transforms

#### OBO Flow Requires Custom Implementation
YARP does NOT provide built-in OBO token exchange. You must:
1. Use ASP.NET Core middleware to handle OIDC/OAuth flows
2. Implement custom request transforms to acquire and attach tokens
3. Integrate Microsoft.Identity.Web or similar libraries

**Example (Custom OBO Transform in YARP):**
```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(async transformContext =>
        {
            // Get user authentication ticket
            var ticket = await transformContext.HttpContext
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Acquire OBO token (requires Microsoft.Identity.Web)
            var tokenService = transformContext.HttpContext.RequestServices
                .GetRequiredService<ITokenAcquisition>();
            var token = await tokenService.GetAccessTokenForUserAsync(
                new[] { "api://downstream-api/.default" });

            // Attach bearer token to proxied request
            transformContext.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        });
    });
```

**Critical Observation:** This implementation **embeds BFF logic into YARP**, defeating the purpose of using YARP as a pure proxy.

---

## Should We Migrate Auth from BFF to YARP?

### Decision Matrix

#### ✅ Use YARP for Auth When:
- You have **simple authentication** (JWT validation, basic policy checks)
- Backend APIs **already issue tokens** (no OBO needed)
- You need **API Gateway patterns** (routing, load balancing, rate limiting)
- Multiple microservices share common auth policies
- You want centralized auth enforcement across many services

#### ❌ Do NOT Migrate Auth to YARP When:
- You need **OBO token flows** (BFF pattern requirement)
- Frontend uses **cookie authentication** but backend needs **Bearer tokens**
- You need **token caching and refresh** (Microsoft.Identity.Web features)
- You're integrating with **Microsoft Entra ID** for user-delegated access
- You need **data aggregation or transformation** (BFF responsibility)

### Pros and Cons Analysis

#### Moving Auth to YARP

**Pros:**
- Centralized authorization policy enforcement
- Reduced code in individual services
- Better separation of concerns (routing vs business logic)
- Load balancing and service discovery built-in

**Cons:**
- **Cannot handle OBO flows natively** (requires custom middleware)
- **Loses Microsoft.Identity.Web benefits** (token caching, refresh, consent)
- Increases complexity (YARP + Auth middleware + custom transforms)
- Debugging auth issues becomes harder (distributed across layers)
- Performance overhead (additional network hop)

### Security Implications

**🔒 Security Considerations:**

1. **Token Exposure Risk:** YARP must have access to tokens to forward them
   - BFF pattern keeps tokens server-side only
   - YARP exposes tokens to proxy layer (additional attack surface)

2. **OBO Flow Complexity:** Implementing OBO in YARP requires:
   - Microsoft.Identity.Web integration
   - Custom token acquisition logic
   - Proper error handling for consent, token refresh
   - This duplicates BFF functionality

3. **Consent and Incremental Auth:** Microsoft.Identity.Web handles consent flows
   - YARP does not provide this
   - Must be implemented separately

**Recommendation:** Keep OBO flows in BFF, use YARP for routing/load balancing if needed.

---

## Architecture Scenarios with YARP

### Scenario A: YARP as API Gateway (In Front of Both BFF and Protected API)

**Architecture Diagram:**

```
┌─────────────┐
│   Browser   │ (Cookies)
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────────────────┐
│    YARP API Gateway         │ (Port 7200)
│  - Route-based forwarding   │
│  - No auth logic            │
│  - Load balancing           │
└──────┬──────────────┬───────┘
       │              │
       │ (Cookies)    │ (Bearer)
       ▼              ▼
┌─────────────┐  ┌──────────────┐
│  Blazor BFF │  │ Protected API│
│  Port 7210  │  │  Port 7220   │
│  - Cookie   │  │  - JWT       │
│  - OBO Flow │  │  - Validation│
└─────────────┘  └──────────────┘
```

**Configuration:**

```json
{
  "ReverseProxy": {
    "Routes": {
      "bff-route": {
        "ClusterId": "blazor-bff",
        "Match": { "Path": "/{**catch-all}" },
        "Order": 2
      },
      "api-route": {
        "ClusterId": "protected-api",
        "Match": { "Path": "/api/{**catch-all}" },
        "Order": 1
      }
    },
    "Clusters": {
      "blazor-bff": {
        "Destinations": {
          "bff1": { "Address": "https://localhost:7210" }
        }
      },
      "protected-api": {
        "Destinations": {
          "api1": { "Address": "https://localhost:7220" }
        },
        "LoadBalancingPolicy": "RoundRobin"
      }
    }
  }
}
```

**Pros:**
- Single entry point for all traffic
- Centralized TLS termination
- Load balancing across multiple API instances
- Path-based routing (frontend vs API traffic)

**Cons:**
- Additional network hop (latency)
- No auth value-add (just routing)
- Extra infrastructure to maintain
- Overkill for single-instance demo

**Use Cases:**
- Microservices with multiple backend APIs
- Multi-tenant apps with per-tenant routing
- Production deployments with load balancing needs

---

### Scenario B: YARP Replacing BFF's Proxying Role

**Architecture Diagram:**

```
┌─────────────┐
│   Browser   │ (Cookies)
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────────────────┐
│    YARP + Auth Middleware   │ (Port 7200)
│  - Cookie Auth (ASP.NET)    │
│  - OBO Token Acquisition    │ <-- THIS IS NOW A BFF!
│  - Custom Transforms        │
└──────┬──────────────────────┘
       │ (Bearer)
       ▼
┌──────────────────────────┐
│   Protected API          │ (Port 7220)
│   - JWT Validation       │
└──────────────────────────┘
```

**Implementation (Program.cs):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure authentication (BFF responsibilities)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// YARP with custom OBO transforms
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            // Acquire OBO token for downstream API
            var tokenAcquisition = transformContext.HttpContext.RequestServices
                .GetRequiredService<ITokenAcquisition>();
            
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                new[] { "api://protected-api/.default" });

            transformContext.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        });
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.Run();
```

**Pros:**
- Combines routing and auth in one layer
- Uses YARP's routing/load balancing features
- Still leverages Microsoft.Identity.Web for OBO

**Cons:**
- **YARP + Auth middleware = BFF by another name**
- Doesn't simplify architecture (adds YARP complexity)
- Harder to debug (auth logic in transform pipeline)
- Violates single responsibility principle

**Critical Insight:** This is a **BFF implemented using YARP**, not "YARP replacing BFF." You're just using YARP as the HTTP handling layer instead of ASP.NET Core endpoints.

**Use Cases:**
- When you need YARP's advanced routing features (header-based, query-based)
- Gradual migration from BFF to microservices gateway

---

### Scenario C: YARP as Sole Entry Point (Eliminating BFF)

**Architecture Diagram:**

```
┌─────────────┐
│   Browser   │ (Cookies)
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────────────────┐
│    YARP Gateway             │ (Port 7200)
│  - Cookie Auth              │
│  - JWT Validation           │ <-- NO OBO!
│  - Policy Enforcement       │
└──────┬──────────────────────┘
       │ (Cookies forwarded)
       ▼
┌──────────────────────────┐
│   Protected API          │ (Port 7220)
│   - Cookie Validation    │ <-- Changed from JWT!
│   - Business Logic       │
└──────────────────────────┘
```

**Configuration:**

```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "protected-api",
        "AuthorizationPolicy": "default",
        "Match": { "Path": "/api/{**catch-all}" }
      }
    },
    "Clusters": {
      "protected-api": {
        "Destinations": {
          "api1": { "Address": "https://localhost:7220" }
        }
      }
    }
  }
}
```

**Pros:**
- Simplified architecture (no BFF layer)
- Single authentication point
- YARP handles all routing

**Cons:**
- **LOSES OBO CAPABILITY** (no token exchange)
- Backend API must accept cookies (not ideal for APIs)
- No data aggregation/transformation layer
- Tight coupling between frontend auth and API auth
- Not suitable for Blazor WASM (needs CORS, can't use cookies)

**When This Works:**
- Simple apps with **no OBO requirements**
- Backend APIs that accept **cookie authentication**
- Server-side rendered apps (no WASM client)

**When This FAILS:**
- Blazor WebAssembly apps (CORS issues, cookie limitations)
- APIs requiring **Bearer tokens** (Microsoft Graph, third-party APIs)
- Multi-tenant scenarios with **user-delegated access**

---

## Architecture Decision Matrix

### When to Use Each Scenario

| Requirement            | Scenario A (Gateway) | Scenario B (YARP-BFF) | Scenario C (YARP Only) |
| ---------------------- | -------------------- | --------------------- | ---------------------- |
| **OBO Token Flow**     | ✅ (BFF handles)      | ✅ (YARP+Middleware)   | ❌ Not supported        |
| **Load Balancing**     | ✅ Built-in           | ✅ Built-in            | ✅ Built-in             |
| **Cookie Auth**        | ✅ (BFF)              | ✅ (YARP)              | ✅ (YARP)               |
| **Bearer Token APIs**  | ✅ (BFF converts)     | ✅ (Transform)         | ❌ Cookies only         |
| **Blazor WASM Client** | ✅ (via BFF)          | ✅ (via YARP)          | ❌ CORS issues          |
| **Data Aggregation**   | ✅ (BFF layer)        | ⚠️ (Custom)            | ❌ Not available        |
| **Microservices**      | ✅ Ideal              | ✅ Works               | ⚠️ Limited              |
| **Simple Monolith**    | ❌ Overkill           | ❌ Overkill            | ✅ Reasonable           |

### Recommendation for demo5

**DO NOT migrate demo5 to YARP.**

**Reasoning:**
1. Demo5's OBO flow is the **core feature** being demonstrated
2. YARP adds complexity without providing value for single-instance deployment
3. Microsoft.Identity.Web + BFF pattern is the **recommended approach** for Blazor Web Apps
4. YARP is designed for **microservices architectures** with multiple backend services

**When YARP Makes Sense (Future Demos):**
- **demo6+**: Microservices architecture with 3+ backend APIs
- **Enterprise scenarios**: Load balancing, service mesh, API gateway patterns
- **Multi-region deployments**: Geographic routing, failover

---

## YARP + BFF Pattern (Microsoft Recommended)

**Source:** [Microsoft Learn - Blazor Web App with Entra (BFF Pattern)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-entra?view=aspnetcore-10.0&pivots=bff-pattern)

### Microsoft's Official BFF Implementation Uses YARP

Microsoft's official Blazor Web App security samples use YARP **alongside** BFF, not as a replacement:

**Architecture:**

```
┌──────────────────┐
│  Blazor Web App  │ (BFF + YARP)
│  - Cookie Auth   │
│  - YARP Forwarder│ <-- MapForwarder() for API proxying
│  - OBO Token     │
└────────┬─────────┘
         │ (Bearer via transform)
         ▼
┌──────────────────┐
│  Weather API     │ (Minimal API)
│  - JWT Bearer    │
└──────────────────┘
```

**Key Implementation (from Microsoft sample):**

```csharp
// BFF with YARP forwarder
app.MapForwarder("/weather-forecast", "https://weatherapi", transformBuilder =>
{
    transformBuilder.AddRequestTransform(async transformContext =>
    {
        // Acquire access token for user (OBO flow)
        var tokenAcquisition = transformContext.HttpContext.RequestServices
            .GetRequiredService<ITokenAcquisition>();
        
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
            new[] { weatherApiScope });

        // Attach bearer token to proxied request
        transformContext.ProxyRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    });
}).RequireAuthorization();
```

**Key Insights:**
- YARP is used for **HTTP forwarding** (replaces manual HttpClient calls)
- BFF layer still handles **authentication, OBO, token management**
- This is **Scenario B** from above (YARP-BFF hybrid)

**Benefits over pure BFF:**
- Less boilerplate (no manual HttpClient configuration)
- Built-in error handling for proxying
- Easier to add transforms (headers, paths, etc.)

**When to adopt this pattern:**
- Production Blazor Web Apps with multiple downstream APIs
- Need request transformation (add correlation IDs, logging headers, etc.)
- Want Microsoft's recommended approach

---

## NuGet Packages and Versions

### YARP Packages (.NET 10)

| Package                                      | Version | Purpose                          |
| -------------------------------------------- | ------- | -------------------------------- |
| `Yarp.ReverseProxy`                          | 2.3.0   | Core reverse proxy functionality |
| `Yarp.Telemetry.Consumption`                 | 2.3.0   | Metrics and diagnostics          |
| `Microsoft.Extensions.ServiceDiscovery.Yarp` | 10.0.0  | Service discovery integration    |

**Installation:**
```bash
dotnet add package Yarp.ReverseProxy --version 2.3.0
```

### Microsoft.Identity.Web Integration

If using YARP with OBO flows (Scenario B or Microsoft's BFF pattern):

| Package                                | Version | Purpose                      |
| -------------------------------------- | ------- | ---------------------------- |
| `Microsoft.Identity.Web`               | 4.1.0   | OIDC, OBO, token acquisition |
| `Microsoft.Identity.Web.DownstreamApi` | 4.1.0   | Downstream API client        |
| `Microsoft.Identity.Web.TokenCache`    | 4.1.0   | Token caching                |

---

## Code Examples: YARP Integration Patterns

### Example 1: Basic YARP with Authorization

**appsettings.json:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "public-route": {
        "ClusterId": "backend",
        "AuthorizationPolicy": "anonymous",
        "Match": { "Path": "/public/{**catch-all}" }
      },
      "protected-route": {
        "ClusterId": "backend",
        "AuthorizationPolicy": "default",
        "Match": { "Path": "/api/{**catch-all}" }
      }
    },
    "Clusters": {
      "backend": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:7220"
          }
        }
      }
    }
  }
}
```

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
```

### Example 2: YARP with OBO Transform (Microsoft Pattern)

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// BFF authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
})
.AddIdentityCookies();

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();

// YARP with custom transforms
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        // Apply transform only to protected routes
        if (context.Route.AuthorizationPolicy == "RequireAuth")
        {
            context.AddRequestTransform(async transformContext =>
            {
                // Acquire OBO token
                var tokenAcquisition = transformContext.HttpContext.RequestServices
                    .GetRequiredService<ITokenAcquisition>();
                
                try
                {
                    var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                        new[] { "api://protected-api/.default" });

                    // Attach bearer token
                    transformContext.ProxyRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
                catch (MicrosoftIdentityWebChallengeUserException)
                {
                    // User needs to consent - return 401
                    transformContext.HttpContext.Response.StatusCode = 401;
                    return;
                }
            });
        }
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
```

### Example 3: Load Balancing with Health Checks

**appsettings.json:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": { "Path": "/api/{**catch-all}" }
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "api1": { "Address": "https://api1.example.com" },
          "api2": { "Address": "https://api2.example.com" },
          "api3": { "Address": "https://api3.example.com" }
        },
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:05",
            "Path": "/health"
          },
          "Passive": {
            "Enabled": true,
            "Policy": "TransportFailureRate"
          }
        }
      }
    }
  }
}
```

---

## Enterprise Scenarios for YARP

### Scenario 1: Microservices API Gateway

**Use Case:** 10+ microservices with shared authentication

**Architecture:**
```
YARP Gateway (Port 443)
├── /auth/**       → Auth Service (Port 5001)
├── /users/**      → User Service (Port 5002)
├── /orders/**     → Order Service (Port 5003)
├── /inventory/**  → Inventory Service (Port 5004)
└── /payments/**   → Payment Service (Port 5005)
```

**Benefits:**
- Centralized TLS termination
- Rate limiting per service
- Correlation ID injection
- Load balancing across service instances

### Scenario 2: Multi-Tenant Routing

**Use Case:** Route requests based on tenant ID in subdomain

```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.AddRequestTransform(transformContext =>
        {
            // Extract tenant from subdomain (tenant1.example.com)
            var host = transformContext.HttpContext.Request.Host.Host;
            var tenant = host.Split('.')[0];

            // Add tenant header for downstream services
            transformContext.ProxyRequest.Headers.Add("X-Tenant-ID", tenant);

            return ValueTask.CompletedTask;
        });
    });
```

### Scenario 3: Geographic Routing

**Use Case:** Route to nearest data center based on user location

```json
{
  "ReverseProxy": {
    "Routes": {
      "us-route": {
        "ClusterId": "us-cluster",
        "Match": {
          "Path": "/api/{**catch-all}",
          "Headers": [
            { "Name": "CloudFront-Viewer-Country", "Values": ["US", "CA", "MX"] }
          ]
        }
      },
      "eu-route": {
        "ClusterId": "eu-cluster",
        "Match": {
          "Path": "/api/{**catch-all}",
          "Headers": [
            { "Name": "CloudFront-Viewer-Country", "Values": ["GB", "DE", "FR"] }
          ]
        }
      }
    }
  }
}
```

---

## Performance and Scalability

### YARP Performance Characteristics

**Source:** [YARP GitHub](https://github.com/dotnet/yarp)

- **Throughput:** ~80,000 RPS (requests per second) on typical hardware
- **Latency:** <1ms overhead for simple proxying
- **Memory:** ~50MB base + ~1KB per active connection
- **HTTP/2 & HTTP/3:** Full support (including multiplexing)

**Comparison to BFF:**

| Metric              | BFF (ASP.NET Core)        | YARP Gateway               |
| ------------------- | ------------------------- | -------------------------- |
| **Latency**         | Baseline (0ms)            | +0.5-1ms                   |
| **Code Complexity** | Business logic + routing  | Routing only               |
| **Scalability**     | Vertical (single process) | Horizontal (load balanced) |
| **Memory**          | Higher (BFF features)     | Lower (pure proxy)         |

**Recommendation:** Use YARP when you need horizontal scaling across multiple instances.

---

## Migration Path: BFF to YARP

If you decide to adopt YARP in future demos, follow this incremental approach:

### Phase 1: Add YARP Alongside BFF

```csharp
// Keep existing BFF endpoints
app.MapGet("/api/weather", async (IWeatherService svc) => 
    await svc.GetWeatherAsync());

// Add YARP for new endpoints
app.MapReverseProxy();
```

### Phase 2: Migrate Non-Auth Endpoints to YARP

Move simple pass-through endpoints to YARP configuration:

```json
{
  "ReverseProxy": {
    "Routes": {
      "health-check": {
        "ClusterId": "api",
        "AuthorizationPolicy": "anonymous",
        "Match": { "Path": "/health" }
      }
    }
  }
}
```

### Phase 3: Implement Auth Transforms

Add custom transforms for authenticated endpoints:

```csharp
builder.Services.AddReverseProxy()
    .AddTransforms(/* OBO logic from Example 2 */);
```

### Phase 4: Remove BFF Endpoints

Once YARP handles all routing, remove manual API endpoints.

**Timeline:** 2-4 weeks for gradual migration (production apps)

---

## Debugging and Monitoring

### YARP Telemetry

**Package:** `Yarp.Telemetry.Consumption` v2.3.0

```csharp
// Program.cs
builder.Services.AddTelemetryConsumer<ForwarderTelemetry>();

public class ForwarderTelemetry : IForwarderTelemetryConsumer
{
    public void OnForwarderStart(DateTime timestamp, string destinationPrefix)
    {
        Console.WriteLine($"Proxying to {destinationPrefix}");
    }

    public void OnForwarderStop(DateTime timestamp, int statusCode)
    {
        Console.WriteLine($"Response: {statusCode}");
    }

    public void OnForwarderFailed(DateTime timestamp, ForwarderError error)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Yarp": "Information",
      "Yarp.ReverseProxy": "Debug"
    }
  }
}
```

---

## Recommendations for Implementation

### For Future Demos (demo6+)

**Recommended Use Cases:**

1. **Microservices Demo (demo6):**
   - 3+ separate API projects (Weather, Users, Reports)
   - YARP as unified API Gateway (Scenario A)
   - BFF still handles OBO for each API

2. **Multi-Region Demo (demo7):**
   - Geographic routing based on user location
   - Health checks and automatic failover
   - YARP with active/passive clusters

3. **Rate Limiting Demo (demo8):**
   - YARP with rate limiting middleware
   - Per-user or per-tenant quotas
   - Integration with ASP.NET Core rate limiting

### Anti-Patterns to Avoid

❌ **Don't:** Use YARP to replace OBO flows (requires custom middleware, defeats purpose)  
❌ **Don't:** Add YARP to single-API apps (unnecessary complexity)  
❌ **Don't:** Implement business logic in YARP transforms (that's BFF's job)  
❌ **Don't:** Use YARP for simple path rewriting (ASP.NET Core middleware is simpler)

✅ **Do:** Use YARP for routing, load balancing, service discovery  
✅ **Do:** Keep auth logic in dedicated BFF/middleware layers  
✅ **Do:** Use YARP's built-in features (health checks, telemetry)  
✅ **Do:** Follow Microsoft's BFF+YARP pattern for production apps

---

## References

### Official Microsoft Documentation

1. [YARP Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/yarp-overview?view=aspnetcore-10.0)
2. [YARP Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/authn-authz?view=aspnetcore-10.0)
3. [YARP Request Transforms](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/transforms?view=aspnetcore-10.0)
4. [Blazor Web App with Entra (BFF Pattern)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-entra?view=aspnetcore-10.0&pivots=bff-pattern)
5. [Blazor Web App with OIDC (BFF Pattern)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-10.0&pivots=bff-pattern)
6. [Microsoft Identity Platform - OBO Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)

### NuGet Packages

1. [Yarp.ReverseProxy 2.3.0](https://www.nuget.org/packages/Yarp.ReverseProxy)
2. [Yarp.Telemetry.Consumption 2.3.0](https://www.nuget.org/packages/Yarp.Telemetry.Consumption)
3. [Microsoft.Extensions.ServiceDiscovery.Yarp 10.0.0](https://www.nuget.org/packages/Microsoft.Extensions.ServiceDiscovery.Yarp)

### GitHub Repositories

1. [YARP (dotnet/yarp)](https://github.com/dotnet/yarp)
2. [YARP Samples](https://github.com/dotnet/yarp/tree/main/samples)
3. [Blazor Samples (BFF Pattern)](https://github.com/dotnet/blazor-samples)

### Community Resources

1. [Sam Newman - Backends For Frontends](https://samnewman.io/patterns/architectural/bff/)
2. [API Gateway vs BFF (GeeksforGeeks)](https://www.geeksforgeeks.org/system-design/api-gateway-vs-backend-for-frontend-bff/)
3. [YARP Auth Proxy (manfredsteyer)](https://github.com/manfredsteyer/yarp-auth-proxy)
4. [Milan Jovanović - API Gateway with YARP](https://www.milanjovanovic.tech/blog/implementing-api-gateway-authentication-with-yarp)

---

## Summary and Conclusion

### Key Takeaways

1. **YARP is NOT a BFF replacement** - it's a reverse proxy toolkit that complements BFF architecture
2. **OBO flows require BFF or custom middleware** - YARP cannot do this natively
3. **Microsoft recommends YARP + BFF** for production Blazor Web Apps (not YARP alone)
4. **Use YARP for microservices** with 3+ backend services, not single-API demos
5. **Keep auth logic in BFF layer** - use YARP for routing and transformation

### Decision Tree for Future Demos

```
Do you have 3+ separate backend APIs?
├── Yes → Consider YARP as API Gateway (Scenario A)
└── No  → Stick with BFF pattern (demo5 approach)

Do you need load balancing or geographic routing?
├── Yes → YARP is the right tool
└── No  → ASP.NET Core endpoints are simpler

Do you need OBO token flows?
├── Yes → Keep BFF layer (Microsoft.Identity.Web)
└── No  → YARP alone might work (rare scenario)
```

### Final Recommendation

**For demo5:** Keep existing BFF architecture (no YARP)  
**For demo6+:** Introduce YARP if building microservices architecture  
**For production apps:** Follow Microsoft's BFF+YARP pattern from Blazor security samples

---

## OBO Token Flow: Blazor BFF to Internal Microservices

This section illustrates the complete request lifecycle when a user action in the Blazor WebAssembly client triggers an API call that flows through the BFF layer to an internal protected microservice (Weather, User, or Report Service).

### Sequence Diagram: End-to-End OBO Flow

```text
┌─────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Browser │     │ YARP Gateway│     │ Blazor BFF  │     │ Entra ID    │     │Weather Svc  │
│ (WASM)  │     │ (Optional)  │     │ Port 7210   │     │ (Token Svc) │     │ Port 5001   │
└────┬────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘
     │                 │                   │                   │                   │
     │ 1. User clicks "Get Weather"        │                   │                   │
     │─────────────────────────────────────>                   │                   │
     │    HTTP GET /api/weather            │                   │                   │
     │    Cookie: .AspNetCore.Identity=... │                   │                   │
     │                 │                   │                   │                   │
     │                 │ 2. Route to BFF   │                   │                   │
     │                 │───────────────────>                   │                   │
     │                 │                   │                   │                   │
     │                 │                   │ 3. Validate Cookie│                   │
     │                 │                   │ Extract user claims                   │
     │                 │                   │ (ClaimsPrincipal) │                   │
     │                 │                   │                   │                   │
     │                 │                   │ 4. Check Token Cache                  │
     │                 │                   │ (ITokenAcquisition)│                   │
     │                 │                   │                   │                   │
     │                 │                   │      ┌────────────┴───────────┐       │
     │                 │                   │      │  Cache Hit?            │       │
     │                 │                   │      │  ├─ Yes: Use cached    │       │
     │                 │                   │      │  │       token         │       │
     │                 │                   │      │  └─ No: Request OBO    │       │
     │                 │                   │      └────────────┬───────────┘       │
     │                 │                   │                   │                   │
     │                 │                   │ 5. OBO Token Request                  │
     │                 │                   │───────────────────>                   │
     │                 │                   │ POST /oauth2/token │                   │
     │                 │                   │ grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
     │                 │                   │ assertion=<user's_access_token>       │
     │                 │                   │ scope=api://weather-svc/.default      │
     │                 │                   │ client_id=<bff_client_id>             │
     │                 │                   │ client_secret=<bff_secret>            │
     │                 │                   │                   │                   │
     │                 │                   │ 6. OBO Token Response                 │
     │                 │                   │<──────────────────│                   │
     │                 │                   │ {                 │                   │
     │                 │                   │   "access_token": "eyJ0eXAi...",      │
     │                 │                   │   "token_type": "Bearer",             │
     │                 │                   │   "expires_in": 3599                  │
     │                 │                   │ }                 │                   │
     │                 │                   │                   │                   │
     │                 │                   │ 7. Cache token    │                   │
     │                 │                   │ (AddInMemoryTokenCaches)              │
     │                 │                   │                   │                   │
     │                 │                   │ 8. Call Weather Service               │
     │                 │                   │──────────────────────────────────────>│
     │                 │                   │ GET /api/forecast │                   │
     │                 │                   │ Authorization: Bearer eyJ0eXAi...     │
     │                 │                   │                   │                   │
     │                 │                   │                   │ 9. Validate JWT   │
     │                 │                   │                   │ - Check signature │
     │                 │                   │                   │ - Verify audience │
     │                 │                   │                   │ - Check expiration│
     │                 │                   │                   │ - Extract claims  │
     │                 │                   │                   │                   │
     │                 │                   │                   │ 10. Authorization │
     │                 │                   │                   │ - Check scopes    │
     │                 │                   │                   │ - Permission check│
     │                 │                   │                   │                   │
     │                 │                   │ 11. Weather Data  │                   │
     │                 │                   │<──────────────────────────────────────│
     │                 │                   │ 200 OK            │                   │
     │                 │                   │ [{ temp: 72, ... }]                   │
     │                 │                   │                   │                   │
     │                 │ 12. Forward Response                  │                   │
     │                 │<──────────────────│                   │                   │
     │                 │                   │                   │                   │
     │ 13. Render Weather Data             │                   │                   │
     │<─────────────────────────────────────                   │                   │
     │ 200 OK          │                   │                   │                   │
     │ [{ temp: 72, ... }]                 │                   │                   │
     │                 │                   │                   │                   │
```

### Step-by-Step Breakdown

| Step      | Component       | Action                 | Details                                                                               |
| --------- | --------------- | ---------------------- | ------------------------------------------------------------------------------------- |
| **1**     | Browser (WASM)  | User initiates request | Blazor WASM sends `fetch()` with `credentials: 'include'` to include cookies          |
| **2**     | YARP Gateway    | Route request          | Forwards `/api/**` to BFF (if YARP is present; otherwise direct to BFF)               |
| **3**     | Blazor BFF      | Validate cookie        | ASP.NET Core Cookie Authentication middleware validates `.AspNetCore.Identity` cookie |
| **4**     | Blazor BFF      | Check token cache      | `ITokenAcquisition` checks in-memory (or distributed) cache for valid token           |
| **5**     | Blazor BFF      | OBO token request      | If no cached token, BFF sends OBO grant to Entra ID with user's token as assertion    |
| **6**     | Entra ID        | Issue OBO token        | Returns new access token scoped for the specific microservice                         |
| **7**     | Blazor BFF      | Cache token            | Stores token with key `{client_id}_{user_id}_{scope}` for future requests             |
| **8**     | Blazor BFF      | Call microservice      | Sends request to Weather Service with `Authorization: Bearer <token>`                 |
| **9**     | Weather Service | Validate JWT           | Microsoft.Identity.Web validates signature, audience, issuer, expiration              |
| **10**    | Weather Service | Authorization check    | Verifies user has required scopes/permissions for the endpoint                        |
| **11**    | Weather Service | Return data            | Business logic executes, returns weather forecast data                                |
| **12-13** | BFF → Browser   | Forward response       | BFF proxies response back to browser client                                           |

### Token Transformation: Cookie → Bearer

The key security transformation in the BFF pattern is converting **cookie-based browser authentication** to **Bearer token API authentication**:

```text
┌───────────────────────────────────────────────────────────────────┐
│                    TOKEN TRANSFORMATION                           │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Browser                    BFF                      Microservice │
│  ┌─────────┐           ┌──────────┐               ┌────────────┐ │
│  │ Cookie  │           │ OBO Flow │               │ JWT Bearer │ │
│  │ (Secure,│    →      │ Exchange │       →       │ Validation │ │
│  │ HttpOnly)│          │          │               │            │ │
│  └─────────┘           └──────────┘               └────────────┘ │
│                                                                   │
│  ✅ Secure:             ✅ Confidential:           ✅ Standard:   │
│  - Not accessible       - Has client secret       - JWT format   │
│    via JavaScript       - Server-side only        - Audience     │
│  - SameSite=Strict      - Token never in          - Scopes       │
│  - HTTPS only             browser                 - Signature    │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

### OBO Token Request Details

**Request to Entra ID:**

```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
&client_id=<bff-app-client-id>
&client_secret=<bff-app-client-secret>
&assertion=<user's-access-token-from-initial-login>
&scope=api://weather-service/.default
&requested_token_use=on_behalf_of
```

**Response from Entra ID:**

```json
{
  "token_type": "Bearer",
  "scope": "api://weather-service/Forecast.Read",
  "expires_in": 3599,
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6...",
  "refresh_token": "0.AXEAO..."
}
```

### Multi-Service OBO Flow

When BFF needs to call **multiple microservices** for a single user request (data aggregation):

```text
┌─────────┐     ┌─────────────┐     ┌─────────────┐
│ Browser │     │ Blazor BFF  │     │ Entra ID    │
└────┬────┘     └──────┬──────┘     └──────┬──────┘
     │                 │                   │
     │ GET /dashboard  │                   │
     │─────────────────>                   │
     │                 │                   │
     │                 │ OBO for Weather   │
     │                 │───────────────────>   ──┐
     │                 │<───────────────────     │ (Can be cached
     │                 │                   │     │  for each scope)
     │                 │ OBO for Users     │     │
     │                 │───────────────────>   ──┤
     │                 │<───────────────────     │
     │                 │                   │     │
     │                 │ OBO for Reports   │     │
     │                 │───────────────────>   ──┘
     │                 │<───────────────────
     │                 │                   │
     │                 │                   │
     │                 │    ┌──────────────────────────────────────┐
     │                 │    │ Parallel calls with different tokens │
     │                 │    └──────────────────────────────────────┘
     │                 │                   │
     │                 │ GET /api/forecast │         ┌─────────────┐
     │                 │───────────────────────────>│Weather Svc  │
     │                 │ GET /api/users    │         ├─────────────┤
     │                 │───────────────────────────>│User Svc     │
     │                 │ GET /api/reports  │         ├─────────────┤
     │                 │───────────────────────────>│Report Svc   │
     │                 │                   │         └─────────────┘
     │                 │                   │
     │                 │<──────────────────────────── (All responses)
     │                 │                   │
     │                 │ Aggregate & Return│
     │<─────────────────                   │
     │ { weather, users, reports }         │
     │                 │                   │
```

### Error Handling in OBO Flow

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                        ERROR SCENARIOS                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ❌ AADSTS65001: Consent Required                                       │
│     └─ User hasn't consented to scopes for this API                    │
│     └─ Solution: Redirect to /MicrosoftIdentity/Account/Challenge      │
│                                                                         │
│  ❌ AADSTS50013: Assertion expired                                      │
│     └─ User's original token expired                                   │
│     └─ Solution: Force re-authentication via challenge                 │
│                                                                         │
│  ❌ AADSTS700024: Client secret expired                                 │
│     └─ BFF's client secret needs rotation                              │
│     └─ Solution: Update secret in Azure Key Vault                      │
│                                                                         │
│  ❌ 401 from Microservice                                               │
│     └─ Token audience mismatch or expired                              │
│     └─ Solution: Verify API app registration audience                  │
│                                                                         │
│  ❌ 403 from Microservice                                               │
│     └─ Valid token but insufficient scopes/permissions                 │
│     └─ Solution: Request additional scopes or update permissions       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Code Example: BFF Calling Weather Service

```csharp
// ServerWeatherService.cs (BFF layer)
public class ServerWeatherService : IWeatherService
{
    private readonly IDownstreamApi _downstreamApi;
    private readonly ILogger<ServerWeatherService> _logger;

    public ServerWeatherService(
        IDownstreamApi downstreamApi,
        ILogger<ServerWeatherService> logger)
    {
        _downstreamApi = downstreamApi;
        _logger = logger;
    }

    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
        try
        {
            // IDownstreamApi handles OBO automatically:
            // 1. Checks token cache
            // 2. Performs OBO exchange if needed
            // 3. Attaches Bearer token to request
            var forecasts = await _downstreamApi.GetForUserAsync<WeatherForecast[]>(
                "WeatherService",  // Configured in appsettings.json
                options =>
                {
                    options.RelativePath = "api/forecast";
                });

            _logger.LogInformation("Retrieved {Count} forecasts from Weather Service", 
                forecasts?.Length ?? 0);
            
            return forecasts ?? [];
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            // User needs to consent - throw to trigger auth challenge
            _logger.LogWarning("Consent required for Weather Service: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Weather Service call failed");
            throw;
        }
    }
}
```

**appsettings.json Configuration:**

```json
{
  "WeatherService": {
    "BaseUrl": "https://localhost:5001",
    "Scopes": ["api://weather-service/Forecast.Read"]
  },
  "UserService": {
    "BaseUrl": "https://localhost:5002",
    "Scopes": ["api://user-service/Users.Read"]
  },
  "ReportService": {
    "BaseUrl": "https://localhost:5003",
    "Scopes": ["api://report-service/Reports.Read"]
  }
}
```

### Token Claims Flow

Understanding what claims flow through the system:

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                        CLAIMS TRANSFORMATION                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  BROWSER COOKIE (Identity Cookie)                                       │
│  ├─ sub: "user-object-id"                                              │
│  ├─ name: "John Doe"                                                   │
│  ├─ email: "john@contoso.com"                                          │
│  ├─ roles: ["Admin", "User"]                                           │
│  └─ tid: "tenant-id"                                                   │
│                                                                         │
│        ↓ Cookie validated, claims extracted                             │
│                                                                         │
│  BFF ClaimsPrincipal                                                    │
│  ├─ sub: "user-object-id"                                              │
│  ├─ name: "John Doe"                                                   │
│  ├─ email: "john@contoso.com"                                          │
│  ├─ roles: ["Admin", "User"]                                           │
│  └─ tid: "tenant-id"                                                   │
│                                                                         │
│        ↓ OBO exchange adds API-specific claims                          │
│                                                                         │
│  OBO ACCESS TOKEN (for Weather Service)                                 │
│  ├─ aud: "api://weather-service"          ← API identifier             │
│  ├─ iss: "https://login.microsoftonline.com/{tenant}/v2.0"             │
│  ├─ sub: "user-object-id"                 ← User identity preserved    │
│  ├─ oid: "user-object-id"                                              │
│  ├─ name: "John Doe"                                                   │
│  ├─ scp: "Forecast.Read"                  ← API scope                  │
│  ├─ azp: "bff-client-id"                  ← Calling app               │
│  └─ exp: 1732550400                       ← Expiration                 │
│                                                                         │
│        ↓ Microservice validates and uses claims                         │
│                                                                         │
│  MICROSERVICE ClaimsPrincipal                                           │
│  ├─ All claims from OBO token                                          │
│  ├─ Custom claims added by ClaimsTransformation (if any)               │
│  └─ Used for authorization decisions                                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Appendix: Architecture Diagrams (ASCII)

### Current Demo5 Architecture (No YARP)

```
┌──────────────────────────────────────────────────┐
│          Blazor Web App (BFF)                    │
│          Port 7210                               │
│                                                  │
│  ┌─────────────────────────────────────────┐    │
│  │  Browser → Cookie Auth                  │    │
│  └─────────────┬───────────────────────────┘    │
│                │                                 │
│  ┌─────────────▼───────────────────────────┐    │
│  │  Identity Server Integration            │    │
│  │  - OIDC Authentication                  │    │
│  │  - OBO Token Acquisition                │    │
│  └─────────────┬───────────────────────────┘    │
│                │                                 │
│  ┌─────────────▼───────────────────────────┐    │
│  │  API Endpoints (Minimal API)            │    │
│  │  - /api/weather                         │    │
│  │  - /api/users                           │    │
│  │  - Uses ServerWeatherService            │    │
│  └─────────────┬───────────────────────────┘    │
└────────────────┼─────────────────────────────────┘
                 │ HTTPS (Bearer Token)
                 │
┌────────────────▼─────────────────────────────────┐
│          Protected API                           │
│          Port 7220                               │
│                                                  │
│  ┌─────────────────────────────────────────┐    │
│  │  JWT Bearer Validation                  │    │
│  └─────────────┬───────────────────────────┘    │
│                │                                 │
│  ┌─────────────▼───────────────────────────┐    │
│  │  Business Logic                         │    │
│  │  - Weather forecasts                    │    │
│  │  - Permission checks                    │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```

### Recommended Future Architecture (YARP + BFF + Microservices)

```text
                           ┌─────────────────────────────────────────────────────────┐
                           │                    INTERNET                             │
                           └────────────────────────┬────────────────────────────────┘
                                                    │
                    ┌───────────────────────────────▼───────────────────────────────┐
                    │                      YARP API Gateway                         │
                    │                      Port 443 (TLS Termination)               │
                    │                                                               │
                    │  Route Configuration:                                         │
                    │  ┌─────────────────────────────────────────────────────────┐ │
                    │  │ • /app/**        → BFF (browser UI requests)            │ │
                    │  │ • /external-api/** → Microservices (external clients)   │ │
                    │  │   (Load Balancing, Rate Limiting, Auth Enforcement)     │ │
                    │  └─────────────────────────────────────────────────────────┘ │
                    └───────────────────────────────┬───────────────────────────────┘
                                                    │
                         ┌──────────────────────────┴──────────────────────────┐
                         │ External Traffic                                    │
                         │ (via YARP)                                          │
                         ▼                                                     ▼
    ┌────────────────────────────────────────┐          ┌───────────────────────────────┐
    │           Blazor BFF                   │          │  External API Clients         │
    │           Port 7210                    │          │  (Mobile Apps, Partners, etc) │
    │                                        │          └───────────────┬───────────────┘
    │  • Cookie Authentication               │                          │
    │  • OBO Token Acquisition               │                          │ Bearer Token
    │  • UI Assets (/app/**)                 │                          │ (via YARP)
    │  • BFF API Endpoints                   │                          │
    └────────────────┬───────────────────────┘                          │
                     │                                                   │
                     │ ┌─────────────────────────────────────────────────┘
                     │ │
                     │ │  INTERNAL NETWORK (Trusted Zone)
    ═════════════════╪═╪═══════════════════════════════════════════════════════════════
                     │ │
                     │ │  BFF calls microservices DIRECTLY (bypasses YARP)
                     │ │  • Lower latency (no extra hop)
                     │ │  • OBO token already acquired
                     │ │  • Internal service-to-service communication
                     │ │
                     ▼ ▼
    ┌────────────────────────────────────────────────────────────────────────────────┐
    │                           MICROSERVICES CLUSTER                                │
    │                                                                                │
    │  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐ │
    │  │  Weather Service     │  │  User Service        │  │  Report Service      │ │
    │  │  Port 5001           │  │  Port 5002           │  │  Port 5003           │ │
    │  │                      │  │                      │  │                      │ │
    │  │  • JWT Bearer Auth   │  │  • JWT Bearer Auth   │  │  • JWT Bearer Auth   │ │
    │  │  • /api/forecast     │  │  • /api/users        │  │  • /api/reports      │ │
    │  │  • Business Logic    │  │  • Business Logic    │  │  • Business Logic    │ │
    │  └──────────────────────┘  └──────────────────────┘  └──────────────────────┘ │
    │                                                                                │
    └────────────────────────────────────────────────────────────────────────────────┘
```

### Traffic Flow Clarification

**Important:** The BFF does NOT go through YARP to reach microservices. There are two distinct traffic patterns:

| Traffic Source            | Path                         | Auth Method      | Use Case                   |
| ------------------------- | ---------------------------- | ---------------- | -------------------------- |
| **Browser → BFF**         | Browser → YARP → BFF         | Cookie           | User accessing Blazor UI   |
| **BFF → Microservices**   | BFF → Direct HTTPS           | OBO Bearer Token | BFF calling internal APIs  |
| **External Client → API** | Client → YARP → Microservice | Bearer Token     | Mobile apps, partners, M2M |

```text
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          TRAFFIC FLOW SUMMARY                                   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  PATTERN 1: Browser User Request (OBO Flow)                                    │
│  ───────────────────────────────────────────                                    │
│                                                                                 │
│    Browser ──Cookie──► YARP ──Cookie──► BFF ──OBO Token──► Weather Service     │
│       │                  │                │                      │              │
│       │                  │                │   (Direct HTTPS,     │              │
│       │                  │                │    no YARP hop)      │              │
│       │                  │                │                      │              │
│       ◄──── JSON ────────┴────────────────┴──────────────────────┘              │
│                                                                                 │
│                                                                                 │
│  PATTERN 2: External API Client (Direct Bearer)                                │
│  ──────────────────────────────────────────────                                 │
│                                                                                 │
│    Mobile App ──Bearer──► YARP ──Bearer──► Weather Service                     │
│       │                     │                    │                              │
│       │    (YARP validates  │    (Service also   │                              │
│       │     auth policy)    │     validates JWT) │                              │
│       │                     │                    │                              │
│       ◄──── JSON ───────────┴────────────────────┘                              │
│                                                                                 │
│                                                                                 │
│  PATTERN 3: Service-to-Service (Internal)                                      │
│  ─────────────────────────────────────────                                      │
│                                                                                 │
│    Weather Service ──Bearer──► User Service                                    │
│       │                             │                                           │
│       │   (Internal network,        │                                           │
│       │    no YARP involved)        │                                           │
│       │                             │                                           │
│       ◄──── JSON ───────────────────┘                                           │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Why BFF Bypasses YARP for Microservice Calls

| Reason                    | Explanation                                                       |
| ------------------------- | ----------------------------------------------------------------- |
| **Lower Latency**         | One less network hop = faster response times                      |
| **Already Authenticated** | BFF has already validated the user via cookie; OBO token is ready |
| **Trusted Network**       | BFF and microservices are in the same internal network/cluster    |
| **No Added Value**        | YARP's auth enforcement isn't needed—BFF already handles auth     |
| **Simpler Debugging**     | Direct calls are easier to trace than proxy chains                |

### When YARP Routes TO Microservices

YARP only routes directly to microservices when:

1. **External API clients** (mobile apps, partners) call `/external-api/**` endpoints
2. **Public APIs** that don't require OBO (using client credentials or pre-issued tokens)
3. **Health checks** and monitoring endpoints

For **user-initiated requests from the browser**, the flow is always:

```text
Browser → YARP → BFF → (Direct) → Microservice
```

## Internal Network Security (Docker Compose/Kubernetes)

### The Question: HTTP vs HTTPS for Internal Services?

When deploying in Docker Compose (or Kubernetes), services communicate over internal networks that are isolated from external traffic. This raises an important question:

> **Do we still need HTTPS between BFF and internal microservices?**

### Docker Network Security Model

```text
┌────────────────────────── Docker Host ──────────────────────────┐
│                                                                  │
│  ┌─── External Network (Bridge) ────┐                           │
│  │                                   │                          │
│  │  ┌─────────┐                     │                           │
│  │  │  YARP   │ ← Port 443 exposed  │                           │
│  │  │ Gateway │   to external       │                           │
│  │  └────┬────┘                     │                           │
│  │       │                          │                           │
│  └───────│──────────────────────────┘                           │
│          │                                                       │
│  ┌───────│─── Internal Network (No external exposure) ───────┐  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  ┌─────────┐        ┌──────────────┐       ┌───────────┐  │  │
│  │  │   BFF   │───────▶│ Weather API  │       │ Users API │  │  │
│  │  │ :5000   │  HTTP? │    :5001     │       │   :5002   │  │  │
│  │  └─────────┘        └──────────────┘       └───────────┘  │  │
│  │                                                            │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Three Approaches

| Approach                 | Description                                        | Complexity | Security Level    |
| ------------------------ | -------------------------------------------------- | ---------- | ----------------- |
| **1. HTTP Internal**     | Use HTTP for all internal service-to-service calls | Low        | Adequate for most |
| **2. HTTPS Self-Signed** | Generate self-signed certs for internal services   | Medium     | Higher            |
| **3. Service Mesh mTLS** | Use Linkerd/Istio for automatic mTLS               | High       | Highest           |

### Approach 1: HTTP for Internal Communication (Common Pattern)

**Docker Compose Configuration:**

```yaml
services:
  yarp-gateway:
    image: myapp/gateway
    ports:
      - "443:443"  # Only HTTPS exposed externally
    networks:
      - external
      - internal

  bff:
    image: myapp/bff
    networks:
      - internal  # No external exposure
    environment:
      - ASPNETCORE_URLS=http://+:5000
      - WeatherApi__BaseUrl=http://weather-api:5001
      - UsersApi__BaseUrl=http://users-api:5002

  weather-api:
    image: myapp/weather-api
    networks:
      - internal
    environment:
      - ASPNETCORE_URLS=http://+:5001

networks:
  external:
    driver: bridge
  internal:
    driver: bridge
    internal: true  # No external gateway, fully isolated
```

**ASP.NET Core Configuration (BFF):**

```csharp
// Program.cs - No HTTPS redirection for internal calls
if (!app.Environment.IsDevelopment())
{
    // YARP terminates TLS, internal traffic is HTTP
    app.UseHsts();  // Only for responses going back through YARP
}

// HttpClient for internal services - plain HTTP
builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri("http://weather-api:5001");
});
```

### Approach 2: HTTPS with Self-Signed Certificates

For environments requiring encryption in transit (compliance, defense in depth):

**Certificate Generation:**

```bash
# Generate CA and service certificates
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ca.key -out ca.crt -subj "/CN=Internal CA"

# Generate service certificate signed by CA
openssl req -nodes -newkey rsa:2048 \
    -keyout weather-api.key -out weather-api.csr \
    -subj "/CN=weather-api"
    
openssl x509 -req -in weather-api.csr -CA ca.crt -CAkey ca.key \
    -CAcreateserial -out weather-api.crt -days 365
```

**Docker Compose with Certificates:**

```yaml
services:
  bff:
    volumes:
      - ./certs/ca.crt:/etc/ssl/certs/internal-ca.crt:ro
    environment:
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/bff.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}

  weather-api:
    volumes:
      - ./certs/weather-api.pfx:/etc/ssl/certs/weather-api.pfx:ro
```

### Approach 3: Service Mesh (mTLS)

For production Kubernetes environments with strict security requirements:

```yaml
# Kubernetes with Linkerd service mesh
apiVersion: apps/v1
kind: Deployment
metadata:
  name: bff
  annotations:
    linkerd.io/inject: enabled  # Automatic mTLS sidecar
spec:
  template:
    spec:
      containers:
        - name: bff
          ports:
            - containerPort: 5000  # HTTP - Linkerd handles mTLS
```

### Decision Matrix: When to Use What

| Factor               | HTTP OK                      | HTTPS Required              |
| -------------------- | ---------------------------- | --------------------------- |
| **Compliance**       | Internal apps, no PCI/HIPAA  | PCI-DSS, HIPAA, SOC2        |
| **Data Sensitivity** | Non-PII, internal metrics    | PII, financial data, tokens |
| **Threat Model**     | Trust internal network       | Zero-trust architecture     |
| **Environment**      | Dev, staging, internal tools | Production, multi-tenant    |
| **Team Expertise**   | Limited PKI experience       | Dedicated security team     |

### Practical Recommendation for Demo Environment

For **demo6+** (YARP integration demo), we recommend:

1. **Development:** HTTP internal (simplicity)
2. **Staging:** Optional HTTPS with self-signed certs (testing)
3. **Production simulation:** Service mesh or HTTPS

```jsonc
// appsettings.Production.json
{
  "DownstreamApis": {
    "WeatherApi": {
      // In Docker Compose: HTTP with isolated network
      // In Kubernetes: Service mesh provides mTLS
      "BaseUrl": "http://weather-api:5001"
    }
  }
}
```

### Key Insight

The **critical security boundary** is between external and internal networks:

- ✅ **YARP Gateway** must use HTTPS (TLS termination)
- ✅ **BFF** can use HTTP internally if network isolation is enforced
- ✅ **Microservices** can use HTTP internally
- ⚠️ **Tokens/secrets** should still use secure transport if not using service mesh

```text
[Internet] ──HTTPS──▶ [YARP:443] ──HTTP──▶ [BFF:5000] ──HTTP──▶ [API:5001]
              ▲                                │
              │                                │
         TLS Required                   Network Isolation
         (untrusted)                    or Service Mesh
```

---

**Research Completed:** November 25, 2025  
**Next Steps:** Review with team before implementing in future demos  
**Document Version:** 1.0
