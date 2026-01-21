# Demo4 – Microsoft Entra ID Integration + Claims Mapping

[[Home](../README.md) > **Demo 4**]

## Goal

Add Microsoft Entra ID as an external identity provider alongside local passkey authentication, supporting a hybrid identity scenario where B2C customers use passkeys while employees authenticate via Entra ID. Implement automatic role mapping based on Entra ID App Roles to enable centralized permission management. Demonstrate how the On-Behalf-Of (OBO) flow enables server-side Microsoft Graph API calls while preserving the existing permission-based authorization system.

## Patterns Selected (Catalog)

| Pattern                                                                                                                                         | Why Here                                                                    | Evidence                                                                                                                                                                                                           |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **OpenID Connect (OIDC)** — [auth-oidc-external-provider](../.docs/reference/patterns/catalog/auth-oidc-external-provider.md)                   | Add an external IdP alongside passkeys for a hybrid identity model.         | `AddMicrosoftIdentityWebApp()` wiring and OIDC callbacks in [demo4/Demo4.EntraIntegration/Program.cs](Demo4.EntraIntegration/Program.cs).                                                                          |
| **Auto-Provisioning** — [authz-auto-provisioning](../.docs/reference/patterns/catalog/authz-auto-provisioning.md)                               | Create and sync local users on first Entra sign-in.                         | `IEntraUserProvisioningService` invoked from `OnTokenValidated` in [demo4/Demo4.EntraIntegration/Program.cs](Demo4.EntraIntegration/Program.cs).                                                                   |
| **Claims Mapping** — [authz-claims-mapping](../.docs/reference/patterns/catalog/authz-claims-mapping.md)                                        | Translate Entra App Roles into local roles and permissions.                 | `PermissionClaimsTransformation` and role mapping logic in [demo4/Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs](Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs). |
| **Multi-Identity** — [auth-multi-identity](../.docs/reference/patterns/catalog/auth-multi-identity.md)                                          | Offer local passkey auth and Entra ID side by side.                         | Dual login options with a unified permission pipeline in `demo4/Demo4.EntraIntegration`.                                                                                                                           |

## Tech Stack

- **.NET 10.0 SDK (10.0.0):** Core framework for building the Blazor Web App and APIs.
- **ASP.NET Core (10.0.0):** For web framework, Identity, and Minimal APIs.
- **ASP.NET Core Identity (10.0.0):** For passkey support, user management, and hybrid authentication using `IdentitySchemaVersions.Version3`.
- **Blazor WebAssembly (10.0.0):** For client-side interactivity.
- **Entity Framework Core (10.0.0):** For database operations, migrations, and identity data persistence.
- **Microsoft.Identity.Web (4.1.0):** For Entra ID integration, OIDC authentication, and OBO flow to Microsoft Graph.
- **Microsoft Graph API:** For server-side fetching of user profiles and photos via the On-Behalf-Of (OBO) flow.

## Research & Documentation

- **Research Findings:** [.docs/research/RESEARCH_FINDINGS.md](.docs/research/RESEARCH_FINDINGS.md)
- **Auto-Provisioning Research:** [.docs/research/AUTO_PROVISIONING_RESEARCH.md](.docs/research/AUTO_PROVISIONING_RESEARCH.md)
- **Architecture Diagrams:** [.docs/research/architecture-c4-model-diagrams.md](.docs/research/architecture-c4-model-diagrams.md)
- **CORS Fix (API-to-Navigation Handoff):** [.docs/research/260120_api_to_navigation_handoff.md](.docs/research/260120_api_to_navigation_handoff.md)
- **Claims Bridge (OBO user_null Fix):** [.docs/research/260120_identity_entra_bridge_logic.md](.docs/research/260120_identity_entra_bridge_logic.md)
- **Multi-Identity Re-Validation:** [.docs/research/260120_multi_identity_validation.md](.docs/research/260120_multi_identity_validation.md)
- **ADR:** [Auto-Provisioning Refactoring](../.docs/issues/251124_entra-auto-provisioning-oidc-refactoring.md) (Note: Moved from root issues to demo-specific context).

## Architecture & Decisions

Demo4 transforms the monolithic Blazor Web App to support **dual authentication sources** while maintaining unified authorization:

### Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Web App                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Authentication Layer (Hybrid)                      │    │
│  │  • Local Identity (Passkeys) ──┐                    │    │
│  │  • Microsoft Entra ID ─────────┼─→ Claims Principal │    │
│  └────────────────────────────────┴────────────────────┘    │
│                         ↓                                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Authorization Layer (Unified)                       │   │
│  │  • IClaimsTransformation                             │   │
│  │  • Permission-Based Policies (from demo3)            │   │
│  └──────────────────────────────────────────────────────┘   │
│                         ↓                                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  BFF APIs                                            │   │
│  │  /api/weather, /api/users, /api/reports              │   │
│  │  (Cookie-based, no bearer tokens)                    │   │
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

### Key Decisions
1. **Cookie Authentication for BFF APIs:** Both local and Entra users authenticate via cookies. No bearer tokens sent to BFF endpoints.
2. **OBO Flow for Graph API:** Server-side code exchanges the Entra token for a downstream token to call Microsoft Graph.
3. **Unified Authorization:** Both identity sources flow through the same `IClaimsTransformation` → permission system from demo3.
4. **State Serialization:** `AddAuthenticationStateSerialization()` passes Entra identity to WASM client without exposing access tokens.

## What's New

- **Microsoft Entra ID Authentication:** Added OIDC integration with `Microsoft.Identity.Web`.
- **Microsoft Graph Integration:** Implemented OBO flow for server-side user profile and photo fetching.
- **Secure State Serialization:** Enabled `AddAuthenticationStateSerialization()` to pass Entra identity to WASM without exposing tokens.
- **Hybrid Identity Data Model:** Extended `ApplicationUser` with Entra fields (e.g., `EntraObjectId`, `DisplayName`).
- **Auto-Provisioning Service:** Created `IEntraUserProvisioningService` for user creation and role syncing in OIDC events.
- **Enhanced Diagnostics:** Updated `AuthStateProbe` to show authentication provider, Entra claims, and Graph data.
- **Entra ID Claims Mapping:** Mapped App Roles to local roles/permissions.
- **Identity-Entra Claims Bridge:** Resolved infinite redirect loops and `user_null` errors by enriching Identity principals with OIDC tokens hints.
- **WASM CORS Resolution:** Implemented the "API-to-Navigation Handoff" pattern to support interactive challenges in InteractiveAuto mode without CORS errors.

## Getting Started

### 1. Prerequisites
- **Completed:** demo3 (BFF APIs + Permission-Based RBAC)
- **.NET 10 SDK** (Preview) with EF Core tools installed
- **Azure Entra ID Tenant** with app registration. See [Azure Entra ID Setup](./.docs/guidance/setup-guide.md) for detailed steps.

### 2. Execution
```powershell
# Apply migrations
cd demo4/Demo4.EntraIntegration/Demo4.EntraIntegration
dotnet ef database update

# Run
dotnet watch
```

### 3. Verification Steps
- [x] **Local Login:** Sign in with a passkey. Expected: `AuthStateProbe` shows "Local (Passkey)".
- [x] **Entra Login:** Click "Sign in with Microsoft". Expected: Successful OIDC flow and auto-provisioning.
- [x] **Graph Data:** Navigate to `/auth-state-probe`. Expected: Graph data (Job Title, Photo) is visible for Entra users.
- [x] **Permissions:** Assign a local role to the Entra user. Expected: Permission claims appear in probe.

## Troubleshooting

See [Troubleshooting](../.docs/support/troubleshooting.md) for common issues and fixes.

### Outstanding Issues (TODO)

- **Graph/OBO Loop (TODO):** Entra Profile page can enter a redirect loop when Graph OBO token acquisition fails (`user_null`). Tracked in [.docs/issues/260121_graph-obo-loop-user-null.md](./.docs/issues/260121_graph-obo-loop-user-null.md).

## What's Next?

**Demo5** introduces a separate downstream API service and contrasts two security patterns: BFF (Cookie) vs. Downstream API (Bearer Token).
