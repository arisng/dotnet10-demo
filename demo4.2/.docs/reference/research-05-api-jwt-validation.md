# Feasibility Research - API JWT Bearer Validation

## Scope
- JWT bearer validation for APIs
- Audience and issuer validation

## Findings
- Microsoft guidance for JWT bearer authentication recommends validating signature, issuer, audience, and token lifetime. (source)
- APIs typically configure AddJwtBearer with Authority and Audience, or set TokenValidationParameters to enforce audience validation. (source)

## Impact on demo4.2 plan
- Configure ValidAudience (e.g., "api") and ValidateAudience = true to ensure tokens are intended for the API.

## Sources
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
