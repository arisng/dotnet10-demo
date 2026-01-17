# .NET 10 Modern Architecture Workshop

This repo is a **progressive workshop** for continuously evolving a modern .NET 10 web application across **both backend and frontend**. It is designed to stay current (today: **January 17, 2026**) and to grow beyond what is already documented. Each demo builds on the previous one so you can learn by doing without losing context.

**Motivation:** turn the workshop into a reusable blueprint that can bootstrap a SaaS business quickly. The goal is to plug in a **dynamic business domain** and produce deployable POCs/MVPs within a day while still following modern architecture standards.

## Patterns Catalog (Single Source of Truth)

We actively maintain `.docs/reference/patterns/` as the curated catalog of modern industry standards and web architecture patterns. It is continuously updated and **drives what we build next**.

- Start here: `.docs/reference/patterns/index.md`
- Pattern entries live in: `.docs/reference/patterns/catalog/`
- Guidance for choosing and applying patterns: `.docs/reference/patterns/guidance/`

## Scope & Glossary (How We Use Terms)

- **Scope:** This workshop is not limited to what is already documented. The catalog and demos evolve as new standards and platform features emerge.
- **Pattern:** A reusable architectural or technical solution (e.g., BFF, OIDC, RBAC) documented in the patterns catalog.
- **Business feature:** Stakeholder-facing capability or outcome the product delivers (e.g., “approve invoices,” “export reports”).
- **Infrastructure capability:** Foundational technical work that enables business features (e.g., identity integration, observability, multi-tenancy).

## Research & Implementation Planning (Required)

- Every selected pattern **must** be researched and grounded with references to **official docs and/or reputable technical blogs**.
- The **implementation plan and references live in the demo’s `.docs/` folder**, not in the root README.
- Root README stays high-level; demo-level `.docs/` files carry the detail.

## Progression Map (How We Track Evolution)

**Progression template (per demo)**

```
Demo: <demoN>
Inherits: <demoN-1>
Adds patterns:
- <Pattern A>
- <Pattern B>
- <Pattern C>
Business lift:
- <New segment, tier, or workflow enabled>
- <Operational or revenue impact>
Technical lift:
- <New runtime boundary, security model, or scalability feature>
- <Key platform capability added>
```

**Example – demo4**

```
Demo: demo4
Inherits: demo3
Adds patterns:
- Entra ID integration
- App Roles → permissions mapping
- OBO (Microsoft Graph)
Business lift:
- Enterprise SSO onboarding
- Centralized role management
Technical lift:
- External identity boundary
- Delegated token flow
```

This progression view is the **strategic map** for building a reusable SaaS blueprint with concrete implementation, strong testing, and production‑ready deployment. Product Owners can provide new business demands, and Tech Leads can quickly map them to an existing demo or select new patterns from the catalog to create the next demo. The Tech Lead continuously maintains and evolves the patterns catalog so the blueprint stays aligned with modern industry standards.

**Decision flow (PO → TL)**

```
PO input → match business lift → find demo
        → if no match → select patterns from catalog → create new demo
        → document plan in demo<N>/.docs/research/ → implement + test
```

**Documentation structure**

- **Demo Lineup (this README):** high-level view of the journey and status.
- **Demo README (`demo<N>/README.md`):** goal, prerequisites, how to run, and what’s new.
- **Demo research & plans (`demo<N>/.docs/research/`):** per‑pattern research notes and implementation plan, with references.
- **Patterns Catalog (`.docs/reference/patterns/`):** the authoritative catalog and guidance used by all demos.

This provides traceability from **catalog → research → implementation**, while keeping the root README focused and scannable.

## Quick Start

1. Install the latest .NET 10 SDK plus the EF Core tools. Run `dotnet new update` so the local template includes the newest Identity scaffolding bits.
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

| Demo    | Status    | Focus                                           | Depends On | Highlights                                                                                              |
| ------- | --------- | ----------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------------- |
| demo1   | Completed | Identity scaffolding baseline                   | —          | CLI scaffolding, cookie auth foundation                                                                 |
| demo2   | Completed | Dual-mode diagnostics + Passkeys                | demo1      | Auth state probe, full passkey implementation, WASM caching                                             |
| demo3   | Completed | BFF APIs + Permission-Based RBAC                | demo2      | Fine-grained permissions, role→permission mapping, claims transformation                                |
| demo4   | Completed | Microsoft Entra ID + Claims Mapping             | demo3      | External provider, Graph API (OBO), Entra App Roles mapping, identity-source agnostic auth              |
| demo5   | Completed | Custom Downstream APIs (Microservices)          | demo4      | Separate API project, Bearer tokens, OBO flow, Architecture comparison                                  |
| demo5.1 | Completed | Distributed Modular Monolith with Aspire & YARP | demo5      | .NET Aspire orchestration, YARP Proxy, "Two Locks" security model, .NET 10 Built-in OpenAPI + Scalar UI |
| demo6   | Planned   | The Multi-Tenant SaaS Monolith (SaaS)           | demo5.1    | Finbuckle, Multi-Identity per Tenant, Shared/Dedicated DB Choice                                        |
| demo7   | Planned   | Feature Flag Management & Hardening             | demo6      | Subscription-based Flags, Azure AppConfig, Operational Hardening                                        |

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

### demo4 – Microsoft Entra ID Integration + Claims Mapping

- **Goal:** Add Microsoft Entra ID as an external identity provider alongside local passkey authentication, and implement automatic role mapping based on Entra ID App Roles to enable centralized permission management.
- **What's new:**
  - **Entra ID Provider Integration:**
    - Configure `AddMicrosoftIdentityWebApp()` or OpenID Connect for Entra ID
    - **Downstream API (Microsoft Graph):** Configure `EnableTokenAcquisitionToCallDownstreamApi` to fetch user profile data (photo, job title) server-side, demonstrating the On-Behalf-Of (OBO) flow
    - **Secure State Serialization:** Explicitly demonstrate `AddAuthenticationStateSerialization` to pass the Entra identity to the WASM client without exposing access tokens
    - Update login UI to offer "Sign in with Microsoft" alongside passkey/password options
    - Map Entra ID claims (email, name, oid) to `ApplicationUser`
    - Handle account linking scenarios (e.g., same email exists as local + Entra user)
  - **Entra ID Claims Mapping (App Roles → Permissions):**
    - **Entra App Roles as Source of Truth:** Define App Roles (e.g., `GlobalAdmin`, `ContentManager`) in the Entra Manifest
    - Enhance `ClaimsTransformation` middleware:
      - If Entra user: read `roles` claim → map to local roles → load permissions
      - If local user: existing role lookup from demo3
    - Example mappings:
      - Entra App Role "GlobalAdmin" → local "Admin" role → all admin permissions
      - Entra App Role "ContentManager" → local "Manager" role → manager permissions
    - Both local passkey admins and Entra-authenticated admins have identical permission claims
    - Add `RoleMappingConfiguration` table to manage Entra App Role value → local role mappings
    - Admin UI to configure mappings (optional: `RoleMappingManager.razor`)
    - Update `AuthStateProbe` to show Entra roles and their mapped permissions
  - **Reuse permission system from demo3:** Authorization infrastructure (permissions, policies) remains unchanged
  - BFF APIs continue working for both local and Entra-authenticated users (cookie-based)
- **Architecture:** Still monolithic, introduce `ExternalAuthenticationState` to track provider, enhance authorization middleware for multi-source role mapping
- **Outcome:** Unified authentication experience where identity source is transparent to the authorization layer. Both passkey admins and Entra admins have identical permissions through the same claims transformation pipeline. Centralized Entra ID role management automatically grants app permissions without touching the application database.

### demo5 – Custom Downstream APIs (Microservice Pattern)

- **Goal:** Create a standalone protected API service and consume it from the Blazor app using Entra ID tokens, contrasting the "BFF" (Cookie) vs. "Downstream" (Token) architectures. Demonstrate how claims mapped in demo4 flow transparently through to custom downstream APIs.
- **What's new:**
  - **Downstream API Architecture:**
    - New Project: `Demo5.DownstreamApi.WeatherApi` (ASP.NET Core Minimal API) running on a separate port
    - API Security: Configure `AddMicrosoftIdentityWebApi` to validate Bearer tokens
    - Entra Configuration:
      - Expose an API in Entra ID (App ID URI)
      - Define custom scopes: `Forecast.Read`
      - Grant permission to the Blazor client app
    - Client Implementation:
      - Use `IDownstreamApi` helper to call the custom API
      - Demonstrate the "On-Behalf-Of" (OBO) flow where the user's identity flows to the API
    - Architecture Comparison:
      - **BFF API:** `/api/weather` (Local, Cookie, Implicit Trust)
      - **Downstream API:** `https://localhost:xxxx/weather` (Remote, Token, Explicit Trust)
  - **Claims Flow:** Show that permission claims mapped in demo4 are available in downstream API (validates end-to-end authorization)
- **Architecture:** Still monolithic, introduce `IDownstreamApi` abstraction for external service communication
- **Outcome:** A solution containing both monolithic (BFF) and microservice (Downstream) patterns, clearly demonstrating when and how to use each security model. Understanding that claims transformation happens centrally (demo4) and flows through all downstream consumers.

### demo5.1 – Distributed Modular Monolith with Aspire & YARP

- **Goal:** Evolve the "Downstream API" pattern into a production-grade Distributed Modular Monolith architecture using .NET Aspire and YARP.
- **What's new:**
  - **.NET Aspire Orchestration:** Centralized management of service discovery, environment variables, and local cloud-native topology.
  - **YARP (Yet Another Reverse Proxy):** Implemented in the Frontend (BFF) to forward `/api/*` requests, removing the need for business logic or manual HttpClient wrappers in the presentation layer.
  - **Identity "Shift Left":** The `ApiService` now owns the Identity logic and user data, while the Frontend focuses on authentication and token acquisition.
  - **"Two Locks" Security Model:** Implementation of defense-in-depth with an outer lock (OAuth scopes/coarse API access) and an inner lock (Local RBAC/fine-grained business permissions).
  - **Multi-Identity Support:** Unified bearer token boundary accepting both Entra ID tokens and first-party JWTs for local accounts.
  - **BFF Token Management:** Proxy middleware automatically attaches tokens to forwarded requests.
  - **Modern .NET 10 OpenAPI:** Integration of built-in `Microsoft.AspNetCore.OpenApi` with source-generated metadata and the **Scalar** interactive API UI.
- **Architecture:** Distributed Modular Monolith orchestrated by .NET Aspire (Web, ApiService, ServiceDefaults).
- **Outcome:** A cloud-native architecture that simplifies the BFF by delegating business routing to YARP and centralizing domain logic in a professional "Modular Monolith" backend. Use the modern Scalar UI to test secure endpoints directly.


### demo6 – The Multi-Tenant SaaS Monolith (SaaS)

- **Goal:** Transform the Distributed Modular Monolith from demo5.1 into a professional SaaS platform with multi-tenant isolation and per-customer identity configurations.
- **What's new:**
  - **Finbuckle.MultiTenant Integration:** Complete setup for tenant resolution via hostnames and header propagation.
  - **Identity-as-a-Setting (Multi-Identity):** Dynamically toggle between Entra ID and local (Passkey) only login based on the resolved tenant's configuration.
  - **Hybrid Data Isolation:** Support both Shared Database (logical isolation via Global Query Filters) and Dedicated Database (physical separation via connection strings).
  - **Tenant Configuration Store:** Create a SQL-backed store for per-tenant metadata, branding, and identity rules.
  - **Blazor Circuit Persistence:** Specialized `ITenantProvider` to handle multi-tenancy in stateful `InteractiveServer` connections.
- **Architecture:** Distributed Modular Monolith orchestrated by .NET Aspire with Finbuckle isolation.
- **Outcome:** A sophisticated SaaS foundation that adapts its security and data persistence layers based on the customer context.

### demo7 – Feature Flag Management & Hardening

- **Goal:** Implement subscription-based feature management in the SaaS platform and prepare the solution for production with enterprise hardening.
- **What's new:**
  - **Feature Flag Management:** Integrate `Microsoft.FeatureManagement` with the Tenant Configuration layer to toggle high-value features (e.g., "Premium Weather Reports") per-tenant.
  - **Azure App Configuration:** Use Azure AppConfig and Key Vault for centralized, dynamic management of flags and secrets.
  - **Secrets Management:** Managed Identity integration and production-grade environment-specific configuration.
  - **Operational Observability:** Serilog structured logging, custom OpenTelemetry metrics, and health check dashboards.
  - **Security Hardening:** Content Security Policy (CSP), rate limiting, and zero-downtime database migration strategies.
- **Outcome:** A production-ready SaaS application with powerful business-driven feature toggles and comprehensive operational resilience.


## Next Steps

1. Implement `demo6` – The Multi-Tenant SaaS Monolith (SaaS): Build the Multi-Identity pipeline and data isolation layers using Finbuckle.
2. Implement `demo7` – Feature Flag Management & Hardening: Implement subscription-based toggles and production observability.
3. Validate all demos (demo1-demo5.1) for completeness and alignment with changelog achievements.
4. Keep this roadmap updated as new .NET 10 identity features ship.

## Demo Creation Rules

- Every new demo must introduce at least one pattern from `.docs/reference/patterns/catalog/`.
- Pattern selection must build on prior demos and reflect a strategic progression (foundation → integration → distribution → hardening).
- The demo README must list the chosen patterns and link back to their catalog entries.
- Each chosen pattern must have demo-level research + implementation planning documented in `demo<N>/.docs/research/` with references.
