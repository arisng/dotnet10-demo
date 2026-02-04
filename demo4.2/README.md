# Demo 4.2: DProcess IdP + BFF + API (OpenIddict + Entra)

[[Home](../README.md) > **Demo 4.2**]

## Goal
Implement a dedicated Identity Provider using OpenIddict + ASP.NET Core Identity with Entra external login, then secure a Blazor BFF and API with permission-based RBAC in an Aspire-orchestrated setup.

## Patterns Selected (Catalog)
Identity federation, permission-based authorization, and BFF proxy patterns introduced in this demo.

| Pattern                                                                                                     | Why Here                                                           | Evidence                                                                                                     |
| ----------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------ |
| [auth-multi-identity](../.docs/reference/patterns/catalog/auth-multi-identity.md)                           | Combines local Identity with external Entra login in the IdP       | [src/DProcess.Idp/DProcess.Idp/Program.cs](src/DProcess.Idp/DProcess.Idp/Program.cs)                         |
| [auth-oidc-external-provider](../.docs/reference/patterns/catalog/auth-oidc-external-provider.md)           | Federates Entra ID via OIDC in the IdP                             | [src/DProcess.Idp/DProcess.Idp/Program.cs](src/DProcess.Idp/DProcess.Idp/Program.cs)                         |
| [authz-permission-rbac](../.docs/reference/patterns/catalog/authz-permission-rbac.md)                       | Role → permission model drives unified RBAC                        | [src/DProcess.Idp/DProcess.Idp/Data/RolePermission.cs](src/DProcess.Idp/DProcess.Idp/Data/RolePermission.cs) |
| [authz-claims-mapping](../.docs/reference/patterns/catalog/authz-claims-mapping.md)                         | Maps `permission` claims from UserInfo into the BFF auth principal | [src/DProcess.Bff/DProcess.Bff/Program.cs](src/DProcess.Bff/DProcess.Bff/Program.cs)                         |
| [api-bff](../.docs/reference/patterns/catalog/api-bff.md)                                                   | BFF owns authentication and forwards API calls                     | [src/DProcess.Bff/DProcess.Bff/Program.cs](src/DProcess.Bff/DProcess.Bff/Program.cs)                         |
| [api-yarp-reverse-proxy](../.docs/reference/patterns/catalog/api-yarp-reverse-proxy.md)                     | YARP routes `/api/*` to the API service                            | [src/DProcess.Bff/DProcess.Bff/Program.cs](src/DProcess.Bff/DProcess.Bff/Program.cs)                         |
| [dist-dotnet-aspire-orchestration](../.docs/reference/patterns/catalog/dist-dotnet-aspire-orchestration.md) | Aspire coordinates IdP, BFF, and API projects                      | [src/DProcess.AppHost/AppHost.cs](src/DProcess.AppHost/AppHost.cs)                                           |

## Tech Stack
Key technologies used to implement the IdP, BFF, and API flow.

- **[.NET 10.0 SDK (10.0.0)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0):** Base runtime for all projects.
- **[ASP.NET Core (10.0.0)](https://learn.microsoft.com/en-us/aspnet/core/):** Hosts IdP, BFF, and API web apps.
- **[Blazor WebAssembly (10.0.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/):** Client UI for the BFF (WASM). IdP uses **InteractiveServer-only** mode.
- **[Blazor WebAssembly (10.0.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/):** Client UI for the BFF (WASM). IdP uses **InteractiveServer-only** mode.
- **[Entity Framework Core (10.0.0)](https://learn.microsoft.com/en-us/ef/core/):** Identity + RBAC persistence in the IdP.
- **[OpenIddict (4.0.0)](https://documentation.openiddict.com/):** Local authorization server for OIDC/OAuth.
- **[YARP (2.3.0)](https://microsoft.github.io/reverse-proxy/):** Reverse proxy routing for `/api/*`.
- **[Microsoft.Identity.Web (4.3.0)](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web):** Entra token validation in the API.
- **[.NET Aspire (13.1.0)](https://learn.microsoft.com/en-us/dotnet/aspire/):** Distributed app orchestration.

## Research & Documentation
Links to demo-specific research and architectural decisions.

- **Research Findings:**
  - [.docs/reference/research-01-openiddict-identity-passkeys.md](.docs/reference/research-01-openiddict-identity-passkeys.md)
  - [.docs/reference/research-02-entra-external-oidc.md](.docs/reference/research-02-entra-external-oidc.md)
  - [.docs/reference/research-03-blazor-bff-yarp-oidc.md](.docs/reference/research-03-blazor-bff-yarp-oidc.md)
  - [.docs/reference/research-04-openiddict-claim-destinations.md](.docs/reference/research-04-openiddict-claim-destinations.md)
  - [.docs/reference/research-05-api-jwt-validation.md](.docs/reference/research-05-api-jwt-validation.md)
  - [.docs/reference/research-06-obo-flow.md](.docs/reference/research-06-obo-flow.md)
- **Implementation Plan:** [.docs/reference/plan.md](.docs/reference/plan.md)
- **ADRs:** None yet.

## Architecture & Decisions
Technical overview of the IdP + BFF + API flow.

### Diagram
```
Browser
  ↓ (OIDC)
DProcess.Bff (Blazor WASM + cookie auth)
DProcess.Bff (Blazor WASM + cookie auth)
  ↓ (Authorize/Token/UserInfo)
DProcess.Idp (OpenIddict + Identity + Entra external) [InteractiveServer-only]
DProcess.Idp (OpenIddict + Identity + Entra external) [InteractiveServer-only]
  ↓ (Bearer token)
DProcess.Api (permission policies)
```

### Key Decisions
1. **Dedicated IdP:** Use OpenIddict + Identity as the local authority while enabling Entra external login for federation.
2. **Permission Claims in UserInfo:** Emit and map `permission` claims into the BFF principal to drive RBAC in UI and API.
3. **BFF + YARP:** Proxy `/api/*` through the BFF to keep access tokens server-side.
4. **Dual Token Validation:** API supports both local OpenIddict and Entra ID tokens via dynamic issuer-based scheme selection.
5. **HTTP 302 Auto-Redirect Pattern:** IdP authorization endpoint uses manual authentication check (not `[Authorize]` attribute) to return HTTP 302 redirect for unauthenticated users. This enables proper browser auto-navigation to login page while preserving OIDC parameters, unlike HTTP 401 which browsers do not automatically follow.

### Configuration Architecture

This demo supports two runtime modes with different configuration source priorities:

#### Aspire Orchestrated Mode (Production Approach)
When running via AppHost (`dotnet run --project src/DProcess.AppHost`):

**Configuration Priority:**
1. **AppHost environment variables** (Highest Priority) - Defined in [src/DProcess.AppHost/AppHost.cs](src/DProcess.AppHost/AppHost.cs)
   - IDP Authority: `https://localhost:7046`
   - API Destination: `https://localhost:7142/`
   - Service discovery endpoints via Aspire references
2. **Launch Settings** (Port Definitions) - Each project's `Properties/launchSettings.json`
   - Used by AppHost to determine actual service ports
3. **appsettings.Development.json** (Overridden) - Contains defaults but overridden by AppHost

**AppHost Configuration Example:**
```csharp
var api = builder.AddProject<Projects.DProcess_Api>("api")
    .WithEnvironment("Idp__Authority", "https://localhost:7046")
    .WithEnvironment("Idp__Issuer", "https://localhost:7046");

builder.AddProject<Projects.DProcess_Bff>("bff")
    .WithReference(api)
    .WithEnvironment("Idp__Authority", "https://localhost:7046")
    .WithEnvironment("ReverseProxy__Clusters__api__Destinations__d1__Address", "https://localhost:7142/");
```

#### Standalone Mode (Development/Testing)
When running projects individually (`dotnet run --project src/DProcess.Bff/DProcess.Bff`):

**Configuration Priority:**
1. **Launch Settings** (Highest Priority) - Each project's `Properties/launchSettings.json`
2. **appsettings.Development.json** (Configuration) - Provides IDP Authority and API destinations

**Note:** Current appsettings.Development.json files contain ports that differ from launch settings. Standalone mode may require manual alignment.

#### Project Ports

| Project     | HTTPS Port | HTTP Port | Purpose                        |
| ----------- | ---------- | --------- | ------------------------------ |
| **AppHost** | 17102      | 15219     | Aspire dashboard launcher      |
| **IDP**     | 7046       | 5187      | Identity Provider (OpenIddict) |
| **API**     | 7142       | 5199      | Backend API                    |
| **BFF**     | 7092       | 5017      | Frontend + YARP Proxy          |

**Verifying Actual Endpoints:**
- Run AppHost: `dotnet run --project src/DProcess.AppHost --launch-profile https`
- Access Aspire dashboard: `https://localhost:17102`
- Navigate to **Resources** tab to view actual service endpoints
- Check **Console Logs** tab for startup binding confirmation

### Authentication Flows

#### Local Identity (OpenIddict) Flow

**OpenIddict Client Configuration:**
- **Client ID:** `bff`
- **Client Secret:** `bff-secret`
- **Grant Type:** Authorization Code with PKCE
- **Scopes:** `openid`, `profile`, `email`, `offline_access`, `api`
- **Redirect URIs:** Configured in [src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs](src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs)
- **Seeding:** Automatic on IDP startup via `IHostedService`

**Step-by-Step Flow:**

1. **Initial Navigation:**
   - User navigates to BFF (`https://localhost:7092`)
   - Unauthenticated → redirect to `/login`

2. **OIDC Challenge:**
   - BFF initiates OpenIdConnect challenge
   - Redirects to IDP `/connect/authorize` with PKCE challenge

3. **Auto-Redirect to Login (Unauthenticated):**
   - IDP detects unauthenticated user
   - **Returns HTTP 302 redirect** to `/Account/Login?ReturnUrl=%2Fconnect%2Fauthorize%3F...`
   - Browser automatically follows the redirect (preserving all OIDC parameters in ReturnUrl)
   - User sees IDP login page

4. **User Authentication:**
   - User logs in at IDP (`https://localhost:7046`) with credentials or passkey
   - IDP validates credentials against ASP.NET Core Identity database
   - IDP redirects back to `/connect/authorize` (using ReturnUrl)
   - Now authenticated, IDP issues authorization code

5. **Token Exchange:**
   - BFF exchanges code for tokens at IDP `/connect/token`
   - PKCE code verifier validated
   - IDP returns: `access_token`, `refresh_token`, `id_token`

6. **User Info Retrieval:**
   - BFF calls IDP `/connect/userinfo` with access token
   - IDP returns claims including `permission` claims from RBAC system

7. **Session Establishment:**
   - BFF stores tokens in encrypted cookie
   - BFF extracts and maps `permission` claims into ClaimsPrincipal
   - User session established with full permission context

8. **Account Management Access:**
   - BFF NavMenu provides authenticated users with a single **"Manage Account"** entry point
   - Link navigates to IdP `/Account/Manage` hub with `ReturnUrl` parameter
   - IdP NavMenu displays only identity-related navigation:
     - Profile management
     - Register/Login (for unauthenticated users)
     - Logout
   - IdP provides prominent **"Back to BFF"** navigation link that validates and preserves ReturnUrl
   - Seamless single-window navigation experience (no new tabs)
   - ReturnUrl validation ensures secure navigation between BFF and IdP boundaries

9. **Token Refresh:**
8. **Account Management Access:**
   - BFF NavMenu provides authenticated users with a single **"Manage Account"** entry point
   - Link navigates to IdP `/Account/Manage` hub with `ReturnUrl` parameter
   - IdP NavMenu displays only identity-related navigation:
     - Profile management
     - Register/Login (for unauthenticated users)
     - Logout
   - IdP provides prominent **"Back to BFF"** navigation link that validates and preserves ReturnUrl
   - Seamless single-window navigation experience (no new tabs)
   - ReturnUrl validation ensures secure navigation between BFF and IdP boundaries

9. **Token Refresh:**
   - BFF automatically refreshes access token when near expiration
   - Uses `refresh_token` to obtain new access token
   - Transparent to user

#### Logout Flow

1. **Initiate Logout:**
   - User hits BFF `/logout-oidc` (from the app UI)
   - BFF clears the local cookie and triggers OIDC sign-out
2. **IdP Sign-Out:**
   - IdP `/Account/Logout` signs out Identity locally
   - IdP redirects to the BFF signed-out landing page using `Bff:BaseUrl` + `Bff:SignedOutLocalPath`
3. **Signed-Out Landing:**
   - BFF `/signed-out` completes sign-out Identity locally and returns to home

#### Microsoft Entra ID (ME-ID) Flow

**External Provider Configuration:**
- **Provider Name:** "Entra" (displayed as "Sign in with Entra")
- **Authority:** `https://login.microsoftonline.com/{tenantId}/v2.0`
- **Callback Path:** `/signin-entra`
- **Sign-in Scheme:** `IdentityConstants.ExternalScheme`

**Step-by-Step Flow:**

1. **External Sign-In:**
   - User navigates to IDP and chooses "Sign in with Entra"
   - IDP redirects to Entra ID for authentication

2. **Entra Authentication:**
   - User authenticates with Microsoft credentials
   - Entra ID validates credentials and MFA (if configured)
   - Entra returns to IDP callback with authorization code

3. **Account Linking:**
   - IDP receives Entra claims via callback
   - IDP links Entra account to local Identity user (creates if new)
   - User completes local Identity sign-in flow

4. **Session Establishment:**
   - User continues through standard OpenIddict flow (steps 5-8 above)
   - Permissions are assigned based on linked local Identity user

**API Token Validation (Dual-Scheme):**
- API uses `BearerSelector` policy scheme to inspect JWT issuer
- **Local IDP issuer** (`https://localhost:7046`) → `LocalBearer` scheme (OpenIddict validation)
- **Entra issuer** → `Bearer` scheme (Microsoft.Identity.Web validation)
- Entra tokens require `access_as_user` scope

#### Permission Propagation

**RBAC Architecture:**
- **Source:** Role → Permission mappings in IDP database
- **Seeding:** [src/DProcess.Idp/DProcess.Idp/Data/DbSeeder.cs](src/DProcess.Idp/DProcess.Idp/Data/DbSeeder.cs)
- **Claim Type:** `permission` (custom claim)

**Propagation Flow:**
1. IDP queries role-permission mappings during authentication
2. IDP adds `permission` claims to UserInfo endpoint response
3. BFF extracts permissions in `OnUserInformationReceived` event
4. BFF adds permission claims to user's ClaimsPrincipal
5. BFF forwards access token to API via YARP proxy (Authorization header)
6. API validates token and enforces permission-based authorization policies

**Supported Permissions:**
- `weather.read`, `weather.write`
- `users.read`, `users.write`, `users.delete`
- `reports.view`, `reports.export`
- `Api.Access` (for Entra tokens)

### YARP Reverse Proxy Configuration

**BFF → API Proxying:**
- **Route:** `/api/*` → `api` cluster
- **Destination:** Aspire service discovery reference resolves to actual API endpoint at runtime
- **Fallback:** [src/DProcess.Bff/DProcess.Bff/appsettings.Development.json](src/DProcess.Bff/DProcess.Bff/appsettings.Development.json) cluster destination (overridden by AppHost)
- **Middleware:** Injects `Authorization: Bearer {access_token}` header automatically
- **Authorization:** Requires authenticated users

**Configuration in BFF Program.cs:**
- YARP services registered via `AddReverseProxy().LoadFromConfig()`
- Custom middleware adds access token from authenticated session
- Routes defined in appsettings `ReverseProxy:Routes:api`

## What's New
Changes from demo4.1.

- **Dedicated IdP project:** OpenIddict server + Identity UI hosted in `DProcess.Idp`.
- **IdP InteractiveServer-only mode:** IdP runs exclusively in InteractiveServer mode (no WASM client project), simplifying the identity provider architecture.
- **Refined Navigation UX:** 
  - BFF exposes single **"Manage Account"** entry point (not multiple IdP links)
  - IdP NavMenu contains only identity-related items (no BFF app pages)
  - Seamless ReturnUrl-based navigation between BFF and IdP (single window, no new tabs)
  - Clear "Back to BFF" navigation from IdP with ReturnUrl validation
- **IdP InteractiveServer-only mode:** IdP runs exclusively in InteractiveServer mode (no WASM client project), simplifying the identity provider architecture.
- **Refined Navigation UX:** 
  - BFF exposes single **"Manage Account"** entry point (not multiple IdP links)
  - IdP NavMenu contains only identity-related items (no BFF app pages)
  - Seamless ReturnUrl-based navigation between BFF and IdP (single window, no new tabs)
  - Clear "Back to BFF" navigation from IdP with ReturnUrl validation
- **Unified RBAC:** Permissions computed once in the IdP and flowed into BFF/API via claims.
- **Aspire wiring:** AppHost orchestrates IdP, BFF, and API projects.
- **HTTP 302 auto-redirect:** IdP uses manual authentication check to return proper browser redirects (not 401) for unauthenticated OIDC authorize requests.

## Getting Started
Instructions to run and verify the demo.

### 1. Prerequisites
- .NET 10.0 SDK installed.
- Update `Entra` settings (except ClientSecret) in [src/DProcess.Idp/DProcess.Idp/appsettings.Development.json](src/DProcess.Idp/DProcess.Idp/appsettings.Development.json) with your tenant details.
- Set the Entra ClientSecret using dotnet user-secrets:
```bash
cd src/DProcess.Idp/DProcess.Idp
dotnet user-secrets set "Entra:ClientSecret" "<YourClientSecret>"
```
- Default local users seeded: `admin@local.app` / `Admin123!`, `manager@local.app` / `Manager123!`, `user@local.app` / `User123!`.


### 2. Execution
```powershell
cd demo4.2/src
aspire run
```

### 3. Verification Steps
- [x] **Launch AppHost:** Open the Aspire dashboard and navigate to the BFF endpoint. - Expected: Home page loads.
- [x] **Authenticate:** Click **Login** and sign in with `admin@local.app`. - Expected: Redirect back to `/` with authenticated user state.
- [x] **Access Weather:** Navigate to `/weather`. - Expected: Weather data renders without authorization errors.

### Configuration Guidance

When working with this demo, keep in mind the two runtime modes and their configuration patterns:

#### Verifying Configuration

**Check Actual Endpoints (Aspire Dashboard):**
1. Run AppHost: `dotnet run --project src/DProcess.AppHost --launch-profile https`
2. Open Aspire dashboard: `https://localhost:17102`
3. Navigate to **Resources** tab
4. Verify endpoint URLs for each service

**Check Token Issuer (Browser DevTools):**
1. Authenticate at BFF
2. Open browser DevTools → Application → Cookies
3. Check `.AspNetCore.Cookies` → Decode JWT access_token (jwt.io)
4. Verify `iss` claim matches IDP Authority (`https://localhost:7046`)

**Check API Authorization (Browser DevTools):**
1. Navigate to `/weather` at BFF
2. Open browser DevTools → Network → Find `/api/weather` request
3. Check `Authorization` header contains `Bearer {token}`
4. API response should be 200 (not 401)

#### When to Update Configuration

| Scenario                          | Update Location                                                                                                                                                                                                              | Notes                                                          |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| **Change service ports**          | `Properties/launchSettings.json` in each project                                                                                                                                                                             | AppHost reads these for port assignments                       |
| **Change IDP Authority**          | [src/DProcess.AppHost/AppHost.cs](src/DProcess.AppHost/AppHost.cs)                                                                                                                                                           | Hardcoded override for Aspire mode                             |
| **Change BFF signed-out landing** | [src/DProcess.Bff/DProcess.Bff/appsettings*.json](src/DProcess.Bff/DProcess.Bff/appsettings.Development.json), [src/DProcess.Idp/DProcess.Idp/appsettings*.json](src/DProcess.Idp/DProcess.Idp/appsettings.Development.json) | Update `Bff:SignedOutLocalPath` (and `Bff:BaseUrl` in the IdP) |
| **Add OAuth client**              | [src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs](src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs)                                                                                                                       | Seed database with client registration                         |
| **Add permissions**               | [src/DProcess.Idp/DProcess.Idp/Data/DbSeeder.cs](src/DProcess.Idp/DProcess.Idp/Data/DbSeeder.cs)                                                                                                                             | Seed database with permissions                                 |
| **Standalone mode (no Aspire)**   | `appsettings.Development.json` in each project                                                                                                                                                                               | Manual configuration without orchestration                     |

## Troubleshooting
Common issues and fixes specific to this demo.

### Authentication Flow Issues

- **Login page not auto-loading (stuck at authorize endpoint):**
  - This was a known issue: IdP was returning HTTP 401 instead of HTTP 302 redirect
  - **Fix applied:** AuthorizationController now uses manual authentication check returning `Redirect()` (HTTP 302)
  - Verify in browser DevTools → Network: `/connect/authorize` should return `302 Found` with `Location: /Account/Login?ReturnUrl=...`
  - If you still see 401 status, check [src/DProcess.Idp/DProcess.Idp/Controllers/AuthorizationController.cs](src/DProcess.Idp/DProcess.Idp/Controllers/AuthorizationController.cs) has the manual authentication pattern
  
- **401 from `/api/*`:** 
  - Verify `Authorization` header is present in the request (check browser DevTools → Network)
  - Confirm IDP Authority matches the token issuer (see [Configuration Guidance](#configuration-guidance))
  - Check YARP cluster destination resolves correctly (inspect Aspire dashboard Resources tab)
  
- **Missing `permission` claims:** 
  - Verify IDP UserInfo endpoint includes `permission` claims in response
  - Check BFF maps them in `OnUserInformationReceived` event: [src/DProcess.Bff/DProcess.Bff/Program.cs](src/DProcess.Bff/DProcess.Bff/Program.cs)
  - Inspect browser cookies (`.AspNetCore.Cookies`) → decode JWT → verify `permission` claims exist
  
- **Entra external login fails:** 
  - Ensure `Entra:TenantId`, `ClientId`, and `ClientSecret` are configured. Update the first three in [src/DProcess.Idp/DProcess.Idp/appsettings.Development.json](src/DProcess.Idp/DProcess.Idp/appsettings.Development.json), and set `ClientSecret` via `dotnet user-secrets set "Entra:ClientSecret" "<secret>"`.
  - Verify redirect URI is registered in Entra app registration: `https://localhost:7046/signin-entra`
  - Check IDP logs for external authentication errors

- **Logout lands on a blank page:**
   - Confirm the OpenIddict client has `https://localhost:7092/signed-out` in `PostLogoutRedirectUri`.
   - Verify `Bff:SignedOutLocalPath` matches the signed-out page in [src/DProcess.Bff/DProcess.Bff/appsettings.Development.json](src/DProcess.Bff/DProcess.Bff/appsettings.Development.json).
   - Verify the IdP redirect uses `Bff:BaseUrl` + `Bff:SignedOutLocalPath` in [src/DProcess.Idp/DProcess.Idp/appsettings.Development.json](src/DProcess.Idp/DProcess.Idp/appsettings.Development.json).

### Configuration Issues

- **OAuth redirect_uri mismatch:** 
  - OpenIddict client registration in [src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs](src/DProcess.Idp/DProcess.Idp/OpenIddictSeeder.cs) contains redirect URIs for port 7181
  - BFF actually runs on port 7092 (see launch settings)
   - If you encounter redirect_uri mismatch errors, update the seeder to use `https://localhost:7092/signin-oidc` and `https://localhost:7092/signed-out`
   - If you encounter redirect_uri mismatch errors, update the seeder to use `https://localhost:7092/signin-oidc` and `https://localhost:7092/signed-out`
  - Delete the IDP database and restart to re-seed with correct URIs
  
- **Service connection failures in Aspire mode:**
  - Check Aspire dashboard (`https://localhost:17102`) → Resources tab → verify all services are running
  - Review service logs in Aspire dashboard → Console Logs tab
  - Ensure no port conflicts with other running applications

- **Configuration mismatches in standalone mode:**
  - BFF [appsettings.Development.json](src/DProcess.Bff/DProcess.Bff/appsettings.Development.json) references IDP at port 7241 and API at 7261
  - Launch settings define IDP at port 7046 and API at 7142
  - In Aspire mode, AppHost overrides mask this issue
  - For standalone mode, manually align appsettings ports with launch settings

## What's Next?
This demo sets the foundation for demo5, which focuses on downstream APIs and delegated access patterns.
