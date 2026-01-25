# Feasibility Research - OpenIddict + Identity Passkeys (.NET 10)

## Scope
- OpenIddict server integration with ASP.NET Core (.NET 10)
- ASP.NET Core Identity passkey support
- Passkey endpoints for Blazor Web Apps

## Findings
- OpenIddict's ASP.NET Core integration supports ASP.NET Core 10 / .NET 10 and uses UseAspNetCore for server/client/validation integration. This allows OpenIddict to run inside standard ASP.NET Core pipelines. (source)
- OpenIddict supports authorization endpoint pass-through (EnableAuthorizationEndpointPassthrough) so a controller or minimal API can issue tokens after custom logic. (source)
- ASP.NET Core Identity includes built-in passkey support, with guidance specific to Blazor Web Apps. (source)
- Passkey support in Identity requires SchemaVersion Version3, and passkey-specific endpoints are added via MapAdditionalIdentityEndpoints (PasskeyCreationOptions / PasskeyRequestOptions). (source)

## Impact on demo4.2 plan
- Keep IdentitySchemaVersions.Version3 and MapAdditionalIdentityEndpoints in the IdP.
- Use OpenIddict authorization endpoint pass-through for custom claims issuance if needed.

## Sources
- https://documentation.openiddict.com/integrations/aspnet-core
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/blazor?view=aspnetcore-10.0
