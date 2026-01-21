# Issue: Graph OBO Loop on Entra Profile (MsalUiRequiredException user_null)

**Date:** 2026-01-21
**Severity:** High (blocks Entra Profile page)  
**Status:** Open  
**Scope:** demo4 (Demo4.EntraIntegration)

## Summary

The Entra Profile page (`/entra-profile`) triggers a **redirect loop** when calling Microsoft Graph via OBO. Server logs show `MsalUiRequiredException` with `ErrorCode: user_null`, indicating MSAL cannot find an account or login hint for silent token acquisition. This causes a repeated challenge to `MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect`, which returns to `/signin-oidc`, then loops back to `/entra-profile`.

## Verified Evidence (from 2026-01-20 interview + logs)

- Loop request sequence observed:
  1) `https://localhost:7210/entra-profile`  
  2) `https://localhost:7210/MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect&redirectUri=...`  
  3) `https://login.microsoftonline.com/.../authorize?...`  
  4) `https://localhost:7210/signin-oidc`  
  5) Redirect back to `/entra-profile` (loop repeats)

- No `/Account/ExternalLogin` request observed in the loop.
- Entra sign-in from Login page works for normal pages (non-Graph).
- Local user is created/updated with `EntraObjectId`.
- `AuthStateProbe` shows `oid` and `tid` after Entra login (outside the loop).
- BFF APIs work after Entra login.
- Entra button label is **OpenIdConnect** (single external provider).
- Observed cookie after `/signin-oidc`: **`.AspNetCore.Identity.Application` only** (no external cookie observed).

### Error log (key facts)

- Exception: `Microsoft.Identity.Client.MsalUiRequiredException`
- `ErrorCode: user_null`
- Message: “No account or login hint was passed to the AcquireTokenSilent call.”
- MSAL log: **0 cache accounts**
- Graph log shows `authType=Identity.Application`

## Interpretation (no assumptions)

The Graph call triggers OBO token acquisition. MSAL cannot resolve an account (or login hint) from cache, so it raises `user_null`. The client then triggers an interactive challenge. Because the underlying cache/account issue is not resolved, the challenge loop repeats.

## Next Verification Checklist (no code changes required)

1) **Confirm token cache population after `/signin-oidc`.**  
   Immediately after the OIDC callback, verify whether any MSAL account is present in cache.

2) **Confirm the principal used for token acquisition.**  
   Ensure the same claims shown in `GraphService` logs (`oid`, `tid`, `msal_account_id`, `preferred_username`, `login_hint`) are visible to the **token acquisition path** that calls `AcquireTokenSilent`.

3) **Confirm authentication scheme alignment.**  
   Verify the scheme name used by:
   - `AddMicrosoftIdentityWebApp(...)`
   - `OpenIdConnectDefaults.AuthenticationScheme`
   - `MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect`
   - `AuthenticationOptionsName` in `GraphService`

4) **Confirm challenge response header.**  
   On `/api/graph/profile` failure, check whether `x-ms-challenge-required: true` is returned and consistently handled by the client.

5) **Prevent loop during diagnosis.**  
   Temporarily avoid auto-challenge so you can inspect auth state or logs without being redirected repeatedly.

## Related Docs / Context

- Research report: `demo4/.docs/research/260120_multi_identity_validation.md`  
- Claims bridge reasoning: `demo4/.docs/research/260120_identity_entra_bridge_logic.md`  
- Profile page + Graph call: `demo4/Demo4.EntraIntegration.Client/Components/Pages/Profile.razor`  
- Graph service: `demo4/Demo4.EntraIntegration/Services/GraphService.cs`

