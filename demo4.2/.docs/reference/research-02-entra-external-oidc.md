# Feasibility Research - External Entra ID Login into the IdP (OIDC)

## Scope
- External provider login using ASP.NET Core OIDC handler
- Recommended flow for web apps

## Findings
- Microsoft guidance for web apps uses the authorization code flow and recommends PKCE for OIDC clients. (source)
- The standard setup is AddOpenIdConnect with cookies for web apps, configured with Authority, ClientId, ClientSecret, and ResponseType code. (source)

## Impact on demo4.2 plan
- The IdP can use AddOpenIdConnect("Entra", ...) as an external provider following the code flow with PKCE.
- External login can be linked to local Identity users via standard ASP.NET Core Identity external login APIs.

## Sources
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0
