# .NET 10 Identity Workshop Roadmap

This workspace hosts an incremental set of demos that teach the newest ASP.NET Core Identity capabilities shipping with .NET 10, including passkeys, dual-mode Blazor apps, RBAC, and the BFF security model. Every demo builds directly on the previous one so you can learn by doing without losing context.

## Grounded highlights (Dec 2025)

- **Passkeys everywhere:** ASP.NET Core Identity’s schema version 3 plus the new Blazor Web App template deliver turnkey passkey registration, login, and Manage UI.¹ ²
- **Out-of-the-box endpoints:** `MapAdditionalIdentityEndpoints` wires `/PasskeyCreationOptions` and `/PasskeyRequestOptions`, so our demos should keep Identity components intact instead of rewriting them.²
- **Security guardrails:** Microsoft recommends explicit HTTPS, HSTS, and custom origin validation when necessary; we’ll surface those practices in later demos.¹
- **Documentation enhancements:** Comprehensive `.docs/` folder structure implemented for research, issues, and agent workflows to support AI-driven development.
- **AI agents integration:** Multi-agent architecture with Research-Agent, Implementation-Agent, and Verifier-Agent for structured .NET 10 feature development.

> ¹ [Enable Web Authentication API (WebAuthn) passkeys](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0) · ² [Implement passkeys in ASP.NET Core Blazor Web Apps](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/blazor?view=aspnetcore-10.0)

## Quick Start

1. Install the .NET 10 SDK (Preview) plus the EF Core tools. Run `dotnet new update` so the local template includes the latest Identity bits called out in Microsoft’s documentation.²
2. Clone this repo, then start with `demo1` inside VS Code or JetBrains Rider.
3. Use the commands below to apply the initial migration and run the first demo:

```powershell
cd demo1/Demo1.IdentityFoundation/Demo1.IdentityFoundation
dotnet ef database update
dotnet watch
```

> When you are ready for `demo2`, run the same commands inside `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff`, sign in, and browse to `/auth-state-probe` to watch the InteractiveAuto handoff in action.

> Port convention: all demos run on `https://localhost:7210` (and `http://localhost:5210` for non-TLS callbacks). Update each new demo’s `launchSettings.json` if a template scaffolds different ports.

> Each subsequent demo reuses the previous codebase. Copy the prior folder forward (e.g., `demo1` ➜ `demo2`) before applying the new steps so you always have a working checkpoint.

## Demo Lineup

| Demo  | Status     | Focus                                         | Depends On | Highlights                                                               |
| ----- | ---------- | --------------------------------------------- | ---------- | ------------------------------------------------------------------------ |
| demo1 | Completed  | Identity scaffolding baseline                 | —          | CLI scaffolding, cookie auth foundation                                  |
| demo2 | Completed  | Dual-mode diagnostics + Passkeys              | demo1      | Auth state probe, full passkey implementation, WASM caching              |
| demo3 | Completed  | BFF APIs + Permission-Based RBAC              | demo2      | Fine-grained permissions, role→permission mapping, claims transformation |
| demo4 | Completed  | Microsoft Entra ID integration                | demo3      | External provider, Graph API (OBO), inherits permission system           |
| demo5 | Completed  | Custom Downstream APIs (Microservices)        | demo4      | Separate API project, Bearer tokens, OBO flow, Architecture comparison   |
| demo6 | Planned    | Entra ID claims → Permission mapping          | demo5      | Auto-map Entra groups/roles to local permissions                         |
| demo7 | Planned    | Production hardening (multi-identity edition) | demo6      | Secrets, logging, monitoring, HTTPS enforcement                          |

## Demo Details

### demo1 – Identity Foundation

- **Goal:** Scaffold a Blazor Web App that keeps Identity cookies valid across Server, Auto, and WASM render modes.
- **What you'll do:** Run `dotnet new blazor -au Individual`, configure a local connection string, apply `InitialIdentity` migration, and verify login/register flows while toggling render modes.
- **What's new:** Initial Blazor Web App scaffolding with ASP.NET Core Identity, cookie authentication foundation, and render mode compatibility.
- **Outcome:** A baseline solution you can reuse for every later demo.
- **Note:** While demo1 provides the scaffolding, **demo2 becomes the real baseline** with complete passkey implementation and diagnostics that all subsequent demos build upon.

### demo2 – Dual-Mode Diagnostics + Passkeys

- **Goal:** Master authentication state flow through InteractiveAuto phases AND implement complete passkey infrastructure, establishing the comprehensive baseline for all subsequent demos.
- **What's new:**
  - Dedicated `AuthStateProbe.razor` page with 4-phase InteractiveAuto lifecycle visualization
  - Visual delay controls via `RenderDelayMs` query parameter to observe phase transitions
  - Real-time status indicators showing when delays are active between timeline events
  - `<CascadingAuthenticationState>` wrapping the entire app for seamless auth flow
  - Reusable `AuthStateSurface` diagnostic component rendered with both `InteractiveServer` and `InteractiveWebAssembly`
  - **Complete passkey implementation:** IdentitySchemaVersion3, `/PasskeyCreationOptions` and `/PasskeyRequestOptions` endpoints, full Manage UI (`Passkeys.razor`, `RenamePasskey.razor`), passwordless login flow
  - Production published mode diagnostics with HTTP caching behavior (`max-age=31536000, immutable`)
  - Local Storage caching discovery and documentation
  - **Key learning:** InteractiveAuto progressive enhancement (4 phases on first visit, 3 phases on subsequent visits when WASM is cached)
- **Outcome:** Both a diagnostic toolkit for auth propagation AND a production-ready passkey implementation that serves as the **real baseline** for demo3-6. This is where the workshop truly begins.

### demo3 – BFF APIs + Permission-Based RBAC

- **Goal:** Implement Backend-for-Frontend pattern with fine-grained permission-based authorization, establishing the security model before introducing external identity providers.
- **What's new:**
  - **Data Model:** `Role`, `Permission`, `RolePermission` junction table, extend Identity's user-role relationships
  - **Authorization Infrastructure:**
    - `IPermissionService` to aggregate user → roles → permissions
    - `IClaimsTransformation` implementation (`PermissionClaimsTransformation`) that adds permission claims to `ClaimsPrincipal` on each request (standard .NET pattern)
    - Custom `PermissionRequirement` and `PermissionAuthorizationHandler`
    - Extension method: `RequirePermission("weather.read")`
    - Use .NET 10's `AddAuthorizationBuilder()` fluent API for policy registration
  - **Seed Data:**
    - Roles: Admin, Manager, User
    - Permissions: `weather.read`, `weather.write`, `users.read`, `users.write`, `users.delete`, `reports.view`, `reports.export`
    - Seeded passkey users: admin@local.app (Admin), manager@local.app (Manager), user@local.app (User)
  - **BFF API Endpoints:**
    - `/api/weather` (GET: `weather.read`, POST: `weather.write`)
    - `/api/users` (GET: `users.read`, DELETE: `users.delete`)
    - `/api/reports` (GET: `reports.view`, `/export`: `reports.export`)
  - **WASM Components:**
    - `WeatherDataFetcher.razor`, `UserManagement.razor`, `ReportsViewer.razor`, `ReportsExporter.razor`
    - Each component calls BFF APIs via `HttpClient`, shows permission-specific UI with 401/403 error handling
  - **UI Authorization:** `<AuthorizeView Policy="RequirePermission" Resource="users.delete">`
  - **Enhanced Diagnostics:** `AuthStateProbe` displays user's roles and aggregated permission claims
  - **Observability (.NET 10):** Leverage built-in authorization metrics (`aspnetcore.authorization.*`) for monitoring permission checks
  - **Cookie API Behavior (.NET 10):** Demonstrate automatic 401/403 responses for Minimal APIs (no login redirects) via `IApiEndpointMetadata`
- **Architecture:** Monolithic Blazor Web App (Server + WASM + APIs + RBAC in one project)
- **Outcome:** Complete permission-based authorization system using .NET 10 best practices with API endpoints explicitly declaring required permissions. Foundation ready for Entra ID integration (demo4 will map Entra roles → existing permissions). Clear separation: authentication (who you are) vs. authorization (what you can do).

### demo4 – Microsoft Entra ID Integration

- **Goal:** Add Microsoft Entra ID as an external identity provider alongside local passkey authentication, supporting a hybrid scenario (B2C customers use passkeys, employees use Entra ID).
- **What's new:**
  - Configure `AddMicrosoftIdentityWebApp()` or OpenID Connect for Entra ID
  - **Downstream API (Microsoft Graph):** Configure `EnableTokenAcquisitionToCallDownstreamApi` to fetch user profile data (photo, job title) server-side, demonstrating the On-Behalf-Of (OBO) flow
  - **Secure State Serialization:** Explicitly demonstrate `AddAuthenticationStateSerialization` to pass the Entra identity to the WASM client without exposing access tokens
  - Update login UI to offer "Sign in with Microsoft" alongside passkey/password options
  - Map Entra ID claims (email, name, oid) to `ApplicationUser`
  - Handle account linking scenarios (e.g., same email exists as local + Entra user)
  - **Reuse permission system from demo3:** Entra users get role assignments via database seeding initially
  - Update `AuthStateProbe` to differentiate local vs. Entra ID authentication source
  - BFF APIs continue working for both local and Entra-authenticated users (cookie-based)
- **Architecture:** Still monolithic, introduce `ExternalAuthenticationState` to track provider
- **Outcome:** Unified authentication experience where identity source is transparent to the authorization layer. Both passkey admins and Entra admins have identical permissions through the same claims transformation pipeline.

### demo5 – Custom Downstream APIs (Microservice Pattern)

- **Goal:** Create a standalone protected API service and consume it from the Blazor app using Entra ID tokens, contrasting the "BFF" (Cookie) vs. "Downstream" (Token) architectures.
- **What's new:**
  - **New Project:** `Demo5.ProtectedApi` (ASP.NET Core Minimal API) running on a separate port
  - **API Security:** Configure `AddMicrosoftIdentityWebApi` to validate Bearer tokens
  - **Entra Configuration:**
    - Expose an API in Entra ID (App ID URI)
    - Define custom scopes: `Forecast.Read`
    - Grant permission to the Blazor client app
  - **Client Implementation:**
    - Use `IDownstreamApi` helper to call the custom API
    - Demonstrate the "On-Behalf-Of" (OBO) flow where the user's identity flows to the API
  - **Architecture Comparison:**
    - **BFF API:** `/api/weather` (Local, Cookie, Implicit Trust)
    - **Downstream API:** `https://localhost:xxxx/weather` (Remote, Token, Explicit Trust)
- **Outcome:** A solution containing both monolithic (BFF) and microservice (Downstream) patterns, clearly demonstrating when and how to use each security model.

### demo6 – Entra ID Claims → Permission Mapping

- **Goal:** Implement automatic role mapping based on Entra ID App Roles, enabling centralized permission management through Entra ID.
- **What's new:**
  - **Entra App Roles as Source of Truth:** Define App Roles (e.g., `GlobalAdmin`, `ContentManager`) in the Entra Manifest
  - Enhance `ClaimsTransformation` middleware:
    - If Entra user: read `roles` claim → map to local roles → load permissions
    - If local user: existing role lookup from demo3
  - Example mappings:
    - Entra App Role "GlobalAdmin" → local "Admin" role → all admin permissions
    - Entra App Role "ContentManager" → local "Manager" role → manager permissions
  - Both local passkey admins and Entra admins have identical permission claims
  - Add `RoleMappingConfiguration` table to manage Entra App Role value → local role mappings
  - Admin UI to configure mappings (optional: `RoleMappingManager.razor`)
  - Update diagnostics to show Entra roles and their mapped permissions
- **Outcome:** Centralized Entra ID role management automatically grants app permissions. IT admins control access via Entra App Roles without touching the application database. Authorization logic remains identity-source agnostic.

### demo7 – Production Hardening (Multi-Identity Edition)

- **Goal:** Prepare the hybrid passkey + Entra ID app for production deployment with operational best practices and enterprise-grade observability.
- **What's new:**
  - **Secrets Management:**
    - Move Entra ID client secrets to Azure Key Vault or User Secrets
    - Environment-specific configuration for Dev/Staging/Production
  - **Strongly-Typed Configuration:**
    - Replace configuration "magic strings" (Scopes, TenantIds) with `IOptions<EntraSettings>` pattern
  - **Logging & Telemetry:**
    - Configure Serilog with structured logging (capture auth provider, user source, permission checks)
    - Add Application Insights telemetry for sign-in events, API calls, authorization failures
    - Custom metrics: passkey vs. Entra sign-in ratio, permission denial rates
  - **Enhanced User Profile:**
    - Extend `ApplicationUser` with custom fields: `FullName`, `Department`, `PreferredAuthMethod`
    - Update Manage profile page to edit custom fields
    - Sync Entra ID profile data (displayName, jobTitle) to local fields on sign-in
  - **Security Hardening:**
    - Enforce HTTPS/HSTS with custom origin validation for passkeys (per Microsoft docs)
    - Add Content Security Policy headers
    - Configure rate limiting for API endpoints
  - **Health Checks:**
    - Database connectivity check
    - Entra ID availability check (token endpoint)
    - Permission cache health
  - **Deployment:**
    - Document deployment to Azure App Service with managed identity
    - Configure Entra ID app registration for production redirect URIs
    - Database migration strategy for zero-downtime deployments
- **Outcome:** A production-ready template for SaaS apps requiring both consumer (passkey) and enterprise (Entra ID) authentication, with comprehensive observability, secure configuration management, and operational resilience.

## Next Steps

1. Implement `demo6` – Entra ID Claims → Permission Mapping: Create the demo using the copy-demo script, then add automatic role mapping based on Entra ID App Roles.
2. Implement `demo7` – Production Hardening: Build on demo6 to add secrets management, logging, telemetry, and security hardening for production deployment.
3. Validate all demos (demo1-demo5) for completeness and alignment with changelog achievements.
4. Keep this roadmap updated as new .NET 10 identity features ship.

