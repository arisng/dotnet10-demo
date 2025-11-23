# Demo4 – Microsoft Entra ID Integration

## Goal

Add Microsoft Entra ID as an external identity provider alongside local passkey authentication, supporting a hybrid identity scenario where B2C customers use passkeys while employees authenticate via Entra ID. Demonstrate how the On-Behalf-Of (OBO) flow enables server-side Microsoft Graph API calls while preserving the existing permission-based authorization system.

## Prerequisites

- **Completed:** demo3 (BFF APIs + Permission-Based RBAC)
- **.NET 10 SDK** (Preview) with EF Core tools installed
- **Azure Entra ID Tenant** with permissions to register applications
- **Microsoft Graph permissions** to read user profile data
- VS Code or JetBrains Rider

## Architecture Changes

Demo4 transforms the monolithic Blazor Web App to support **dual authentication sources** while maintaining unified authorization:

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Web App                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Authentication Layer (Hybrid)                      │   │
│  │  • Local Identity (Passkeys) ──┐                    │   │
│  │  • Microsoft Entra ID ─────────┼─→ Claims Principal │   │
│  └────────────────────────────────┴──────────────────────┘   │
│                         ↓                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Authorization Layer (Unified)                       │   │
│  │  • IClaimsTransformation                             │   │
│  │  • Permission-Based Policies (from demo3)            │   │
│  └──────────────────────────────────────────────────────┘   │
│                         ↓                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  BFF APIs                                            │   │
│  │  /api/weather, /api/users, /api/reports             │   │
│  │  (Cookie-based, no bearer tokens)                   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                         ↓
            ┌────────────────────────┐
            │  Microsoft Graph API   │
            │  (OBO Flow)            │
            │  • User profile        │
            │  • Photo               │
            └────────────────────────┘
```

**Key Architectural Decisions:**

1. **Cookie Authentication for BFF APIs:** Both local and Entra users authenticate via cookies. No bearer tokens sent to BFF endpoints.
2. **OBO Flow for Graph API:** Server-side code exchanges the Entra token for a downstream token to call Microsoft Graph.
3. **Unified Authorization:** Both identity sources flow through the same `IClaimsTransformation` → permission system from demo3.
4. **State Serialization:** `AddAuthenticationStateSerialization()` passes Entra identity to WASM client without exposing access tokens.

## What's New

### 1. Microsoft Entra ID Authentication

- **Package:** `Microsoft.Identity.Web` (v4.1.0) and `Microsoft.Identity.Web.DownstreamApi`
- **Configuration:** `AddMicrosoftIdentityWebApp()` with OpenID Connect
- **Login Flow:** "Sign in with Microsoft" button alongside passkey/password options
- **Claims Mapping:** Map Entra ID claims (`oid`, `preferred_username`, `name`) to `ApplicationUser`

### 2. Microsoft Graph Integration (OBO Flow)

- **Service:** `IDownstreamApi` to call Microsoft Graph server-side
- **Endpoints:**
  - `/me` – Fetch user profile (displayName, jobTitle, mail)
  - `/me/photo/$value` – Fetch user profile photo
- **Scopes Required:** `User.Read`
- **Pattern:** Server fetches Graph data on behalf of the authenticated Entra user

### 3. Secure State Serialization

- **Purpose:** Pass Entra identity to WASM client without exposing access tokens
- **Implementation:** `AddAuthenticationStateSerialization()` in `Program.cs`
- **Outcome:** WASM components see the same `ClaimsPrincipal` as server components

### 4. Hybrid Identity Data Model

**Extended `ApplicationUser`:**
```csharp
public string? ExternalAuthenticationProvider { get; set; } // "Entra" or null
public string? EntraObjectId { get; set; } // Entra "oid" claim
public string? DisplayName { get; set; } // Synced from Graph API
public string? JobTitle { get; set; } // Synced from Graph API
```

**Account Linking Strategy:**
- If an Entra user signs in with email matching a local user, prompt for account linking (future enhancement)
- For now, treat them as separate accounts to demonstrate dual authentication sources

### 5. Enhanced Diagnostics

**Updated `AuthStateProbe.razor`:**
- Display authentication provider: "Local (Passkey)" vs. "Microsoft Entra ID"
- Show Entra-specific claims: `oid`, `tid`, `preferred_username`
- Visualize Graph API data: displayName, jobTitle, profile photo
- Permission claims remain identical regardless of authentication source

### 6. BFF API Behavior (Unchanged)

- All BFF endpoints (`/api/weather`, `/api/users`, `/api/reports`) continue using cookie authentication
- Both local and Entra users access the same APIs with the same permission requirements
- No bearer token validation in BFF layer (deferred to demo5 for downstream API pattern)

## Azure Entra ID Setup

### 1. Register Application in Entra Portal

1. Navigate to **Azure Portal** → **Microsoft Entra ID** → **App registrations** → **New registration**
2. **Name:** `Demo4.EntraIntegration`
3. **Supported account types:** "Accounts in this organizational directory only"
4. **Redirect URI:** 
   - Platform: Web
   - URI: `https://localhost:7210/signin-oidc`
5. Click **Register**

### 2. Configure Authentication

1. Under **Authentication**, add additional redirect URI:
   - `https://localhost:7210/signout-callback-oidc`
2. Enable **ID tokens** checkbox
3. Save

### 3. Configure API Permissions

1. Under **API permissions**, add:
   - `Microsoft Graph` → `Delegated permissions` → `User.Read` (enabled by default)
2. Grant admin consent for your tenant (if required)

### 4. Create Client Secret

1. Under **Certificates & secrets**, click **New client secret**
2. Description: `Demo4 Dev Secret`
3. Expires: 6 months (or per policy)
4. **Copy the secret value immediately** (only shown once)

### 5. Note Configuration Values

From the **Overview** page, copy:
- **Application (client) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Directory (tenant) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

## Configuration

### Update `appsettings.Development.json`

Add the Entra ID configuration section:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "ClientSecret": "YOUR-CLIENT-SECRET",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  }
}
```

**Security Note:** For production, move `ClientSecret` to Azure Key Vault or User Secrets. For local development:

```powershell
cd demo4/Demo4.EntraIntegration
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR-CLIENT-SECRET"
```

## How to Run

### 1. Apply Database Migrations (if needed)

```powershell
cd demo4/Demo4.EntraIntegration/Demo4.EntraIntegration
dotnet ef database update
```

The database schema from demo3 already includes roles, permissions, and seeded users. Demo4 adds columns to `ApplicationUser` for Entra integration.

### 2. Run the Application

```powershell
dotnet watch
```

The app launches at:
- **HTTPS:** `https://localhost:7210`
- **HTTP:** `http://localhost:5210`

### 3. Test Local Authentication (Baseline)

1. Navigate to `https://localhost:7210`
2. Click **Register** → Create account with passkey
3. Sign in with passkey
4. Navigate to `/auth-state-probe` → Verify authentication provider: "Local (Passkey)"
5. Test BFF APIs: `/weather`, `/users`, `/reports`

### 4. Test Entra ID Authentication

1. Sign out from local account
2. Click **Sign in with Microsoft**
3. Authenticate with your Entra ID account
4. **First Sign-In:** App creates a new `ApplicationUser` record, mapping Entra claims
5. Navigate to `/auth-state-probe`:
   - Authentication provider: "Microsoft Entra ID"
   - Entra claims: `oid`, `preferred_username`, `name`
   - Graph data: displayName, jobTitle, profile photo
6. Test BFF APIs with the same endpoints (permission assignment required)

### 5. Assign Permissions to Entra User

**Option A: Manual Database Seeding (Quick Test)**

```sql
-- Find Entra user ID
SELECT Id, UserName, Email, ExternalAuthenticationProvider 
FROM AspNetUsers 
WHERE ExternalAuthenticationProvider = 'Entra';

-- Assign "Admin" role to Entra user
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 'ENTRA-USER-ID', Id FROM AspNetRoles WHERE Name = 'Admin';
```

**Option B: Admin UI (Future Enhancement)**

In demo6, we'll implement automatic role mapping from Entra App Roles.

### 6. Verify Unified Authorization

1. Sign in with Entra ID account (assigned Admin role)
2. Navigate to `/auth-state-probe` → Confirm permission claims appear
3. Test protected APIs:
   - `/api/weather` (GET/POST) – Requires `weather.read`/`weather.write`
   - `/api/users` (GET/DELETE) – Requires `users.read`/`users.delete`
   - `/api/reports` (GET/export) – Requires `reports.view`/`reports.export`
4. Both local passkey admins and Entra admins have identical API access

## Key Implementation Details

### Program.cs Changes

```csharp
using Microsoft.Identity.Web;

// Add Entra ID authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();

// Serialize auth state for WASM
builder.Services.AddAuthenticationStateSerialization();
```

### Claims Transformation Enhancement

**Updated `PermissionClaimsTransformation.cs`:**

```csharp
public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
{
    var identity = (ClaimsIdentity)principal.Identity!;
    
    // Detect authentication source
    var isEntraUser = principal.HasClaim(c => c.Type == "oid");
    
    if (isEntraUser)
    {
        // Load user by Entra Object ID
        var oid = principal.FindFirstValue("oid");
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        
        // If first login, create user record
        if (user == null)
        {
            user = await CreateEntraUserAsync(principal);
        }
    }
    else
    {
        // Existing local user logic
        var userId = _userManager.GetUserId(principal);
        // ... load permissions as before
    }
    
    // Load roles → permissions (unified for both sources)
    var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
    foreach (var permission in permissions)
    {
        identity.AddClaim(new Claim("permission", permission));
    }
    
    return principal;
}
```

### Microsoft Graph Service

**New: `IGraphService` (server-side)**

```csharp
public interface IGraphService
{
    Task<UserProfile?> GetUserProfileAsync();
    Task<byte[]?> GetUserPhotoAsync();
}

public class GraphService : IGraphService
{
    private readonly IDownstreamApi _downstreamApi;
    
    public async Task<UserProfile?> GetUserProfileAsync()
    {
        return await _downstreamApi.GetForUserAsync<UserProfile>(
            "DownstreamApi", 
            options => options.RelativePath = "me");
    }
    
    public async Task<byte[]?> GetUserPhotoAsync()
    {
        // Call /me/photo/$value
        // Returns image bytes
    }
}
```

## Troubleshooting

### "AADSTS50011: The reply URL does not match"

- Verify redirect URIs in Entra app registration match exactly: `https://localhost:7210/signin-oidc`
- Check for typos in `appsettings.Development.json` → `AzureAd:CallbackPath`

### "AADSTS65001: The user or administrator has not consented"

- Navigate to **API permissions** in Entra portal
- Click **Grant admin consent for [Tenant]**
- Refresh browser and retry sign-in

### Entra User Has No Permissions

- Entra users start with **no roles** until explicitly assigned
- Manually assign roles via SQL (see "How to Run" section)
- In demo6, we'll automate this via Entra App Roles mapping

### Graph API Returns 401 Unauthorized

- Verify `User.Read` scope is granted in Entra portal
- Check `DownstreamApi:Scopes` in `appsettings.Development.json`
- Ensure `EnableTokenAcquisitionToCallDownstreamApi()` is called in `Program.cs`

## Observability

Demo4 inherits .NET 10 authorization metrics from demo3:

- `aspnetcore.authorization.request_duration` (histogram)
- `aspnetcore.authorization.success` (counter)
- `aspnetcore.authorization.failure` (counter)

**New Telemetry:**
- Sign-in events: Track authentication provider (Local vs. Entra)
- Graph API call latency and failures
- Token acquisition metrics from Microsoft.Identity.Web

## What's Next?

**Demo5** introduces a separate downstream API service and contrasts two security patterns:

1. **BFF Pattern (Cookie):** Current `/api/weather`, `/api/users`, `/api/reports` endpoints
2. **Downstream API Pattern (Bearer Token):** New `Demo5.ProtectedApi` project secured with Entra ID access tokens

You'll implement the On-Behalf-Of (OBO) flow to call a custom protected API from the Blazor app, demonstrating microservice authentication.

---

**Demo4 Checkpoint:** You now have a production-grade hybrid authentication system where local passkey users and Entra ID employees share the same permission-based authorization infrastructure. The BFF security model ensures cookie-based authentication while the OBO flow enables server-side Graph API calls.
