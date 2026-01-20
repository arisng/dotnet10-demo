# Demo4 Implementation & Patterns

This document provides a concise overview of the architectural decisions, implementation details, and patterns used in Demo4's Entra ID integration.

## Implementation Overview
Demo4 implements dual authentication (local Identity + Microsoft Entra ID) via a BFF (Backend-for-Frontend) cookie boundary.

- **Dual-Mode Auth**: Entra logins use `AddMicrosoftIdentityWebApp()` with token acquisition, sharing the same permission pipeline as local users.
- **Enhanced User Profile**: `ApplicationUser` includes Entra-specific fields (`EntraObjectId`, `DisplayName`, `JobTitle`, etc.) linked via the `oid` claim.
- **Graph Integration**: `IGraphService` handles Microsoft Graph calls via the On-Behalf-Of (OBO) flow.
- **Provisioning**: `PermissionClaimsTransformation` enriches both account types and handles just-in-time provisioning for Entra users.
- **Diagnostics**: An `AuthStateSurface` component provides real-time visibility into claims, providers, and permissions.

## Key Patterns & Hardening
| Pattern                      | Implementation Detail                                                                               |
| :--------------------------- | :-------------------------------------------------------------------------------------------------- |
| **Distributed Token Cache**  | Prevents token loss and scales. Production requires switching to Redis/SQL and enabling encryption. |
| **Auth State Serialization** | Guarantees permission claims flow to WASM via `AddAuthenticationSerialization`.                     |
| **Fluent Auth Builder**      | Uses the .NET 10 `.AddAuthorizationBuilder()` for cleaner policy registration.                      |
| **Microsoft Graph + OBO**    | Securely fetches profile data without exposing access tokens to the client.                         |
| **Claims Transformation**    | Unifies local and Entra users into a single RBAC system with a `permission_transformed` guard.      |
| **Observability**            | Captures `aspnetcore.authentication` and `authorization` metrics via OpenTelemetry.                 |

## Essential Files
- **`Program.cs`**: Main configuration for Entra, OBO, caches, and metrics.
- **`Authorization/PermissionClaimsTransformation.cs`**: Claims enrichment and user provisioning logic.
- **`Demo4.EntraIntegration/Data/ApplicationUser.cs`**: Extended data model for Entra/Graph metadata.
- **`Services/GraphService.cs`**: Wrapper for `IDownstreamApi` Graph calls.
- **`Components/Diagnostics/AuthStateSurface.razor`**: UI for verifying auth state and claims.

## Testing & Verification
- **Migrations**: Apply `AddEntraIntegration` via `dotnet ef database update`.
- **Matrix**: Test local passkeys, Entra first-time login, Graph OBO calls, and permission enforcement (e.g., `weather.read`).
- **Telemetry**: Verify `aspnetcore.authentication.*` counters in logs after sign-in.

## Next Steps & Limitations
- **OIDC Events**: Move provisioning from `IClaimsTransformation` to `OnTokenValidated` for better separation.
- **Caching**: Transition from in-memory to distributed cache for multi-node deployments.
- **Account Linking**: Implement logic to link local and Entra accounts with identical emails.
- **Sync**: Add background jobs for periodic Graph profile synchronization.
