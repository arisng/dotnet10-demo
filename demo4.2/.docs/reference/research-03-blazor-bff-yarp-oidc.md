# Feasibility Research - Blazor Web App BFF + YARP + OIDC

## Scope
- Blazor Web App security model with OIDC
- BFF pattern with YARP reverse proxy
- API calls using access tokens

## Findings
- Microsoft documents a Blazor Web App OIDC approach that supports a BFF pattern using YARP reverse proxy and Aspire for orchestration. (source)
- The pattern stores tokens in the server-side auth session and forwards access tokens to downstream APIs from the BFF. (source)

## Impact on demo4.2 plan
- The BFF can use OIDC (authorization code flow) and forward the access token to the API via YARP.
- Aspire orchestration for IdP/BFF/API is aligned with the documented approach.

## Sources
- https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/config-files?view=aspnetcore-10.0
