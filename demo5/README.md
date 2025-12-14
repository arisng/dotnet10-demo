# Demo5: Downstream API Integration

## Goal

Create a standalone protected API service and consume it from the Blazor app using Entra ID tokens, contrasting the BFF (Cookie) vs. Downstream (Token) architectures.

## Architecture Guide

For detailed explanations of the concepts in this demo, see [ARCHITECTURE_DEEP_DIVE.md](.docs/issues/ARCHITECTURE_DEEP_DIVE.md), which covers:
- IDownstreamApi registration patterns and multi-API scenarios
- API hosting architecture decisions (separate process vs co-hosted)
- Complete OBO token lifecycle and flow
- Security considerations and troubleshooting

This README focuses on getting the demo running. Read the architecture guide for deeper understanding.

## Glossary

### Key Terms in Demo5 Context

| Term               | Definition                                                                                                                                                                             | Example in Demo5                                                                                               | Contrast                                                                                                       |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| **Downstream API** | An API called by the application (BFF) on behalf of the user or client. The application makes outbound calls to fetch data or perform operations.                                      | `Demo5.DownstreamApi.WeatherApi` (port 7220) is a downstream API called by the BFF app (port 7210).            | **Upstream API**: The API that receives calls (rare perspective in client-server architecture).                |
| **Private API**    | An API designed for internal use only, with no public access. Typically protected by authentication and authorization. Consumed by specific applications under organizational control. | `Demo5.DownstreamApi.WeatherApi` is private—only the Blazor BFF can call it. Not accessible from the internet. | **Public API**: Open to external developers/applications (e.g., Twitter API, GitHub API).                      |
| **Internal API**   | An API owned and operated by the same organization, running within your infrastructure or cloud environment.                                                                           | `Demo5.DownstreamApi.WeatherApi` is internal—developed and maintained by the same team running the Blazor app. | **External API**: Provided by third-party vendors or SaaS providers (e.g., Microsoft Graph, Stripe, SendGrid). |
| **External API**   | An API provided by a third-party service or SaaS provider outside your organization.                                                                                                   | Microsoft Graph (user profile data) is external—Microsoft owns and operates it.                                | **Internal API**: Owned by your organization.                                                                  |
| **SaaS API**       | A Software-as-a-Service API provided by a cloud vendor. The vendor manages hosting, scaling, security, and uptime. Accessed via HTTPS with API keys, OAuth tokens, or credentials.     | Microsoft Graph is a SaaS API—Microsoft manages all infrastructure, updates, and availability.                 | **On-Premises API**: API you host and maintain on your own servers.                                            |

### Essential Authentication & Authorization Terms

| Term               | Definition                                                                                                                                                                                      | Example in Demo5                                                                                                                             | Related Terms                                                        |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| **Bearer Token**   | A token (usually JWT) sent in the HTTP `Authorization` header with the format `Bearer <token>`. The API validates this token to authorize requests.                                            | WeatherApi validates Bearer tokens from the BFF to confirm the caller has `Forecast.Read` scope.                                           | Access Token, JWT, OAuth Token                                      |
| **JWT (JSON Web Token)** | A cryptographically signed token containing claims (user info, permissions, scopes). Human-readable (base64-encoded) and self-validating without a database lookup. Format: `header.payload.signature`. | Entra ID issues JWTs for both the BFF and downstream API calls. Inspect at https://jwt.ms to see claims like `oid`, `scp`, `aud`.            | Claims, Token Claims, Signature                                     |
| **OAuth Scope**    | A permission label defining what an application can do on behalf of a user. Scopes are requested during login; users grant consent. Enforced by the API.                                         | `Forecast.Read` scope lets the Blazor app call WeatherApi. User grants consent once; BFF uses it for subsequent calls.                      | Permission, Delegation, Consent                                     |
| **OBO (On-Behalf-Of) Flow** | An OAuth pattern where the application exchanges the user's token for a new token scoped for a downstream API, maintaining user identity across service boundaries.                              | BFF receives user's Entra ID token, exchanges it with Entra ID for a **delegated access token** scoped to WeatherApi. WeatherApi validates the token and sees the user's identity.       | Token Exchange, Delegation Flow, Confidential Client                |
| **Client Credentials Flow** | An OAuth pattern for service-to-service communication where an application authenticates using client ID and client secret, without user involvement. Results in an app-only access token.       | Not used in demo5. Example: a scheduled job service calling WeatherApi without user context would use client credentials flow with app-only token.  | App-Only Access Token, OAuth Grant, Service Account                 |
| **IDownstreamApi** | Microsoft.Identity.Web service that automates token acquisition, caching, and refresh for calling downstream APIs. Handles OBO flow transparently.                                             | `IDownstreamApi.GetForUserAsync<T>("WeatherApi", ...)` retrieves user profile data with automatic Bearer token attachment.                  | Token Acquisition, ITokenAcquisition                                |
| **Audience (aud)** | A JWT claim identifying the intended recipient of the token. APIs validate that the `aud` matches their own identity to prevent token misuse.                                                  | WeatherApi expects tokens with `aud: api://[api-client-id]`. Tokens for other APIs (e.g., Graph) are rejected.                             | Token Validation, JWT Claims                                        |
| **Delegated Access Token** | An access token issued for a specific API resource, acquired on behalf of a user via OBO flow. Contains the user's identity and the granted scopes. Expires after ~1 hour.                  | BFF exchanges the user's Entra ID token for a delegated access token scoped to WeatherApi (`api://[api-client-id]`).                     | Resource Access Token, Downstream Access Token, App-Only Token      |
| **Resource Access Token** | A generic OAuth term for an access token scoped to a specific API/resource (as opposed to an ID token or refresh token). Validates what API the token can be used with via the `aud` claim. | The token the BFF sends to WeatherApi is a resource access token—it's scoped only to that API, not usable for Microsoft Graph.             | Delegated Access Token, Audience, Scope                             |
| **App-Only Access Token** | An access token issued for application-to-application communication without user involvement. Uses client credentials flow instead of OBO. No user identity in the token.                      | Not used in demo5. Example: a background service calling WeatherApi without user context would use app-only flow.                          | Client Credentials Flow, Service-to-Service, Confidential Client    |

### Advanced Authentication Concepts

| Term                    | Definition                                                                                                                                                                              | Example in Demo5                                                                                                                           | Why It Matters                                                       |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------- |
| **Token Caching**       | Storing acquired tokens in memory or distributed cache to avoid repeated token acquisition requests. Improves performance and reduces authentication server load.                         | `AddInMemoryTokenCaches()` caches tokens so the BFF doesn't ask Entra ID for a new token on every WeatherApi call.                        | Performance, reduced latency, fewer auth requests                   |
| **Token Refresh**       | Automatically exchanging an expired or expiring refresh token for a new access token without user interaction. Enables long-running sessions.                                            | When the BFF's token for WeatherApi expires (1 hour), `IDownstreamApi` automatically refreshes it using the refresh token.                | Seamless user experience, maintains authenticated sessions            |
| **Confidential Client** | An application that can securely store client secrets (e.g., server-side apps). Allowed to use grant flows requiring credentials like authorization code or OBO.                         | The Blazor BFF is a confidential client—it runs on a server and can safely store the client secret for Entra ID.                           | Security, enables OBO flow, higher trust from identity provider      |
| **Public Client**       | An application that cannot securely store secrets (e.g., browser-based WASM, mobile apps). Uses PKCE (Proof Key for Code Exchange) for security.                                       | Blazor WebAssembly is a public client—tokens stored in browser cannot be protected, so direct API calls are less secure than BFF pattern.  | Mobile/browser apps, implicit trust model                           |
| **Claims Transformation** | A middleware/handler that extracts claims from a token and enriches them with application-specific data (e.g., role, permission mappings). Runs on each request.                        | `PermissionClaimsTransformation` reads user roles from the database and adds `permission` claims for authorization decisions.              | RBAC mapping, fine-grained authorization, decouples auth from policy |
| **Token Validation**    | The process of cryptographically verifying a token's signature, expiration, audience, and issuer. Ensures the token was issued by a trusted authority and hasn't been tampered with.   | WeatherApi validates incoming Bearer tokens: checks signature (valid issuer), expiration, and audience (`api://[client-id]`).             | Security, prevents token spoofing, ensures token integrity           |
| **CORS (Cross-Origin Resource Sharing)** | A browser security mechanism allowing/restricting HTTP requests from one origin to another. Controlled by server-side headers. Server-to-server calls bypass CORS.                     | WeatherApi has NO CORS configured because it's called server-to-server (BFF → API), not from browser. CORS only applies to browser requests. | Frontend security, prevents unauthorized browser-based API calls     |
| **BFF Pattern (Backend for Frontend)** | Architecture where a server-side backend handles authentication, token management, and serves tailored APIs to a frontend client. Frontend never holds tokens. Recommended for security. | Demo5's architecture: Browser → BFF (port 7210, holds tokens) → WeatherApi (port 7220, validates tokens). Browser doesn't see tokens.     | Security (XSS protection), centralized auth, token confidentiality   |

### Demo5 Architecture in Glossary Terms

Demo5 demonstrates calling **two downstream APIs**:
1. **Microsoft Graph** (external, SaaS, private) - Private SaaS service from Microsoft
2. **Demo5.DownstreamApi.WeatherApi** (internal, private) - Your own weather API

Both are **private APIs** (authentication required), but their **ownership** differs:
- **Internal ownership**: WeatherApi (you build/operate it)
- **External ownership**: Microsoft Graph (Microsoft builds/operates it)

## Prerequisites

- demo4 completed and working
- .NET 10 SDK installed
- EF Core tools installed
- Two Entra ID app registrations:
  1. Blazor Web App (client) - from demo4
  2. Protected API (new) - needs to be created
- Understanding of Bearer tokens and OBO (On-Behalf-Of) flow

## Entra ID Configuration

### Step 1: Create API App Registration

1. Navigate to Azure Portal → Entra ID → App Registrations
2. Click "New registration"
3. Name: "Demo5 Protected API"
4. Supported account types: Single tenant
5. No redirect URI needed for API
6. Click "Register"
7. Note the **Application (client) ID** and **Directory (tenant) ID**

### Step 2: Expose an API

1. In the API app registration, go to "Expose an API"
2. Click "Add" next to Application ID URI
3. Accept the default: `api://[your-api-client-id]`
4. Click "Add a scope"
   - Scope name: `Forecast.Read`
   - Admin consent display name: "Read weather forecast data"
   - Admin consent description: "Allows the app to read weather forecast data on behalf of the user"
   - State: Enabled
5. Click "Add scope"

### Step 3: Grant API Permissions to Client App

1. Navigate to your Blazor app registration (from demo4)
2. Go to "API permissions"
3. Click "Add a permission"
4. Select "My APIs" tab
5. Select "Demo5 Protected API"
6. Select delegated permission: `Forecast.Read`
7. Click "Add permissions"
8. Click "Grant admin consent" (if you're an admin)

### Step 4: Update Configuration Files

**Main App** (`Demo5.DownstreamApi/appsettings.json`):
```json
"WeatherApi": {
  "BaseUrl": "https://localhost:7220",
  "Scopes": [ "api://[API-CLIENT-ID]/Forecast.Read" ]
}
```

**API Project** (`Demo5.DownstreamApi.WeatherApi/appsettings.json`):
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "[YOUR-TENANT-ID]",
  "ClientId": "[API-CLIENT-ID]",
  "Audience": "api://[API-CLIENT-ID]"
}
```

Replace placeholders with actual values from Azure Portal.

## How to Run

### Step 1: Apply Database Migrations (reuses demo4 database)

```powershell
cd demo5/Demo5.DownstreamApi
dotnet ef database update
```

### Step 2: Start the Protected API

Open a terminal:
```powershell
cd demo5/Demo5.DownstreamApi.WeatherApi
dotnet watch
```

Expected output: API running on `https://localhost:7220`

### Step 3: Start the Blazor App

Open another terminal:
```powershell
cd demo5/Demo5.DownstreamApi
dotnet watch
```

Expected output: App running on `https://localhost:7210`

### Step 4: Test the Implementation

1. Navigate to `https://localhost:7210`
2. Sign in with passkey or Entra ID
3. Visit `/api-comparison` page
4. Observe side-by-side comparison:
   - **Left side**: BFF pattern calling `/api/weather` with cookies
   - **Right side**: Downstream pattern calling `https://localhost:7220/weather` with Bearer tokens
5. Check browser DevTools Network tab to see the different authentication methods

## What's New

### New Project: Demo5.DownstreamApi.WeatherApi

- Standalone ASP.NET Core Minimal API running on port 7220
- Configured with `AddMicrosoftIdentityWebApi` for Bearer token validation
- Validates tokens issued by Entra ID
- Requires `Forecast.Read` scope for `/weather` endpoint
- CORS is intentionally NOT configured to enforce server-to-server calls only (no browser access)

### Entra ID Configuration

- **Exposed API**: Application ID URI `api://[client-id]`
- **Custom Scope**: `Forecast.Read` (delegated permission)
- **API Permissions**: Blazor app granted access to Protected API
- **OBO Flow**: Automatic token exchange handled by `IDownstreamApi`

### Client Implementation

- **IDownstreamApi Service**: Configured to call custom API with automatic OBO flow
- **Service Registration**: `AddDownstreamApi("WeatherApi", config)` in Program.cs
- **Token Management**: Microsoft.Identity.Web handles token acquisition, caching, and refresh

**Key Concepts:**
- `EnableTokenAcquisitionToCallDownstreamApi()` enables automatic token acquisition, caching, and refresh for OBO flow
- `AddDownstreamApi()` registers a named downstream API with specific scopes and base URL
- Multiple downstream APIs can be registered with different names (see ARCHITECTURE_DEEP_DIVE.md for examples)

### New Components

- **DownstreamWeatherFetcher.razor**: Calls downstream API using `IDownstreamApi`, demonstrates Bearer token authentication
- **ApiArchitectureComparison.razor**: Side-by-side comparison page showing both BFF and Downstream patterns
- **Updated WeatherDataFetcher.razor**: Added explanatory note about BFF pattern

### Architecture Comparison

| Aspect             | BFF (Cookie-based)              | Downstream (Token-based)                  |
| ------------------ | ------------------------------- | ----------------------------------------- |
| **Endpoint**       | `/api/weather` (local)          | `https://localhost:7220/weather` (remote) |
| **Authentication** | Cookie                          | Bearer Token (JWT)                        |
| **Trust Model**    | Implicit (same origin)          | Explicit (token validation)               |
| **CORS**           | Not required                    | Not required (server-to-server)           |
| **Token Exposure** | None (server-side only)         | Managed by Microsoft.Identity.Web         |
| **Use Case**       | Monolithic apps, tight coupling | Microservices, distributed systems        |
| **Network Hops**   | 1 (client → server)             | 2 (client → server → API)                 |
| **Scalability**    | Limited to monolith             | Independent scaling                       |

**Hosting Note:** Demo5 runs the Protected API as a separate process (port 7220) to demonstrate distributed architecture. For guidance on co-hosting APIs within the same application, see the [API Hosting Architecture](.docs/issues/ARCHITECTURE_DEEP_DIVE.md#3-api-hosting-architecture-decision) section of the architecture guide.

### OAuth Scope vs RBAC Permission

Demo5 uses two distinct authorization layers that are **intentionally separate**:

| Layer                      | Purpose                          | Example         | Enforced By                        |
| -------------------------- | -------------------------------- | --------------- | ---------------------------------- |
| **OAuth Scope** (Entra ID) | API access consent               | `Forecast.Read` | WeatherApi JWT validation          |
| **RBAC Permission** (BFF)  | Business operation authorization | `weather.read`  | BFF PermissionAuthorizationHandler |

**Why are they different?**
- **OAuth scopes** (`Forecast.Read`) authorize the *application* to access an API on behalf of a user
- **RBAC permissions** (`weather.read`) authorize specific *operations* based on user roles

**Authorization flow:**
```
User Request → [weather.read RBAC check] → BFF → [Forecast.Read OAuth scope] → WeatherApi
```

**Benefits of separation:**
- Can map multiple BFF permissions to one OAuth scope
- Internal permission names can change without updating Entra ID
- Business domain language in BFF, API contract language in OAuth

### Downstream API Naming Convention

Demo5 registers two downstream APIs with descriptive names:

| Configuration Key | Type            | Purpose                                   |
| ----------------- | --------------- | ----------------------------------------- |
| `MicrosoftGraph`  | External SaaS   | Microsoft Graph API for user profile data |
| `WeatherApi`      | Internal Domain | Protected weather forecast API            |

**Naming Guidelines:**
- **External APIs**: Use vendor/service name (e.g., `MicrosoftGraph`, `AzureStorage`, `Stripe`)
- **Internal APIs**: Use domain/feature name (e.g., `WeatherApi`, `OrdersApi`, `InventoryApi`)

This naming convention provides:
- Clear distinction between owned and third-party services
- Self-documenting configuration
- Easier troubleshooting and monitoring

### Security Considerations

- **OBO Flow**: User's identity propagates from Blazor app through to Protected API
- **Scope Validation**: API validates that incoming tokens contain the required `Forecast.Read` scope
- **Token Lifetime**: Tokens are cached and automatically refreshed by Microsoft.Identity.Web
- **HTTPS Only**: Both services enforce HTTPS in production
- **Token Flow**: For detailed token acquisition, caching, and refresh lifecycle, see [OBO Token Lifecycle](.docs/issues/ARCHITECTURE_DEEP_DIVE.md#6-obo-token-lifecycle--flow) in the architecture guide

### Key Learning Points

1. **When to use BFF**: Simple apps, tightly coupled frontend-backend, no token exposure to client
2. **When to use Downstream**: Microservices architecture, independent scaling, reusable APIs
3. **OBO Flow**: Enables secure identity propagation across service boundaries
4. **Scope-based Authorization**: More granular than role-based, better for API-to-API calls

### Troubleshooting

**401 Unauthorized from API:**
- Verify API app registration client ID in appsettings.json
- Check that admin consent was granted for API permissions
- Ensure user is signed in with Entra ID (not passkey)

**403 Forbidden from API:**
- Verify `Forecast.Read` scope is included in token
- Check token claims in https://jwt.ms
- Ensure API permissions were granted in Entra ID

**Token not acquired:**
- Verify `EnableTokenAcquisitionToCallDownstreamApi` is configured in main app
- Check that DownstreamApi configuration has correct BaseUrl and Scopes
- Ensure Entra ID user has consented to permissions

### Next Steps

- Experiment with adding more downstream API endpoints
- Try adding a second downstream API service
- Explore app-only authentication (client credentials flow) for daemon scenarios
- Compare performance between BFF and Downstream patterns under load