# Demo4.2 atomic tasks

> Goal: break the demo4.2 plan into small, independently completable tasks.

## 0) Prep + structure
- [ ] Confirm demo4.2 uses **DProcess.* naming** (intentional deviation from repo conventions).
- [ ] Create `demo4.2/src/` folder if missing.
- [ ] Ensure Aspire templates are installed (`dotnet new install Aspire.ProjectTemplates`).
- [ ] Verify Aspire templates are available (`dotnet new list aspire`).
- [ ] Create `.slnx` solution file to host `DProcess.*` projects.
- [ ] Scaffold projects using `dotnet new` (see commands below).
- [ ] Add all projects to the solution.

### Scaffolding commands
```bash
# Workspace root
mkdir -p demo4.2/src

# Solution (.slnx)
# If --format slnx is not supported by your SDK, create .sln and convert in the IDE.
dotnet new sln -n DProcess -o demo4.2 --format slnx

# Aspire
dotnet new aspire-apphost -n DProcess.AppHost -o demo4.2/src/DProcess.AppHost
dotnet new aspire-servicedefaults -n DProcess.ServiceDefaults -o demo4.2/src/DProcess.ServiceDefaults

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
- [ ] Add EF Core + Identity + OpenIddict package references.
- [ ] Port `ApplicationDbContext` from `demo3/Demo3.BffRbac/Data/ApplicationDbContext.cs` into `DProcess.Idp`.
- [ ] Port `RolePermission` entity from `demo3/Demo3.BffRbac/Data/RolePermission.cs` into `DProcess.Idp`.
- [ ] Update namespaces to `DProcess.Idp.*` and align with new DB context.
- [ ] Add `PermissionService` based on `demo3/Demo3.BffRbac/Services/PermissionService.cs` into `DProcess.Idp.Security`.
- [ ] Wire `PermissionService` into DI.

## 3) IdP OpenIddict + Identity host
- [ ] Configure `Program.cs` for Razor components + Identity + OpenIddict + Entra external OIDC.
- [ ] Configure Identity to use `IdentitySchemaVersions.Version3`.
- [ ] Add `OpenIddictSeeder` hosted service to seed the BFF client.
- [ ] Add `AuthorizationController` to emit permission claims into tokens + userinfo.
- [ ] Ensure `/connect/userinfo` returns `permission` claims for BFF auth state.
- [ ] Add controllers to DI + routing.
- [ ] Add `appsettings.Development.json` with IdP DB + Entra settings.

## 4) IdP Identity UI (Blazor components)
- [ ] Port Blazor Identity components from demo4:
  - [ ] `Components/Account/Pages/Login.razor`
  - [ ] `Components/Account/Pages/ExternalLogin.razor`
  - [ ] `Components/Account/Shared/ExternalLoginPicker.razor`
  - [ ] `Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
  - [ ] `Components/Account/IdentityRedirectManager.cs`
- [ ] Ensure routes are under `/Account/*` and `MapAdditionalIdentityEndpoints()` is present.
- [ ] Ensure external login flow uses the Blazor components (no Razor Pages).

## 5) IdP seed data (users/roles/permissions)
- [ ] Port `DbSeeder.cs` from `demo3/Demo3.BffRbac/Data/DbSeeder.cs`.
- [ ] Seed roles: `Admin`, `Manager`, `User`.
- [ ] Seed users: `admin@local.app`, `manager@local.app`, `user@local.app`.
- [ ] Seed permissions and role-permission matrix aligned to demo3.
- [ ] Invoke seeder on startup (or via hosted service).

## 6) BFF auth + policies (DProcess.Bff)
- [ ] Add package references for OIDC + YARP.
- [ ] Configure `Program.cs` with OIDC client pointing at IdP.
- [ ] Enable `SaveTokens` + `GetClaimsFromUserInfoEndpoint` for access token forwarding.
- [ ] Map `permission` from UserInfo into the auth principal (ClaimActions).
- [ ] Register authorization policies for each permission (no claims transformation).
- [ ] Register `PermissionAuthorizationHandler` (port from demo3).
- [ ] Register `PersistingServerAuthenticationStateProvider` for InteractiveAuto.
- [ ] Configure YARP reverse proxy with access-token forwarding middleware.
- [ ] Add `/login` and `/logout` endpoints.
- [ ] Update `NavMenu` (or equivalent) with login/logout links.
- [ ] Add `appsettings.Development.json` with IdP + YARP cluster config.
- [ ] Add access token refresh strategy (offline_access + refresh or token management library).

## 7) API auth + permissions (DProcess.Api)
- [ ] Add `JwtBearer` package reference.
- [ ] Port authorization helpers from demo3:
  - [ ] `AuthorizationExtensions.cs`
  - [ ] `PermissionAuthorizationHandler.cs`
  - [ ] `PermissionRequirement.cs`
- [ ] Configure JWT validation with IdP authority + audience `api`.
- [ ] Register `PermissionAuthorizationHandler`.
- [ ] Map minimal endpoints with `.RequirePermission("...")`.
- [ ] Add `appsettings.Development.json` for IdP authority.
- [ ] If enabling Graph/OBO: add a second JWT bearer scheme for Entra and split endpoint auth policies.
- [ ] If enabling Graph/OBO: choose RBAC consistency strategy (enrich Entra principal or split policies).

## 8) Aspire AppHost (DProcess.AppHost)
- [ ] Wire up AppHost to reference IdP, BFF, and API projects.
- [ ] Expose external HTTP endpoints for IdP + BFF.
- [ ] Ensure service references between BFF ⇄ IdP/Api.

## 9) Solution wiring + build
- [ ] Verify project references and namespaces compile.
- [ ] Confirm ports/redirect URIs across IdP + BFF + Entra registration.
- [ ] Build `DProcess` solution.

## 10) Optional: OBO (Graph path) setup
- [ ] Add secondary Entra OIDC config for BFF (Graph-enabled path).
- [ ] Configure API with `Microsoft.Identity.Web` + Graph downstream API.
- [ ] Add YARP route (e.g., `/api/graph/*`) that uses Entra access token.
- [ ] Update API auth to support multi-issuer (OpenIddict + Entra) with per-route policies.
- [ ] Document RBAC handling for Entra endpoints (enrichment vs split policies).
- [ ] Document two Entra app registrations and required scopes.

## 11) Documentation updates
- [ ] Create `demo4.2/README.md` by inheriting demo4.1 narrative + note dedicated IdP.
- [ ] Update root `README.md` demo table to include demo4.2.
- [ ] Link chosen patterns to `.docs/reference/patterns/` entries.
