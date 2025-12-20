# Your First Entra → Graph Call (demo4)

This tutorial walks you through validating that demo4 can:

- Sign in with Microsoft Entra ID
- Call Microsoft Graph on behalf of the signed-in user
- Survive the Blazor `InteractiveAuto` SSR → WASM handoff with the correct Entra markers (`oid`/`tid`) available in the browser

## Prerequisites

- Demo4 configured with working Entra app registration (tenant, client id/secret)
- Demo4 database migrated/created

## Steps

1. Start demo4.

2. Navigate to the authentication diagnostics page:

   - `/auth-state-probe`

3. Sign in with **Microsoft Entra ID**.

4. In the Auth State timeline output, verify all of the following are present after sign-in:

   - `ProviderClaim(auth_provider): entra`
   - `oid = ...`
   - `tid = ...`
   - `Policy entra.user satisfied: True`

5. Validate Graph calls:

   - The UI should load the Graph profile (`/api/graph/profile`) and profile photo (`/api/graph/profile/photo`).
   - If a photo is not set in your tenant, the photo endpoint may return 404. That should be treated as “no photo”, not an application failure.

## Expected results

- Entra users can call Graph endpoints.
- Local Identity users (passkey/password) do not call Graph endpoints.
- No MSAL `user_null` errors appear in logs.

## Troubleshooting

- If you see `MsalUiRequiredException` with `user_null`:
  - This indicates a Graph “for user” call happened without a valid Entra user context.
  - Confirm Graph endpoints require the `entra.user` policy (server).

- If you see `IDW10502` / `MicrosoftIdentityWebChallengeUserException`:
  - This usually indicates incremental consent or Conditional Access (claims challenge). Treat this as a separate scenario from `user_null`.

## Related

- Server auth pipeline and policies: `demo4/Demo4.EntraIntegration/Program.cs`
- Auth state persistence (SSR → WASM):
  - `demo4/Demo4.EntraIntegration/Authorization/PersistingServerAuthenticationStateProvider.cs`
  - `demo4/Demo4.EntraIntegration.Client/Services/PersistentAuthenticationStateProvider.cs`
