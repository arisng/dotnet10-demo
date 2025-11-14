# .NET 10 Identity Workshop Roadmap

This workspace hosts an incremental set of demos that teach the newest ASP.NET Core Identity capabilities shipping with .NET 10, including passkeys, dual-mode Blazor apps, RBAC, and the BFF security model. Every demo builds directly on the previous one so you can learn by doing without losing context.

## Grounded highlights (Nov 2025)

- **Passkeys everywhere:** ASP.NET Core Identity’s schema version 3 plus the new Blazor Web App template deliver turnkey passkey registration, login, and Manage UI.¹ ²
- **Out-of-the-box endpoints:** `MapAdditionalIdentityEndpoints` wires `/PasskeyCreationOptions` and `/PasskeyRequestOptions`, so our demos should keep Identity components intact instead of rewriting them.²
- **Security guardrails:** Microsoft recommends explicit HTTPS, HSTS, and custom origin validation when necessary; we’ll surface those practices in later demos.¹

> ¹ [Enable Web Authentication API (WebAuthn) passkeys](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0) · ² [Implement passkeys in ASP.NET Core Blazor Web Apps](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/blazor?view=aspnetcore-10.0)

## Quick Start

1. Install the .NET 10 SDK (Preview) plus the EF Core tools. Run `dotnet new update` so the local template includes the latest Identity bits called out in Microsoft’s documentation.²
2. Clone this repo, then start with `demo1` inside VS Code or JetBrains Rider.
3. Use the commands below to scaffold and run the first demo:

```powershell
cd demo1
dotnet new blazor -n Demo1.IdentityFoundation -au Individual --interactivity Auto --all-interactive
dotnet watch
```

> Each subsequent demo reuses the previous codebase. Copy the prior folder forward (e.g., `demo1` ➜ `demo2`) before applying the new steps so you always have a working checkpoint.

## Demo Lineup

| Demo  | Focus                                                    | Depends On | Highlights |
|-------|----------------------------------------------------------|------------|------------|
| demo1 | Identity foundation with Blazor Server + WASM dual mode  | —          | CLI scaffolding, cookie auth across render modes |
| demo2 | Dual-mode authentication state handoff                  | demo1      | Auto render flow, AuthenticationStateProvider probes |
| demo3 | Passkeys & WebAuthn-first login flows                    | demo2      | Identity v3 schema, manage page UX, passwordless sign-in |
| demo4 | Role-based access control with seeded admin accounts     | demo3      | `AddRoles`, Db seeding, admin-only UI |
| demo5 | Backend-for-Frontend secure APIs consumed from WASM      | demo4      | Minimal APIs with auth cookies, WASM HttpClient, failure UX |
| demo6 | Production polish (secrets, logging, profile enrichment) | demo5      | User Secrets, Serilog, custom `ApplicationUser` fields |

## Demo Details

### demo1 – Identity Foundation

- **Goal:** Scaffold a Blazor Web App that keeps Identity cookies valid across Server, Auto, and WASM render modes.
- **What you’ll do:** Run `dotnet new blazor -au Individual`, configure a local connection string, apply `InitialIdentity` migration, and verify login/register flows while toggling render modes.
- **Outcome:** A baseline solution you can reuse for every later demo.

### demo2 – Dual-Mode Authentication State Handoff

- **Goal:** Observe how authentication flows from the Server prerender to the WASM instance when using `@rendermode InteractiveAuto`.
- **What’s new:**
  - Add a dedicated `AuthStateProbe.razor` page that injects `AuthenticationStateProvider`, subscribes to `AuthenticationStateChanged`, and logs the `ClaimsPrincipal` before and after the WASM handoff.
  - Use `<CascadingAuthenticationState>` plus `OnAfterRenderAsync` to show when the client reconnects with the same auth cookie (look for the browser switching from WebSocket traffic to WASM-only requests).
  - Render both a server-only component (`@rendermode InteractiveServer`) and a WASM-only component (`@rendermode InteractiveWebAssembly`) on the same page to prove that Identity cookies seamlessly protect both.
- **Outcome:** You gain a concrete recipe for debugging auth propagation through Blazor Server, Auto, and WASM phases.

### demo3 – Passkey-Ready Identity

- **Goal:** Light up the .NET 10 passkey experience end-to-end.
- **What’s new:** Ensure `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3`, confirm `MapAdditionalIdentityEndpoints`, and exercise the Manage ➜ Passkeys UI plus passwordless login, following the Microsoft Learn guidance noted above.
- **Outcome:** Users can register and authenticate with platform passkeys alongside passwords.

### demo4 – Production RBAC

- **Goal:** Introduce roles, seed an admin, and gate protected UI.
- **What’s new:** `AddRoles<IdentityRole>()`, a scoped `DbSeeder`, `AdminDashboard.razor` guarded by `[Authorize(Roles="Admin")]`, and `<AuthorizeView>` navigation links.
- **Outcome:** Clear separation between admin and standard experiences with automated bootstrap.

### demo5 – BFF-Secured APIs for WASM

- **Goal:** Show how WASM components call secure APIs without tokens using the built-in BFF pattern.
- **What’s new:** Minimal API endpoints protected with `RequireAuthorization`, WASM components (e.g., `AdminDataFetcher`) running under `@rendermode InteractiveWebAssembly`, and resilient HttpClient logging.
- **Outcome:** Admins fetch sensitive data client-side while cookies stay server-issued and never exposed to storage.

### demo6 – Operational Polish & Profile Data

- **Goal:** Harden the boilerplate for real deployments and capture richer profile info.
- **What’s new:** Move secrets to User Secrets / env variables, configure Serilog or Application Insights, extend `ApplicationUser` with fields like `FullName`, and scaffold the Identity Manage page to edit them while incorporating Microsoft’s HTTPS/HSTS/origin recommendations for passkeys.
- **Outcome:** A production-ready baseline with observability, safer configuration, and customizable user identity data.

## Next Steps

1. Create `demo1` and add its README (`Goal`, `Prerequisites`, `How to Run`).
2. Duplicate `demo1` into `demo2` to build the Authentication State handoff lab, then continue cloning each completed demo forward before starting the next.
3. Keep this roadmap updated as new .NET 10 identity features ship (e.g., enriched audit logs or future Identity passkey UX improvements).

