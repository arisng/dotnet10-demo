# Demo5.1 — `access_as_user` scope + local RBAC (Implementation Plan)

## Goal
Adopt a **single coarse OAuth scope** (`access_as_user`) as the consistent **API boundary gate** (“outer lock”) for `Demo5_1.ApiService`, while keeping **local RBAC permissions** as the **business authorization** (“inner lock”).

This plan intentionally supports the long-term target where a tenant can enable **multiple identity options** (Local Identity and Microsoft Entra ID) without changing the local RBAC model.

## Non-goals
- Model every business permission as an Entra scope.
- Implement Microsoft Graph integration (only consider how it fits).
- Implement full multi-tenant data isolation (tenant resolution is out of scope for this doc).

## Current state (as of 2026-01-09)
- `Demo5_1.Web` acquires and forwards an Entra access token via YARP, using `ApiService:Scopes` (currently `api://.../Forecast.Read`).
- `Demo5_1.ApiService` validates Entra bearer tokens via `AddMicrosoftIdentityWebApi(...)`.
- Business authorization is enforced via local RBAC policies (e.g., `reports.export`) backed by `PermissionClaimsTransformation`.
- `ApiService` does **not** enforce an OAuth scope gate today.

## Target state
### Outer lock (API boundary)
- `ApiService` requires **one scope**: `access_as_user` for inbound requests to `/api/*`.
- For Entra-issued tokens: check `scp` contains `access_as_user`.
- For first-party/local-issued tokens: check a **scope-like claim** using the same shape (MUST use `scp` for consistency).

### Inner lock (business authorization)
- Continue to enforce local RBAC permissions per endpoint (existing `PermissionRequirement` / `PermissionClaimsTransformation`).
- Policy composition: Create `RequireApiPermission(string)` extension to enforce both the "Outer Lock" scope and "Inner Lock" permission.

## Key design decisions
1. **Single inbound scope:** use `access_as_user` for *all* interactive calls to `ApiService`.
2. **Scopes stay coarse:** do not create per-module/per-feature scopes.
3. **Provider-agnostic RBAC:** roles/permissions remain local and apply regardless of identity provider.
4. **One API credential type (workshop direction):** prefer `Authorization: Bearer ...` for API calls.

## Naming guidance
- Entra scope: `access_as_user` (delegated permission).
- Local scope-like claim for first-party JWTs: reuse `scp` (space-separated) so one policy works for both issuers.

## Implementation steps (sequential)

### Phase 1 — Switch to `access_as_user` for Entra path (minimal change)

1. **Entra app registration (API)**
   - In the API app registration (“Expose an API”), add delegated scope: `access_as_user`.
   - Ensure the Web client app has permission to request it and consent is granted.

2. **Update Web configuration**
   - Change `Demo5_1.Web/appsettings.json`:
     - `ApiService:Scopes` becomes `[ "api://<api-client-id>/access_as_user" ]`.
   - Ensure `Demo5_1.Web/Program.cs` continues to request scopes from config (already does).

3. **Add a scope policy in ApiService**
   - Add an authorization policy (example name: `Api.Access`) requiring `access_as_user`.
   - Apply it to API route groups:
     - `/api/weather/*`, `/api/users/*`, `/api/reports/*`, `/api/identity/provision`.
   - Keep existing permission policies as-is for now.

4. **Verify behavior**
   - Requests without bearer token → 401.
   - Requests with bearer token but missing scope → 403.
   - Requests with scope but lacking RBAC permission → 403.

Deliverables:
- Updated `Demo5_1.Web/appsettings.json`
- Updated `Demo5_1.Web/README.md` instructions (if needed)
- Updated `Demo5_1.ApiService/Program.cs` (scope policy + enforcement)


### Phase 2 — Make scope enforcement issuer-agnostic (prepare for multi-identity)

5. **Normalize scope checks and compose policies**
   - Implement a small helper (or requirement) that:
     - Reads `scp` claim (space-separated scopes).
     - Treats missing claim as “no scope”.
   - Implement `RequireApiPermission(string permission)` extension:
     - This combines `RequireAuthorization("Api.Access")` with the specific local permission requirement.
   - Refactor `ApiService` endpoints to use this unified extension.

6. **Document outer/inner lock rule**
   - Outer lock: require `access_as_user` for `/api/*`.
   - Inner lock: require local permission claims for business actions.

7. **Simulate Tenant Context (Pragmatic Solution)**
   - Since full multi-tenant infrastructure is deferred, implement a simple middleware or header-based approach to simulate `tenantId` (e.g., look for `X-Tenant-Id` header or a specific claim in the token).

Deliverables:
- A reusable `RequireScope("access_as_user")` policy/extension.
- Tenant simulation mechanism.


### Phase 3 — Add local identity bearer tokens

7. **Implement local token issuer in ApiService**
   - `ApiService` exposes a `/api/identity/token` endpoint. 
   - It validates credentials against `ApplicationDbContext`.
   - On success, it issues a JWT signed with a developer key (for the workshop) or a Secure Secret.
   - Claims include: `sub`, `email`, `scp` = `access_as_user`, `idp` = `local`.

8. **Configure ApiService for Multi-Scheme Authentication**
   - Add a second `JwtBearer` scheme (e.g., `"LocalBearer"`) for local tokens.
   - Use a `ForwardDefaultSelector` in the default scheme options or `PolicyScheme` to choose between `MicrosoftIdentityWebApi` and `JwtBearer` based on the token's `iss` (issuer).
   - Ensure `PermissionClaimsTransformation` handles both `oid` (for Entra) and `sub` (for Local) correctly.

9. **Web integration (Token Handoff)**
   - Abstract token acquisition in the `Web` project (e.g., `IApiTokenProvider`).
   - Implementation for Entra: Wraps `ITokenAcquisition.GetAccessTokenForUserAsync`.
   - Implementation for Local: Retrieves the local bearer token stored in the user's session/cookie.
   - Update YARP transform to use `IApiTokenProvider.GetTokenAsync()` regardless of the sign-in method.

10. **Refine User Provisioning**
    - Update `/api/identity/provision` in `ApiService`:
      - If it's an Entra user (has `oid`), proceed with traditional provisioning.
      - If it's a local user (already present in the DB that issued the token), just return `Ok()`.

Deliverables:
- Local token issuer endpoint in `ApiService`.
- Multi-scheme Auth configuration in `ApiService`.
- `IApiTokenProvider` abstraction and implementations in `Web`.
- Updated YARP transform logic.


## Security checklist
- Enforce scope gate on **all** `/api/*` endpoints (including provisioning).
- Keep scopes coarse; enforce business rights via local RBAC.
- Ensure forwarded headers cannot spoof user identity (do not accept user-id headers from untrusted callers).
- Keep token lifetimes short; use refresh strategy as needed.

## Testing/validation checklist
- Manual:
  - Call `/api/weather` with:
    - no token → 401
    - token missing `scp` or without `access_as_user` → 403
    - valid token + scope + permission → 200
- Verify `PersistingServerAuthenticationStateProvider` continues to function (permission claims present after provisioning).

## Open questions (Answered)
- **Where does tenant context come from for RBAC checks?**
  - Simulated for now (e.g., via header or claim). Full infrastructure deferred to next demo project.
- **Do we want `scp` only, or support both `scp` + `scope` for first-party tokens?**
  - Only `scp`.
- **Do we need app-only access (daemon jobs)?**
  - Defer to near future.
