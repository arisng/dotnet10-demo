# How to Support Local Identity + Microsoft Entra ID in One App

## Goal

Let users choose between:

- Local Identity (password/passkey)
- Microsoft Entra ID (OIDC)

…and let the app reliably determine which provider is in use.

## Recommended pattern

### 1) Provide explicit entrypoints per login method

- Local Identity: use Identity’s built-in endpoints/UI (`/Account/*`).
- Entra: provide a login action/endpoint that issues a challenge for the Entra scheme.

This avoids confusion with “default challenge scheme” when multiple handlers exist.

### 2) Detect provider by claims (and optionally a durable marker)

- Entra users: presence of `oid` and `tid` claims is the simplest detection.
- Optionally add `auth_provider` = `entra|local` for a single, durable marker.

### 3) Persist provider markers across SSR → WASM handoff (Blazor InteractiveAuto)

If the app uses `InteractiveAuto`, persist the provider markers into `PersistentComponentState` and rehydrate them into the WASM `AuthenticationState`.

## Demo4 notes

- Demo4 uses ASP.NET Core Identity cookie as the app’s primary session. The OIDC principal seen during `OnTokenValidated` can contain more claims than the later cookie principal.
- If the app needs `oid`/`tid` later (policies, UI), persist them as Identity user claims.

## Related

- Shared persisted user model: `demo4/Demo4.EntraIntegration.Shared/Models/UserInfo.cs`
- Server persistence: `demo4/Demo4.EntraIntegration/Authorization/PersistingServerAuthenticationStateProvider.cs`
- WASM rehydration: `demo4/Demo4.EntraIntegration.Client/Services/PersistentAuthenticationStateProvider.cs`
