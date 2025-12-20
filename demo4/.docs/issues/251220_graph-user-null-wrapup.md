# Demo4 — Graph `user_null` (AcquireTokenSilent) wrap-up

Date: 2025-12-20

## Context

Demo4 supports **two auth modes**:
- Local auth via ASP.NET Core Identity cookie (`Identity.Application`)
- Entra ID auth via Microsoft.Identity.Web OIDC scheme (`MicrosoftEntra`)

Microsoft Graph calls are done server-side via Microsoft.Identity.Web / `IDownstreamApi` (OBO).

## Original failure

When calling Graph endpoints (profile/photo), the server threw:
- `MicrosoftIdentityWebChallengeUserException` wrapping
- `MsalUiRequiredException: user_null` (“No account or login hint was passed to AcquireTokenSilent”)

MIW logs showed token acquisition context missing user identity hints:
- `LoginHint = False`
- `HomeAccountId = False`

## Root causes identified

1. **Graph endpoints were accessible to local-only sessions**
   - Local Identity users cannot produce Entra user tokens for Graph OBO.

2. **Scheme inference bug (`IDW10503`)**
   - MIW inferred `Identity.Application` instead of `MicrosoftEntra`.

3. **Principal lacked MSAL account identification hints**
   - Cookie principal didn’t reliably include values MIW/MSAL uses for `AcquireTokenSilent` (login hint / account id).

## Fixes implemented

### 1) Entra-only gating for Graph

- Added policy `entra.user` requiring `oid` and `tid`.
- Required this policy for `/api/graph/*`.

Outcome: local-only authenticated users should no longer reach Graph token acquisition.

### 2) Explicit scheme selection for token acquisition

- Forced token acquisition to use Entra:
  - `AcquireTokenOptions.AuthenticationOptionsName = "MicrosoftEntra"`

Outcome: avoids MIW inferring `Identity.Application` and fixes `IDW10503` path.

### 3) Claim enrichment for token acquisition hints

Goal: Ensure MIW can identify a user account for `AcquireTokenSilent`.

Changes:
- In Entra `OnTokenValidated`, add claims before the principal is stored in the auth cookie:
  - `preferred_username`
  - `login_hint` (alias)
  - `msal_account_id` (and legacy URI form)
  - `uid`/`utid` approximations for demo purposes
- In per-request `PermissionClaimsTransformation`, enrich the request principal with:
  - `tid`, `preferred_username`, `msal_account_id` (fallback where needed)
- Persist durable Entra hints onto the local Identity user record during provisioning:
  - `auth_provider=entra`, `oid`, `tid`, `preferred_username`, `msal_account_id`

### 4) Convert UI-required exceptions into interactive challenges

- Graph minimal API endpoints catch `MicrosoftIdentityWebChallengeUserException`.
- Invoke MIW consent/CA handler to issue an interactive challenge instead of returning a hard failure.

### 5) Client-side resiliency

- Client Graph service tolerates 401/403/404 and returns `null` (prevents UI breakage).

### 6) Diagnostics

- `/auth-state-probe` page displays relevant token-acquisition hints:
  - `login_hint`, `preferred_username`, `msal_account_id`, `uid`, `utid`, etc.
- `GraphService` logs a “token context” line at Info level.

## Files touched (high-level)

- demo4/Demo4.EntraIntegration/Program.cs
- demo4/Demo4.EntraIntegration/Services/GraphService.cs
- demo4/Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs
- demo4/Demo4.EntraIntegration/Services/EntraUserProvisioningService.cs
- demo4/Demo4.EntraIntegration.Client/Components/Pages/AuthStateProbe.razor
- Root `.docs/` docs set (previous)

## What is still unverified

We did **not** complete the end-to-end runtime verification that:
- MIW logs now show `LoginHint=True` and/or `HomeAccountId=True` after Entra sign-in.
- `/api/graph/profile` and `/api/graph/profile/photo` succeed for Entra users.

## Operational blocker observed

While attempting to validate, the server process frequently exited quickly (exit code 1 / immediate shutdown).
- Build succeeded, but the host wasn’t reachable during probe scripts.
- This looks like a dev-loop/run orchestration problem (or something stopping the host), not compilation.

## Follow-up checklist (next session)

1. Run demo4 with the intended launch profile and keep it alive:
   - `dotnet run --project demo4/Demo4.EntraIntegration/Demo4.EntraIntegration.csproj --launch-profile https-dev`
2. In browser:
   - Sign in with Entra.
   - Visit `/auth-state-probe` and confirm `login_hint` and/or `msal_account_id` is populated.
   - Call `/api/graph/profile` and `/api/graph/profile/photo`.
3. In logs:
   - Confirm MIW token request shows `LoginHint=True` and/or `HomeAccountId=True`.
   - If still false, use the `GraphService` “token context” line to see which hint is missing and adjust claim mapping.
4. Triage the “host exits immediately” issue first if endpoints are not reachable.

## Success criteria

- Local Identity users:
  - Cannot call `/api/graph/*` (blocked by `entra.user`).
- Entra users:
  - Graph endpoints succeed OR trigger a clean interactive challenge.
  - No `user_null` from `AcquireTokenSilent` after fresh Entra sign-in.
