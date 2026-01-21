# Demo 4.1: Entra + BFF (YARP) + Interactive Auto + Aspire

[[Home](../README.md) > **Demo 4.1**]

## Goal
Demonstrate refined Entra ID integration with the BFF pattern using YARP reverse proxy and .NET Aspire orchestration for secure downstream API communication in a distributed setup.

## Patterns Selected (Catalog)
Enterprise authentication, API proxying, and distributed orchestration patterns introduced in this demo.

| Pattern                                                                                                               | Why Here                                                                  | Evidence                                                                      |
| --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| [auth-obo-flow](../.docs/reference/patterns/catalog/auth-obo-flow.md)                                                 | Implements On-Behalf-Of flow for secure token exchange to downstream APIs | [SaaS.Backend/Program.cs](SaaS.Backend/Program.cs)                             |
| [api-bff](../.docs/reference/patterns/catalog/api-bff.md)                                                             | Backend-for-Frontend pattern to handle auth and proxy requests            | [SaaS.Frontend/Program.cs](SaaS.Frontend/Program.cs)                            |
| [api-yarp-reverse-proxy](../.docs/reference/patterns/catalog/api-yarp-reverse-proxy.md)                               | YARP proxy for routing API calls to backend services                      | [SaaS.Frontend/Program.cs](SaaS.Frontend/Program.cs)                            |
| [dist-dotnet-aspire-orchestration](../.docs/reference/patterns/catalog/dist-dotnet-aspire-orchestration.md)           | .NET Aspire for service discovery and orchestration                       | [SaaS.AppHost/Program.cs](SaaS.AppHost/Program.cs)                             |
| [ui-interactiveauto-render-progression](../.docs/reference/patterns/catalog/ui-interactiveauto-render-progression.md) | Blazor InteractiveAuto for seamless SSR to WASM transition                | [SaaS.Frontend.Client/Program.cs](SaaS.Frontend.Client/Program.cs)              |

## Tech Stack
Key technologies enabling enterprise-grade authentication and distributed architecture.

- **[.NET 10.0 SDK (10.0.0)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0):** Core runtime for ASP.NET Core and Blazor applications.
- **[ASP.NET Core (10.0.1)](https://learn.microsoft.com/en-us/aspnet/core/):** Hosts the BFF and backend API endpoints.
- **[Blazor WebAssembly (10.0.1)](https://learn.microsoft.com/en-us/aspnet/core/blazor/):** Client-side UI with InteractiveAuto render mode.
- **[Entity Framework Core (10.0.0)](https://learn.microsoft.com/en-us/ef/core/):** Not used in this demo; data access is not the focus.
- **[Microsoft.Identity.Web (4.2.0)](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web):** Entra ID integration and OBO flow for downstream APIs.
- **[.NET Aspire (13.1.0)](https://learn.microsoft.com/en-us/dotnet/aspire/):** Orchestration and service discovery for distributed apps.
- **[YARP (2.3.0)](https://microsoft.github.io/reverse-proxy/):** Reverse proxy for API routing.

## Research & Documentation
Links to demo-specific research and architectural decisions.

- **Research Findings:** [.docs/251221-demo4.1-graph-downstreamapi-401-token-empty.md](.docs/251221-demo4.1-graph-downstreamapi-401-token-empty.md)
- **Implementation Plan:** [.docs/251221-demo4-refined-implementation-plan.md](.docs/251221-demo4-refined-implementation-plan.md)
- **ADRs:** [.docs/251221-demo4.1-retrospective-auth-and-aspire.md](.docs/251221-demo4.1-retrospective-auth-and-aspire.md)

## Architecture & Decisions
Technical overview of the refined Entra integration with distributed components.

### Diagram
```
[Frontend BFF (Blazor Server + YARP)]
    ↓ (OIDC Auth)
[Entra ID]
    ↓ (OBO Token)
[Backend API (Weather)]
    ↓ (Delegated Scopes)
[Microsoft Graph (User.Read)]
```

### Key Decisions
1. **YARP Proxy Integration:** Use the BFF to proxy `/api/*` requests to backend services, centralizing auth handling.
2. **.NET Aspire Orchestration:** Manage service discovery and configuration for the distributed setup.
3. **OBO Flow for Downstream APIs:** Exchange user tokens for API access with delegated permissions.

## What's New
Refinements from demo4, introducing distributed orchestration and proxy patterns.

- **Added .NET Aspire:** Orchestration for the app host and service defaults.
- **Integrated YARP Proxy:** BFF routes `/api/*` to backend services.
- **Enhanced Token Handling:** OBO flow for weather API and Microsoft Graph.

## Getting Started
Instructions to run and verify the demo.

### 1. Prerequisites
- Microsoft Entra tenant with admin access for app registrations.
- .NET 10.0 SDK installed.
- Two Entra app registrations:
  - **Backend API:** Expose scope `Weather.Get`.
  - **Frontend BFF:** Web app with redirect URIs for `https://localhost:7001/signin-oidc` and `https://localhost:7001/signout-callback-oidc`.

### 2. Execution
```powershell
cd demo4.1
dotnet run --project SaaS.AppHost --launch-profile https
```

### 3. Verification Steps
- [x] **Launch App:** Open the frontend endpoint from Aspire dashboard. - Expected: Login page loads.
- [x] **Authenticate:** Sign in with Entra credentials. - Expected: Redirect to `/weather`.
- [x] **Access Weather API:** Navigate to `/weather`. - Expected: Weather forecast data displays.

## Troubleshooting
Common issues and fixes specific to this demo.

- **401 Errors on API Calls:** Ensure OBO scopes are configured in user-secrets and admin consent is granted.
- **Sign-in Redirect Failures:** Confirm redirect URIs match the frontend launch settings.
- **Aspire Dashboard Missing:** Run with launch profile: `dotnet run --project SaaS.AppHost --launch-profile https`.

## What's Next?
This demo evolves into demo5, focusing on downstream API communication patterns and protected scopes.
