# Demo4 Implementation Patterns

This guide captures the architectural and operational decisions behind Demo4’s Entra ID work. Keep it in sync with the `research/` notes so the evidence behind each pattern is just a click away.

## Pattern Map
| Pattern                              | Why It Matters                                                       | Proof Point                                                                                                |
| ------------------------------------ | -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Distributed Token Cache              | Prevents token loss across restarts and scales to multiple instances | `research/microsoft-identity-web.md` → Token cache section, `guidance/implementation-summary.md` migration list |
| Authentication State Serialization   | Guarantees permission claims flow to WASM components                 | `guidance/implementation-summary.md` → authentication section                                              |
| Fluent Authorization Builder         | Uses .NET 10 fluent API for permission policies                      | `research/security-and-metrics.md` → `.AddAuthorizationBuilder()` description                                 |
| Microsoft Graph + OBO                | Feeds profile data and claims enrichment without exposing tokens     | `research/AUTO_PROVISIONING_RESEARCH.md` + `guidance/implementation-summary.md` graph service section      |
| Claims Transformation & Provisioning | Ensures both local and Entra users hit the same RBAC system          | `research/AUTO_PROVISIONING_RESEARCH.md` deep-dive                                                         |
| Observability Metrics                | Captures auth/authz metrics for production debugging                 | `research/security-and-metrics.md` metrics section                                                            |

## Production Hardening Essentials
1. **Token cache security** – switch `AddInMemoryTokenCaches()` to `AddDistributedTokenCaches()` backed by Redis or SQL Server and enable `MsalDistributedTokenCacheAdapterOptions.Encrypt`. Note this also requires a shared Data Protection key ring (see `reference/quick-reference.md`).
2. **Credential hygiene** – move the client secret out of checked-in JSON and into User Secrets/Key Vault. In development, keep `dotnet user-secrets` commands handy (see `reference/quick-reference.md`).
3. **Claims serialization** – call `AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true)` on the server and `AddAuthenticationStateDeserialization()` on the client so permissions travel with the Blazor circuit.
4. **Authorization builder** – register all permission policies through `AddAuthorizationBuilder()` to keep the policy graph readable and testable.
5. **Cookie & OIDC tweaks** – enforce `HttpOnly`, `SameSite=Lax`, and `SecurePolicy=Always` while disabling inbound claim mapping (`options.MapInboundClaims = false`) so the raw `oid` and `sub` claims remain intact.

## Graph & User Provisioning
- **Graph service**: `IGraphService`/`GraphService` wrap `IDownstreamApi` so every call goes through the OBO flow defined in `Program.cs`. Log failures at WARN instead of letting the login fail.
- **Permission claims**: `PermissionClaimsTransformation` enriches both local and Entra accounts by loading permissions from `IPermissionService`. Guard transformation with a `permission_transformed` sentinel and avoid running provisioning logic twice per request.
- **OID consistency**: always link external logins using the `oid` claim (not `sub`). The `AUTO_PROVISIONING_RESEARCH.md` diagnostic proves why mismatched ProviderKeys break multi-tenant flows.
- **Profile sync**: after provisioning, update the local `ApplicationUser` (DisplayName, JobTitle, Department, MobilePhone, LastGraphSync) and record success/failure in the logs so the troubleshooting playbook can surface issues fast.

## Observability & Testing Notes
- **Metrics**: register OpenTelemetry meters for `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authorization`, and `Microsoft.AspNetCore.Identity` to capture counters listed in `research/security-and-metrics.md`.
- **Telemetry**: surface authentication/authorization events via console exporter in development and point production at OTLP/Exporter of choice.
- **Test matrix**:
  - Local enrolment + passkey
  - Entra login (first time + returning)
  - Graph profile and photo calls (handle 404s)
  - Permission enforcement (weather/users/reports APIs)
  - Auth proto metrics (sign-in, challenge, policy evaluation)
- **Verification**: every change should include checklist updates, database migration notes, and a reminder in `guidance/implementation-summary.md` so reviewers know what shifted.

## What to Capture in This Guide
- Link any new pattern to the catalog entry under `.docs/reference/patterns/`.
- Add summaries and links back to `research/` files whenever you expand a section.
- Mention the relevant project files (e.g., `Demo4.EntraIntegration/Program.cs`, `Authorization/PermissionClaimsTransformation.cs`, `Services/GraphService.cs`) under a `Related files` subheading for quick navigation.
