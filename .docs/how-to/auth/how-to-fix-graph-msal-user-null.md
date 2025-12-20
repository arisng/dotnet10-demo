# How to Fix Microsoft Graph `MsalUiRequiredException (user_null)` (demo4)

## Problem

You see an error similar to:

- `MsalUiRequiredException: No account or login hint was passed to the AcquireTokenSilent call.`
- `ErrorCode: user_null`

This happens when Microsoft.Identity.Web tries to acquire a token “for user”, but the current request is **not** an Entra-authenticated user (often a local Identity cookie user).

## Fix

### 1) Gate Graph endpoints to Entra users

Require an authorization policy that checks for Entra markers (`oid` + `tid`) on the principal.

In demo4, the policy is `entra.user` and Graph endpoints require it.

### 2) Make the client treat 401/403/404 as “no Graph data”

Even with correct gating, the UI should not crash when Graph endpoints are unavailable.

Update the client Graph service to return `null` on `401/403/404` instead of throwing.

### 3) Ensure authentication middleware exists

If you register authentication/authorization services, the pipeline must include:

- `app.UseAuthentication();`
- `app.UseAuthorization();`

Otherwise, `HttpContext.User` won’t consistently reflect the incoming cookie, and auth-dependent flows (policies, downstream token acquisition) behave unpredictably.

## Verify

- Log in as a local Identity user → Graph endpoints should be blocked; no `user_null` logs.
- Log in as an Entra user → Graph endpoints succeed (or return 404 for missing photo).

## Related implementation files

- Policy + endpoints: `demo4/Demo4.EntraIntegration/Program.cs`
- Server Graph service: `demo4/Demo4.EntraIntegration/Services/GraphService.cs`
- Client Graph service: `demo4/Demo4.EntraIntegration.Client/Services/ClientServices.cs`
