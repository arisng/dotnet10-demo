# Feasibility Research - OpenIddict Claim Destinations (Permission Claims)

## Scope
- Emitting permission claims in access tokens
- OpenIddict claim destinations behavior

## Findings
- OpenIddict does not automatically include arbitrary claims in access or identity tokens; claims must be assigned explicit destinations (e.g., Destinations.AccessToken) to be included. (source)
- This is required for custom claims like `permission` to appear in the access token that the API validates. (source)

## Responsibility
- The **IdP** is responsible for emitting permission claims into access tokens.
- The **BFF** should treat tokens as read-only and should not mint or enrich access tokens.

## Impact on demo4.2 plan
- The IdP must set claim destinations for `permission` claims to ensure they are present in access tokens.

## Sources
- https://documentation.openiddict.com/configuration/claim-destinations.html
