# Glossary — Demo4.2 Implementation Plan (Terms & Context)

This glossary defines technical terms and concepts referenced by the implementation plan. Each entry states the term, a concise definition, and a clear repository/context note ("In this repo...") so reviewers can confirm intent without assumptions.

## Identity & Local Authentication

- **IdentityUser / ASP.NET Core Identity**: The local user representation when using ASP.NET Core Identity. In this repo: presence requires EF Core stores, `AddDefaultIdentity` (or similar) registration, and migrations; absence indicates no local Identity-backed account store.

- **Passkey / WebAuthn**: A public-key credential authentication method (platform or roaming authenticators). In this repo: referenced in demos that use `IdentitySchemaVersions.Version3`; if `IdentityUser` and Identity DB are not wired, passkeys are not available.

- **IdentitySchemaVersions.Version3**: An Identity DB schema version that supports WebAuthn/passkey fields and metadata. In this repo: used by demos that explicitly migrate Identity to support passkeys.

- **Multi‑Identity / Multi‑Identity Providers**: The support for multiple authentication providers (e.g., local passkey + Entra OIDC). In this repo: multi‑identity is only present if both local Identity and external OIDC wiring exist and the sign-in surface exposes both flows.

## External Identity & Providers

- **OIDC (OpenID Connect)**: An identity layer on top of OAuth2 for user authentication. In this repo (demo4.2): the BFF uses `AddOpenIdConnect(...)` to authenticate against the local IdP, and the IdP uses OIDC to federate with Entra as an external provider.

- **Entra ID (Microsoft Entra / Azure AD)**: The cloud identity provider that issues OIDC/OAuth2 tokens, app roles, and consented scopes. In this repo (demo4.2): Entra is configured as an **external provider** for the local IdP.

- **App Roles (Entra App Roles)**: Role definitions configured on an Entra app registration and included as claims in tokens. In this repo: mapping App Roles into local permissions requires a claims-mapping component to be implemented.

- **OpenIddict**: An open-source framework for building OAuth2/OpenID Connect servers in ASP.NET Core. In this repo (demo4.2): `DProcess.Idp` uses OpenIddict as the local authorization server, issuing tokens for the BFF and API.

- **OpenIddictSeeder**: A small `IHostedService` that seeds OpenIddict artifacts at startup (clients/applications, scopes, redirect URIs, and client secrets) using `IOpenIddictApplicationManager` and related managers. In this repo: `OpenIddictSeeder` automates local dev setup for the IdP (`DProcess.Idp`), ensures idempotent client registrations for the BFF and other local clients, and is typically registered via `builder.Services.AddHostedService<OpenIddictSeeder>()`. Note: seeders are intended for local/dev convenience and should avoid committing production secrets.

- **OpenIddict artifacts**: Persisted entities that make up the OpenIddict authorization server configuration and runtime state. Typical artifacts include:
	- **Applications / Clients**: Records describing client apps (client id, redirect URIs, grant types, client secrets).
	- **Scopes**: Named permission sets that clients can request (delegated or delegated+app scopes).
	- **Authorizations / Persisted Grants**: Consent or authorization records linking a subject (user/tenant) and a client to allowed scopes (used for persisted consent and refresh tokens).
	- **Tokens**: Issued access, refresh, and identity tokens persisted for revocation or reference (when configured).
	- **Keys/Cryptographic material**: Keys used to sign/encrypt tokens (managed by OpenIddict or external key stores).
	- **Claim destinations**: Metadata controlling whether specific claims are emitted into access/identity tokens.

	In this repo: `OpenIddict artifacts` refers to the seeded items that `OpenIddictSeeder` creates (clients, scopes, redirect URIs, client secrets, and any initial persisted grants) to make the local IdP usable for the BFF and other local clients.

- **Authorization endpoint pass-through / Pass-through (OpenIddict)**: A mode provided by OpenIddict (`EnableAuthorizationEndpointPassthrough`) that lets the ASP.NET Core app handle authorization endpoint requests in application code (controller or minimal API) rather than using OpenIddict's built-in UI/handlers. In this repo: pass-through is recommended when custom logic (for example, issuing additional claims or applying custom consent/permission rules) must run before returning tokens; enabling it requires wiring the OpenIddict server with passthrough options and implementing the controller endpoints that read `HttpContext.GetOpenIddictServerRequest()` and `SignIn(...)` as the research notes describe.

## Tokens, Flows & Acquisition

- **JWT (JSON Web Token)**: A signed token format commonly used as a bearer token. In this repo: `SaaS.Backend` validates JWTs issued by Entra (authority `https://login.microsoftonline.com/{tenant}/v2.0`).

- **Bearer Token**: An access token sent in `Authorization: Bearer <token>` used for API authentication between services. In this repo: YARP transforms inject bearer tokens when proxying to backends.

- **Scope (OAuth2 scope)**: A named permission string included in access tokens (e.g., `access_as_user`, `User.Read`). In this repo: backend policies validate the `scp` (scope) claim for required scopes.

- **OBO (On‑Behalf‑Of) Flow**: An OAuth2 delegated flow where a service exchanges a user's token for another token to call a downstream API. In this repo (demo4.2): **not implemented**; the BFF forwards the IdP-issued access token directly to the API.

- **Token Acquisition / ITokenAcquisition**: An MIW service used to obtain tokens for the current user or application. In this repo: used in reverse-proxy transforms via `GetAccessTokenForUserAsync(scopes)`.

## Consent & Grants

- **Consent (OAuth/OIDC)**: The explicit grant a user or administrator gives to a client application to allow that client to access specific scopes/resources on the user's behalf. In this repo: consent is required before MIW token acquisition succeeds for delegated scopes; missing consent can surface as token acquisition exceptions that must be handled by the UI or a consent/conditional-access handler.

- **User Consent**: Permission granted by an individual user for an application to access scopes the user is allowed to consent to (e.g., `User.Read`). In this repo: user consent may be requested interactively during sign-in or token acquisition flows.

- **Admin Consent**: A tenant administrator grant that allows an application to access scopes or application permissions that ordinary users cannot consent to. In this repo: admin consent is required for some delegated or application-level permissions and is typically performed by tenant administrators in Entra.

- **Persisted Grant**: A recorded consent (persistent grant) stored by the IdP (e.g., `OAuth2PermissionGrant` in Entra) so that the client does not need to ask the user again until the grant is revoked. In this repo: be aware that local IdP or OpenIddict scenarios must persist grants if you expect repeat behavior.

- **Consent vs Conditional Access**: Consent authorizes scopes; conditional access (MFA, device checks, policies) is an enforcement layer applied by the IdP. In this repo: MIW may throw exceptions for conditional access which are surfaced by the consent/conditional-access handler registered in `SaaS.Frontend`.

## Authorization & Claims

- **DProcess.Shared**: A shared project for cross-cutting contracts and constants. In this repo (demo4.2): used to centralize permission name constants (e.g., `weather.read`) and shared DTOs consumed by IdP/BFF/API.

- **IClaimsTransformation**: An ASP.NET Core extension point that can modify principal claims post-authentication. In this repo (demo4.2): **not used in the BFF** because permission claims are emitted by the IdP into the ID token/UserInfo (Option A). Use it only if the BFF must enrich claims locally.

- **Claims Mapping / PermissionClaimsTransformation**: The process or component that translates external claims (App Roles, group IDs) into local `permission` claims or roles. In this repo (demo4.2): **handled in the IdP during token issuance**, not in the BFF.

- **Permission Claim**: A claim type representing fine-grained permissions (often named `permission`). In this repo (demo4.2): permissions follow demo3 naming (e.g., `weather.read`) and are emitted by the IdP into **access tokens and ID token/UserInfo** so the API and BFF can enforce policies.

- **AuthZ Policy (RequireAssertion / RequireAuthenticatedUser)**: Policy configurations registered with `AddAuthorization(...)` that assert conditions (scopes, permissions) for endpoint access. In this repo: `WeatherGet` demonstrates a scope-based policy verifying `access_as_user`.

## Infrastructure & Orchestration

- **.NET Aspire / Aspire AppHost**: A local orchestration/runtime used to register and run multiple demo services together. In this repo: `SaaS.AppHost` boots Aspire and provides a developer dashboard and service discovery while `ServiceDefaults` centralizes config conventions.

- **Service Discovery / ServiceDefaults**: Utilities and conventions enabling services to find each other in the local Aspire environment. In this repo: `ServiceDefaults` provides consistent base URLs and settings used by other projects.

## Reverse Proxy & Routing

- **YARP (Yet Another Reverse Proxy)**: A reverse-proxy library used to route and transform requests to backend services. In this repo: `SaaS.Frontend` configures YARP to proxy `/api/proxy/*` to the weather backend and apply transforms to add Authorization headers.

- **Reverse‑Proxy Transform**: A YARP capability that modifies proxied requests/responses (e.g., remove cookies, add bearer token). In this repo: a transform uses MIW to acquire user tokens, strips cookies, and injects the `Authorization` header.

## Blazor & UI Behavior

- **InteractiveAuto (Blazor)**: A Blazor rendering mode supporting server prerender and seamless handoff to WASM client. In this repo: `AddInteractiveServerComponents()` and `AddInteractiveWebAssemblyComponents()` indicate support for the InteractiveAuto workflow.

- **AddAuthenticationStateSerialization**: A service registration that serializes authentication state for prerendering so the WASM client can initialize without direct access to tokens. In this repo: used by the BFF to avoid exposing raw tokens to the client during prerender.

- **AuthStateProbe**: A diagnostic component (when present) that inspects authentication provider, claims, and Graph data for debugging. In this repo: referenced in demo4 as a verification aid; presence must be confirmed per-demo.

## Microsoft.Identity.Web & Helpers

- **Microsoft.Identity.Web (MIW)**: A helper library that simplifies Entra ID integration in ASP.NET Core. In this repo: used to add OIDC sign-in (`AddMicrosoftIdentityWebApp`), token acquisition helpers, and `AddDownstreamApi(...)` definitions.

- **AddDownstreamApi / IDownstreamApi**: Abstractions for naming downstream APIs and their scopes so MIW can request tokens for them. In this repo: `WeatherApi` and `MicrosoftGraph` are registered as downstream APIs.

- **MIW Consent/Conditional Access Handler**: A component that converts MIW token acquisition exceptions into interactive challenges (redirects, consent prompts). In this repo: registered as a scoped service to surface conditional access/consent flows to the UI.

## Tokens & Validation (Operational)

- **Authority / Issuer**: The token issuer URL (e.g., `https://login.microsoftonline.com/{tenant}/v2.0`) used to validate token origin. In this repo: backends validate issuer and audience values against configured `AzureAd` settings.

- **Audience (aud claim)**: The intended recipient of a token. In this repo: backends accept normalized audience forms (e.g., `api://{clientId}`) and sometimes multiple audience formats for compatibility.

## Misc / Verification

- **Verification Checklist**: A set of reproduction steps (e.g., local login, Entra login, Graph data visible) used to confirm identity features. In this repo: checklists appear in demo READMEs; the glossary does not assume those checks are implemented unless documented.

---

Notes on interpretation

- Entries explicitly state repository context ("In this repo:") so reviewers can verify presence or absence of implementation.
- Terms that require additional wiring (e.g., passkeys, auto-provisioning) are flagged with the precise runtime prerequisites (Identity, EF Core, migrations) needed to make the feature functional.
