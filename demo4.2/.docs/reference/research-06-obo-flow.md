# Feasibility Research - OBO (On-Behalf-Of) for Microsoft Graph in demo4.2

## Scope
- OBO flow requirements for Microsoft Entra ID
- Using Microsoft.Identity.Web in ASP.NET Core APIs
- Applicability to the demo4.2 architecture (IdP issues tokens)
- **Downstream target limited to Microsoft Graph**

## Findings
- OBO is a delegated flow where a web API exchanges an incoming **user access token** for a new access token to call **Microsoft Graph** on behalf of the user. The exchange is performed with MSAL’s `AcquireTokenOnBehalfOf` using a **user access token**, not an ID token. (source)
- For ASP.NET Core web APIs, Microsoft recommends using **Microsoft.Identity.Web**, which provides `EnableTokenAcquisitionToCallDownstreamApi()` and `AddDownstreamApi(...)` to enable OBO token acquisition with caching. (source)
- The standard protocol for token exchange is OAuth 2.0 Token Exchange (RFC 8693), which defines the `urn:ietf:params:oauth:token-type:access_token` and related token type identifiers used in exchanges. (source)

## Impact on demo4.2 plan
- Because the downstream target is **Microsoft Graph**, the OBO assertion must be an **Entra-issued access token**. Tokens issued by the local OpenIddict IdP cannot be used directly for Entra OBO.
- OBO for Graph is feasible in demo4.2 only if you **shift the authority** for the downstream path to Entra (BFF gets Entra access tokens for DProcess.Api, and DProcess.Api uses Microsoft.Identity.Web for OBO), or if you implement a **token-exchange** bridge where the IdP can exchange its tokens for Graph tokens (requires RFC 8693 support and trust configuration).

## Sources
- https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow
- https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-app-registration
- https://datatracker.ietf.org/doc/rfc8693/
