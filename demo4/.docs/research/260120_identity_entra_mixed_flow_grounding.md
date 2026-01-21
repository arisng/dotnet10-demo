# Mixed Identity & Entra ID Integration: Architecture & Grounding

**Date:** 2026-01-20
**Status:** Verified
**Context:** Fixing infinite redirects and "User Null" errors in a Blazor Web App (InteractiveAuto) using both ASP.NET Core Identity (Local) and Entra ID (OIDC) with On-Behalf-Of (OBO) flow.

## 1. Problem Definition
When integrating Microsoft Entra ID (via `Microsoft.Identity.Web`) into an application that primarily uses ASP.NET Core Identity (`Identity.Application`), two major issues arrised:

1.  **Infinite Redirect Loop**: Use of `[Authorize]` or `ChallengeAsync` triggered a redirect to Entra, which succeeded, but the application failed to recognize the user as "Signed In" under the correct scheme, causing a loop.
2.  **OBO Failure (User Null/Missing Token)**: Even when authenticated, the `GraphService` failed to acquire an access token for Microsoft Graph, throwing `ChallengeRequiredException`, which further contributed to the loop.

## 2. Solution: Scheme Alignment & Claims Bridging

### 2.1. Aligning Authentication Schemes
**Theory**: ASP.NET Core supports multiple authentication handlers. However, `SignInManager` and the default Identity UI rely exclusively on `IdentityConstants.ApplicationScheme`. If `Microsoft.Identity.Web` (MIW) signs the user in using a different scheme (e.g., `Cookies`), the primary application context remains unauthenticated.

**Fix**: We configured `AddMicrosoftIdentityWebApp` to use the **same cookie scheme** as ASP.NET Core Identity.

```csharp
// Program.cs
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(options => { ... }, 
    openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
    cookieScheme: IdentityConstants.ApplicationScheme); // <--- Key alignment
```

**Documentation Grounding**:
*   **Reference**: [Migrate authentication and Identity to ASP.NET Core 2.0](https://learn.microsoft.com/en-us/aspnet/core/migration/1x-to-2x/identity-2x?view=aspnetcore-10.0#authentication-middleware-and-services) highlights the importance of the `DefaultScheme` matching the expected handler.
*   **Reference**: [Authorize with a specific scheme](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme?view=aspnetcore-10.0) explains how schemes isolate identities; aligning them unifies the identity.

### 2.2. Bridging Claims for MSAL Cache (OBO Flow)
**Theory**: The *On-Behalf-Of* (OBO) flow requires MSAL.NET to find a cached user token. MSAL uses a cache key derived from specific claims in the user's Principal (typically `uid`, `utid`, `oid`, `tid`, or `preferred_username`).
When ASP.NET Core Identity creates the final `ApplicationUser` principal (via `SignInManager.CreateUserPrincipalAsync`), it creates a clean principal based on the database user, **discarding** the original claims from the Entra ID OIDC token. As a result, MSAL cannot calculate the cache key, finds no token, and throws a `ChallengeRequiredException`.

**Fix**: We intercepted the `OnTokenValidated` event to manually copy these critical infrastructure claims from the OIDC principal to the Identity principal before sign-in.

```csharp
// Program.cs - OnTokenValidated
var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
var identityPrincipal = await signInManager.CreateUserPrincipalAsync(user);

// Copy claims required by MSAL to find the cache entry
var claimsToCopy = new[] { "oid", "tid", "msal_account_id", ... };
foreach (var type in claimsToCopy)
{
    // ... copy logic ...
}

context.Principal = identityPrincipal; // Sign in with the HYBRID principal
```

**Documentation Grounding**:
*   **Reference**: [On-behalf-of flows with MSAL.NET](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow#practical-usage-of-obo-in-an-aspnet---aspnet-core-application) confirms that `GetMsalAccountId` relies on claims like `oid`/`tid`/`upn`.
*   **Reference**: [Token cache serialization](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization) emphasizes that the cache is keyed by the user's account identifier.

### 2.3. Client-Side State Propagation (Blazor)
**Theory**: In Blazor `InteractiveAuto`, the WASM client uses a separate `AuthenticationStateProvider`. For the client to recognize the user as an "Entra User" (and pass policy checks like `RequireClaim("oid")`), these claims must be serialized from the server.

**Fix**: updated `PersistingServerAuthenticationStateProvider` (Server) and `PersistentAuthenticationStateProvider` (Client) to explicitly include `EntraObjectId` and `EntraTenantId` in the `UserInfo` DTO.

**Documentation Grounding**:
*   **Reference**: [Secure an ASP.NET Core Blazor Web App with Microsoft Entra ID](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-entra?view=aspnetcore-10.0) describes the architecture of flowing authentication state in BFF patterns for Blazor.

## 3. Summary of Configuration
1.  **Identity Core**: Manages the persistent user database and application cookie.
2.  **Microsoft Identity Web**: Handles the OIDC protocol and Token Acquisition.
3.  **Bridge**:
    *   **Cookie**: Shared (`IdentityConstants.ApplicationScheme`).
    *   **Identity**: Enriched with MSAL claims (`oid`, `tid`, `msal_account_id`).
    *   **Blazor**: State persistence includes Entra IDs.

This architecture ensures robust support for scenarios where users can log in via Local Password OR Entra ID, while still supporting advanced features like Graph API access (OBO) and Blazor WASM interactivity.
