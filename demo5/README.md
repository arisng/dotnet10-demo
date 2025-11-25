# Demo5: Downstream API Integration

## Goal
Create a standalone protected API service and consume it from the Blazor app using Entra ID tokens, contrasting the BFF (Cookie) vs. Downstream (Token) architectures.

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
"DownstreamApi": {
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
- CORS configured to allow requests from `https://localhost:7210`

### Entra ID Configuration
- **Exposed API**: Application ID URI `api://[client-id]`
- **Custom Scope**: `Forecast.Read` (delegated permission)
- **API Permissions**: Blazor app granted access to Protected API
- **OBO Flow**: Automatic token exchange handled by `IDownstreamApi`

### Client Implementation
- **IDownstreamApi Service**: Configured to call custom API with automatic OBO flow
- **Service Registration**: `AddDownstreamApi("ProtectedApi", config)` in Program.cs
- **Token Management**: Microsoft.Identity.Web handles token acquisition, caching, and refresh

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
| **CORS**           | Not required                    | Required                                  |
| **Token Exposure** | None (server-side only)         | Managed by Microsoft.Identity.Web         |
| **Use Case**       | Monolithic apps, tight coupling | Microservices, distributed systems        |
| **Network Hops**   | 1 (client → server)             | 2 (client → server → API)                 |
| **Scalability**    | Limited to monolith             | Independent scaling                       |

### Security Considerations
- **OBO Flow**: User's identity propagates from Blazor app through to Protected API
- **Scope Validation**: API validates that incoming tokens contain the required `Forecast.Read` scope
- **Token Lifetime**: Tokens are cached and automatically refreshed by Microsoft.Identity.Web
- **HTTPS Only**: Both services enforce HTTPS in production

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

**CORS errors:**
- Verify CORS policy in `Demo5.ProtectedApi/Program.cs` includes `https://localhost:7210`
- Check that CORS middleware is added before authorization

**Token not acquired:**
- Verify `EnableTokenAcquisitionToCallDownstreamApi` is configured in main app
- Check that DownstreamApi configuration has correct BaseUrl and Scopes
- Ensure Entra ID user has consented to permissions

### Next Steps
- Experiment with adding more downstream API endpoints
- Try adding a second downstream API service
- Explore app-only authentication (client credentials flow) for daemon scenarios
- Compare performance between BFF and Downstream patterns under load