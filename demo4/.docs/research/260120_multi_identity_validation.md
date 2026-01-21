# Research: Demo4 Multi-Identity Re-Validation (Local Passkey + Entra ID)

**Date:** 2026-01-20  
**Scope:** demo4 (Demo4.EntraIntegration)  
**Goal:** Re-validate the Multi-Identity implementation against current official documentation; no code changes.

## Summary

The demo4 Multi-Identity implementation (local passkey + Microsoft Entra ID) aligns with current official guidance for:

- ASP.NET Core Identity external providers (multiple providers + explicit login choice)
- Microsoft.Identity.Web OIDC integration
- OBO (On-Behalf-Of) downstream API calls via `IDownstreamApi`
- Blazor Web App InteractiveAuto auth-state persistence
- Claims transformation for unified authorization

One behavior to **verify** (not fix here): the Entra login path should be consistent with the chosen external login flow, because the Identity external login UI relies on the external cookie scheme while demo4 uses OIDC events to bridge directly into the Identity application cookie.

## Evidence (demo4 files)

- OIDC integration + token acquisition: `demo4/Demo4.EntraIntegration/Program.cs`
- Auto-provisioning at OIDC event: `demo4/Demo4.EntraIntegration/Program.cs`
- Entra user provisioning: `demo4/Demo4.EntraIntegration/Services/EntraUserProvisioningService.cs`
- Unified permissions via claims transformation: `demo4/Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs`
- Blazor auth-state persistence (SSR → WASM):  
  `demo4/Demo4.EntraIntegration/Authorization/PersistingServerAuthenticationStateProvider.cs`  
  `demo4/Demo4.EntraIntegration.Client/Services/PersistentAuthenticationStateProvider.cs`
- External login UI and endpoints:  
  `demo4/Demo4.EntraIntegration/Components/Account/Pages/Login.razor`  
  `demo4/Demo4.EntraIntegration/Components/Account/Shared/ExternalLoginPicker.razor`  
  `demo4/Demo4.EntraIntegration/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
- Graph OBO calls: `demo4/Demo4.EntraIntegration/Services/GraphService.cs`

## Findings (Validation)

### ✅ Aligned with official guidance

1. **Multiple provider entrypoints**  
   The UI provides explicit local login and external provider selection, matching the recommended Identity external provider flow.

2. **Entra OIDC setup**  
   `AddMicrosoftIdentityWebApp()` is the correct entry point for Entra ID web app authentication, with downstream API token acquisition enabled.

3. **Auto-provisioning in OIDC events**  
   User provisioning is performed in `OnTokenValidated`, which is the appropriate place for per-login initialization and claim adjustments.

4. **Unified authorization**  
   `IClaimsTransformation` enriches the principal with app permissions, keeping local and Entra users in a shared authorization pipeline.

5. **InteractiveAuto auth-state persistence**  
   Server → client authentication state persistence uses `PersistentComponentState`, aligning with Blazor Web App guidance for InteractiveAuto.

6. **OBO / Graph integration**  
   `IDownstreamApi.GetForUserAsync` usage follows the recommended OBO pattern.

### ⚠️ Verify (design consistency — explicit checks)

The goal here is **not** to change anything, but to **explicitly verify** how the Entra flow behaves in demo4 so the design is unambiguous and documented. The checks below avoid assumptions and only describe what to confirm.

**A) Which Entra entrypoint is actually used in the running app?**  
Confirm which UI action triggers the Entra sign-in challenge:

- The login page renders external providers via `ExternalLoginPicker.razor` and posts to `/Account/PerformExternalLogin`.  
  Files:  
  - `demo4/Demo4.EntraIntegration/Components/Account/Pages/Login.razor`  
  - `demo4/Demo4.EntraIntegration/Components/Account/Shared/ExternalLoginPicker.razor`  
  - `demo4/Demo4.EntraIntegration/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
- Some pages (e.g., Profile) use the Microsoft Identity UI challenge endpoint directly:  
  `MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect`  
  File: `demo4/Demo4.EntraIntegration.Client/Components/Pages/Profile.razor`

**Explicit verification questions:**
1. In normal sign-in (Login page), is Entra initiated via `/Account/PerformExternalLogin` or via `MicrosoftIdentity/Account/Challenge`?  
2. If both are used, are they equivalent in how they establish the final application cookie?

**B) Which cookie does OIDC sign-in actually issue?**  
In `Program.cs`, `OpenIdConnectOptions.SignInScheme = IdentityConstants.ApplicationScheme`. This means OIDC signs directly into the **Identity application cookie**, not the external cookie.

**Explicit verification questions:**
1. After Entra sign-in, does the request pipeline contain an **external cookie** at any point, or only the application cookie?  
2. If only the application cookie is used, is the external-login UI (`ExternalLogin.razor`) still part of the flow, or is it bypassed entirely?  

**Why this matters (doc‑level clarity):**  
The standard Identity external login flow relies on the external cookie + `ExternalLoginInfo`. If demo4 intentionally bypasses that by signing directly into the application cookie, document this explicitly as the chosen design.

**C) Is `ExternalLogin.razor` reachable for Entra users?**  
`ExternalLogin.razor` expects `SignInManager.GetExternalLoginInfoAsync()` to succeed. That typically relies on the external cookie (IdentityConstants.ExternalScheme).

**Explicit verification questions:**
1. When Entra is initiated via `/Account/PerformExternalLogin`, does the flow ever hit `/Account/ExternalLogin`?  
2. If it does, is `externalLoginInfo` non-null?  
3. If it does not, is that because OIDC events are doing provisioning and issuing the final cookie instead?

**D) Is the Entra provisioning path the single source of truth?**  
Provisioning is done in `OnTokenValidated` and in `EntraUserProvisioningService`.

**Explicit verification questions:**
1. Is **all** Entra user creation handled by `OnTokenValidated` + `IEntraUserProvisioningService` (and never by `ExternalLogin.razor`)?  
2. If both can run, which path wins and is it deterministic?

**E) Authentication scheme naming consistency**  
You use `OpenIdConnectDefaults.AuthenticationScheme` and label the provider "Microsoft Entra ID" in user logins. Ensure the scheme names match the provider list returned by `GetExternalAuthenticationSchemesAsync()`.

**Explicit verification questions:**
1. Does `GetExternalAuthenticationSchemesAsync()` return the same scheme name as used in `MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect`?  
2. Does the provider list show exactly one Entra provider, and is it the one expected by `PerformExternalLogin`?

**F) Documentation decision (explicit statement)**  
After verifying A–E, document one of the following **explicitly** in this report or README:

- **Option 1:** “Entra uses Identity’s external login UI flow, and external cookies are used.”  
or  
- **Option 2:** “Entra bypasses external login UI and signs directly into the Identity application cookie via OIDC events.”

This is a **design clarity check**, not a required change.

## Verification Interview (2026-01-20)

This section records the interview-style verification answers so the flow is explicit and reproducible.

### Answers (verbatim summary)

1. Entra sign-in from Login uses `/Account/PerformExternalLogin`: **Yes**  
2. Challenge endpoint used during Graph failure: **Yes** (`MicrosoftIdentity/Account/Challenge`)  
3. `/Account/ExternalLogin` observed in the loop: **No**  
4. Cookie seen after `/signin-oidc`: **.AspNetCore.Identity.Application only**  
5. External cookie observed: **No / unknown**  
6. Loop flow type: **B** (Challenge -> `/signin-oidc`)  
7. Entra login works for normal pages (non-Graph): **Yes**  
8. Local user created/updated with `EntraObjectId`: **Yes**  
9. `oid`/`tid` claims visible in AuthStateProbe after Entra login: **Yes**  
10. AuthStateProbe during the loop: **Unknown** (blocked by infinite loop)  
11. Avoiding Profile page avoids the loop: **Yes**  
12. BFF APIs succeed after Entra login: **Yes**  
13. Entra provisioning source: **A** (OnTokenValidated only)  
14. `x-ms-challenge-required` header observed: **Unknown**  
15. Entra button present: **Yes**  
16. Entra button label: **OpenIdConnect**  
17. Server error during loop: **MsalUiRequiredException (user_null)**  
18. authType in loop logs: **Identity.Application**  

### Error log excerpt (key facts)

- `MsalUiRequiredException` with `ErrorCode: user_null`  
- Message: "No account or login hint was passed to the AcquireTokenSilent call."  
- Token cache shows **0 accounts**  
- Log shows `authType=Identity.Application` while loop occurs  

### Flow interpretation (documented, not assumed)

Based on the observed requests and answers:

- The **Profile page** calls `/api/graph/profile`.  
- When token acquisition fails, the client **navigates to** `MicrosoftIdentity/Account/Challenge?scheme=OpenIdConnect`.  
- The flow completes at `/signin-oidc`, then redirects back to `/entra-profile`.  
- The loop repeats, and the **Identity application cookie** appears to be the only cookie set.  

### Explicit design statement (current observed behavior)

**Entra sign-in for Graph challenges uses the Microsoft.Identity.Web challenge endpoint and completes at `/signin-oidc`, issuing the Identity application cookie (no external cookie observed), and does not route through `/Account/ExternalLogin`.**


## Next providers (planning alignment)

- **OTP**: keep as a local Identity factor (2FA) rather than a separate external provider.
- **Google**: add as another external provider using the same explicit provider-selection UI and account-linking UX.

## Sources (Official)

- ASP.NET Core Identity external provider auth:  
  https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-10.0
- Blazor Web App with Entra ID (InteractiveAuto):  
  https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-entra?view=aspnetcore-10.0
- Blazor security overview (auth-state serialization):  
  https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-9.0
- AddMicrosoftIdentityWebApp:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.web.microsoftidentitywebappauthenticationbuilderextensions.addmicrosoftidentitywebapp?view=msal-model-dotnet-latest
- EnableTokenAcquisitionToCallDownstreamApi:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.web.microsoftidentitywebappauthenticationbuilder.enabletokenacquisitiontocalldownstreamapi?view=msal-model-dotnet-latest
- Web app calls downstream APIs (configuration):  
  https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-app-configuration
- IDownstreamApi.GetForUserAsync:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.abstractions.idownstreamapi.getforuserasync?view=msal-model-dotnet-latest
- IClaimsTransformation.TransformAsync:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.iclaimstransformation.transformasync?view=aspnetcore-8.0
- OpenIdConnectEvents.OnTokenValidated:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectevents.ontokenvalidated?view=aspnetcore-9.0
