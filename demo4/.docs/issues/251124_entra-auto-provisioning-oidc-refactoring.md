# Auto-Provisioning Refactoring: ClaimsTransformation to OIDC Events

## Date
2025-11-24

## Context
Demo4 initially implemented Entra user auto-provisioning in `IClaimsTransformation.TransformAsync()` to handle user creation and role syncing during authentication.

## Problem
Claims transformation is designed for **read-only claim enrichment**, not database mutations. This approach violated separation of concerns and introduced:
- Race condition vulnerabilities on concurrent logins
- Limited error handling and rollback capabilities
- Side effects in a pipeline meant for claim processing
- Partial state if `AddLoginAsync()` failed after user creation

## Solution
Refactored auto-provisioning to the OIDC `OnTokenValidated` event with a dedicated `EntraUserProvisioningService`.

## Benefits
- ✅ Runs once during authentication (not on every request)
- ✅ Proper error handling with automatic rollback
- ✅ Clean separation: provisioning in auth events, permission loading in claims transformation
- ✅ Idempotency and race condition protection at database level
- ✅ Can fail authentication if provisioning fails (prevents incomplete state)
- ✅ Aligns with Microsoft's recommended patterns

## Implementation
- Moved logic to `OnTokenValidated` in `Program.cs`
- Created `IEntraUserProvisioningService` for user lifecycle operations
- Updated `PermissionClaimsTransformation` to focus only on claim enrichment

## Risks & Mitigations
- **Risk:** Provisioning failures block login.
- **Mitigation:** Graceful degradation and detailed logging.

## References
- Microsoft.Identity.Web documentation
- ASP.NET Core Identity best practices