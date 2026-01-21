# Research: Entra Profile Diagnostics Baseline

## Analysis of Current State

### 1. The Token Acquisition Loop
The `GraphService` calls `_downstreamApi.GetForUserAsync`. This triggers `Microsoft.Identity.Web`'s token acquisition logic.
- **Observed Behavior**: Logs show `ErrorCode: user_null`.
- **Inference**: The `ClaimsPrincipal` available in the `HttpContext` doesn't have the internal claims MSAL uses to index the token cache (like `home_account_id` or `login_hint` as recognized by MSAL), or the token simply isn't there because the session was established via standard OIDC login but without OBO scopes immediately available.

### 2. The Broken Challenge Mechanism
`Program.cs` registers `MicrosoftIdentityConsentAndConditionalAccessHandler` (CCA).
The API endpoint handles the `MicrosoftIdentityWebChallengeUserException` by calling `cca.HandleException(ex)`.
- **Observed Behavior**: CCA issues a 302 and sets headers for a challenge.
- **Fail Point**: The challenge target `/MicrosoftIdentity/Account/Challenge` is part of `Microsoft.Identity.Web.UI`. This package is currently missing from the CSPROJ and the middleware pipeline.

### 3. Identity Scheme Conflict
`Program.cs` uses `IdentityConstants.ApplicationScheme` as the default scheme.
Entra is added via `AddMicrosoftIdentityWebApp` with scheme `"MicrosoftEntra"`.
When calling Graph, we specifically request `EntraAuthenticationScheme`.
There may be a mismatch in how tokens are cached when the user is logged in via the standard "Login" page (which might be using the OIDC "MicrosoftEntra" scheme) vs. how they are subsequently used in the BFF API.

## Research Findings

### 1. Missing `Microsoft.Identity.Web.UI`
The 404 error at `/MicrosoftIdentity/Account/Challenge` is directly caused by the absence of the `Microsoft.Identity.Web.UI` package. 
*   **Solution**: Install `Microsoft.Identity.Web.UI` and call `builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI()` and `app.MapControllers()`.
*   **Nuance**: Even in a predominantly Blazor app, `Microsoft.Identity.Web` relies on these standard MVC controllers to handle OIDC challenges, login, and logout redirects.

### 2. Resolving `MsalUiRequiredException` (user_null)
The `user_null` error occurs because MSAL cannot find a token for the current user in its cache. 
*   **Root Cause**: When the user authenticates via the standard Login page (which uses `IdentityConstants.ApplicationScheme`), their `ClaimsPrincipal` is populated from the local database session cookie. Unless the Entra OIDC flow was explicitly triggered and completed *side-by-side* or as part of that login, the token cache remains uninitialized for that user.
*   **Solution**: The `GraphService` correctly catches the exception, but the system must successfully redirect to the Entra challenge to "link" the session to an Entra token.

### 3. Claim Mapping in .NET 10
In .NET 10, manual claim mapping in `OnTokenValidated` (as seen in `Program.cs`) is often necessary when combining local Identity with Entra to ensure the `oid` and `tid` claims are present and correctly formatted for MSAL's account lookup logic.
*   **Verification**: Ensure the `IdToken` claims are preserved and mapped to the standard `claimtypes` used by Microsoft Identity Web.

## Proposed Implementation Plan
1.  **Add Package**: `dotnet add package Microsoft.Identity.Web.UI`.
2.  **Configure Services**: Update `Program.cs` to include MVC controllers and Identity UI.
3.  **Map Endpoints**: Ensure `app.MapControllers()` is called.
4.  **Verify Redirect**: Re-test the `/entra-profile` page to ensure it correctly triggers a redirect to Microsoft Entra for consent.
