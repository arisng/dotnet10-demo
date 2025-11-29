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

**API Project** (`Demo5.ProtectedApi/appsettings.json`):
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
cd demo5/Demo5.ProtectedApi
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

### New Project: Demo5.ProtectedApi

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