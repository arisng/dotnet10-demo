# Demo4.2 atomic tasks

> Goal: break the demo4.2 plan into small, independently completable tasks.

## 0) Prep + structure
- [x] Confirm demo4.2 uses **DProcess.* naming** (intentional deviation from repo conventions).
- [x] Decide issuer strategy **early** (single vs multi‑issuer). **Selected: multi‑issuer** (OpenIddict + Entra).
- [x] Create `demo4.2/` folder if missing.
- [ ] Ensure Aspire templates are installed (`dotnet new install Aspire.ProjectTemplates`).
- [ ] Verify Aspire templates are available (`dotnet new list aspire`).
- [x] Create `.slnx` solution file to host `DProcess.*` projects.
- [x] Scaffold projects using `dotnet new` (see commands below).
- [x] Add all projects to the solution.

### Scaffolding commands
```bash
# Workspace root
mkdir -p demo4.2

# Solution (.slnx)
# If --format slnx is not supported by your SDK, create .sln and convert in the IDE.
dotnet new sln -n DProcess -o demo4.2 --format slnx

# Aspire
dotnet new aspire-apphost -n DProcess.AppHost -o demo4.2/src/DProcess.AppHost
dotnet new aspire-servicedefaults -n DProcess.ServiceDefaults -o demo4.2/src/DProcess.ServiceDefaults

# Note: some Aspire templates or IDE scaffolds may generate a .sln.
# If that happens, convert to .slnx and delete/ignore the .sln to avoid drift.

# Blazor Web Apps
# IdP (Identity-based)
dotnet new blazor -n DProcess.Idp -o demo4.2/src/DProcess.Idp -int Auto -au Individual

# BFF (no built-in auth; we add OIDC manually)
dotnet new blazor -n DProcess.Bff -o demo4.2/src/DProcess.Bff -int Auto -au None

# API (ASP.NET Core Web API template; minimal APIs by default)
dotnet new webapi -n DProcess.Api -o demo4.2/src/DProcess.Api

# Shared library
dotnet new classlib -n DProcess.Shared -o demo4.2/src/DProcess.Shared
```

## 1) Shared project (DProcess.Shared)
- [ ] Add permission constants (align with demo3: `weather.read`, `weather.write`, `users.read`, `users.write`, `users.delete`, `reports.view`, `reports.export`).
- [ ] Add shared DTOs/contracts used by IdP/BFF/Api (minimal placeholders if needed).
- [ ] Reference `DProcess.Shared` from IdP/BFF/Api.

## 2) IdP data + RBAC (DProcess.Idp)
- [x] Add EF Core + Identity + OpenIddict package references.
- [x] Port `ApplicationDbContext` from `demo3/Demo3.BffRbac/Data/ApplicationDbContext.cs` into `DProcess.Idp`.
- [x] Port `RolePermission` entity from `demo3/Demo3.BffRbac/Data/RolePermission.cs` into `DProcess.Idp`.
- [x] Update namespaces to `DProcess.Idp.*` and align with new DB context.
- [ ] Add `PermissionService` based on `demo3/Demo3.BffRbac/Services/PermissionService.cs` into `DProcess.Idp.Security`.
- [x] Wire `PermissionService` into DI.

## 3) IdP OpenIddict + Identity host
- [x] Configure `Program.cs` for Razor components + Identity + OpenIddict + Entra external OIDC.
- [x] Ensure IdP uses **Interactive Server only** (no WASM components/render mode).
- [x] Configure Identity to use `IdentitySchemaVersions.Version3`.
- [x] Add `OpenIddictSeeder` hosted service to seed the BFF client.
- [x] Add `AuthorizationController` to emit permission claims into tokens + userinfo.
- [x] **Fix auto-redirect pattern:** Use manual authentication check (`HttpContext.AuthenticateAsync()`) in `AuthorizeEndpoint()` to return HTTP 302 redirect (not 401) for unauthenticated users.
- [x] Ensure `/connect/userinfo` returns `permission` claims for BFF auth state.
- [x] Add controllers to DI + routing.
- [x] Add `appsettings.Development.json` with IdP DB + Entra settings.

## 4) IdP Identity UI (Blazor components)
- [x] Port Blazor Identity components from demo4:
  - [x] `Components/Account/Pages/Login.razor`
  - [x] `Components/Account/Pages/ExternalLogin.razor`
  - [x] `Components/Account/Shared/ExternalLoginPicker.razor`
  - [x] `Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
  - [x] `Components/Account/IdentityRedirectManager.cs`
- [x] Ensure routes are under `/Account/*` and `MapAdditionalIdentityEndpoints()` is present.
- [x] Ensure external login flow uses the Blazor components (no Razor Pages).

## 5) IdP seed data (users/roles/permissions)
- [x] Port `DbSeeder.cs` from `demo3/Demo3.BffRbac/Data/DbSeeder.cs`.
- [x] Seed roles: `Admin`, `Manager`, `User`.
- [x] Seed users: `admin@local.app`, `manager@local.app`, `user@local.app`.
- [x] Seed permissions and role-permission matrix aligned to demo3.
- [x] Invoke seeder on startup (or via hosted service).

### Conflicts / Notes (IdP audit)
- `PermissionService` exists but is in [demo4.2/src/DProcess.Idp/DProcess.Idp/Services/PermissionService.cs](demo4.2/src/DProcess.Idp/DProcess.Idp/Services/PermissionService.cs) rather than the requested `DProcess.Idp.Security` namespace.

## 6) BFF auth + policies (DProcess.Bff)
- [x] Add package references for OIDC + YARP.
- [x] Configure `Program.cs` with OIDC client pointing at IdP.
- [x] Enable `SaveTokens` + `GetClaimsFromUserInfoEndpoint` for access token forwarding.
- [x] Map `permission` from UserInfo into the auth principal (use `OnUserInformationReceived` to handle arrays).
- [x] Register authorization policies for each permission (no claims transformation).
- [x] Register `PermissionAuthorizationHandler` (port from demo3).
- [x] Register `PersistingServerAuthenticationStateProvider` for InteractiveAuto.
- [x] Configure YARP reverse proxy with access-token forwarding middleware.
- [x] Add `/login` and `/logout` endpoints.
- [x] Update `NavMenu` (or equivalent) with login/logout links.
- [x] Add `appsettings.Development.json` with IdP + YARP cluster config.
- [x] Add access token refresh strategy (offline_access + refresh or token management library).

## 7) API auth + permissions (DProcess.Api)
- [x] Add `JwtBearer` package reference.
- [x] Port authorization helpers from demo3:
  - [x] `AuthorizationExtensions.cs`
  - [x] `PermissionAuthorizationHandler.cs`
  - [x] `PermissionRequirement.cs`
- [x] Configure JWT validation with IdP authority + audience `api`.
- [x] Register `PermissionAuthorizationHandler`.
- [x] Map minimal endpoints with `.RequirePermission("...")`.
- [x] Add `appsettings.Development.json` for IdP authority.
- [x] Add a second JWT bearer scheme for Entra and split endpoint auth policies (required by selected multi‑issuer strategy).
- [x] Choose RBAC consistency strategy for Entra endpoints (enrich Entra principal or split policies).

## 8) Aspire AppHost (DProcess.AppHost)
- [x] Wire up AppHost to reference IdP, BFF, and API projects.
- [x] Expose external HTTP endpoints for IdP + BFF.
- [x] Ensure service references between BFF ⇄ IdP/Api.

## 9) Solution wiring + build
- [ ] Verify project references and namespaces compile.
- [ ] Confirm ports/redirect URIs across IdP + BFF + Entra registration.
- [ ] Build `DProcess` solution.

## 10) Optional: OBO (Graph path) setup
- [ ] Add secondary Entra OIDC config for BFF (Graph-enabled path).
- [ ] Configure API with `Microsoft.Identity.Web` + Graph downstream API.
- [ ] Add YARP route (e.g., `/api/graph/*`) that uses Entra access token.
- [ ] Update API auth to support multi-issuer (OpenIddict + Entra) with per-route policies (selected strategy).
- [ ] Document RBAC handling for Entra endpoints (enrichment vs split policies).
- [ ] Document two Entra app registrations and required scopes.

## 11) Documentation updates
- [x] Create `demo4.2/README.md` by inheriting demo4.1 narrative + note dedicated IdP.
- [x] Update root `README.md` demo table to include demo4.2.
- [x] Link chosen patterns to `.docs/reference/patterns/` entries.
