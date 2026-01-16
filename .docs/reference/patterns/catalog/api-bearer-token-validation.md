# Bearer Token Validation


**Introduced:** demo5  
**Category:** API Architecture  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Process of cryptographically verifying an incoming Bearer token (usually JWT) to ensure it was issued by a trusted authority, hasn't expired, and has the required claims/scopes.

**Use Cases:**
- Protecting API endpoints from unauthorized calls
- Enforcing OAuth scopes
- Multi-tenant API isolation
- Service-to-service authentication

**Implementation Details:**
```csharp
builder.Services.AddMicrosoftIdentityWebApi(configuration);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/weather", GetWeather)
    .RequireAuthorization(policy => 
        policy.RequireClaim("scp", "Forecast.Read"));
```

- Uses OIDC key set discovery (automatic public key rotation)
- Validates issuer, audience, expiration
- Extracts claims for authorization

**Strengths:**
- ✅ Stateless (no session needed)
- ✅ Automatic key rotation
- ✅ Distributed API support
- ✅ Audit trail via JWT claims

**Weaknesses:**
- ❌ Token revocation is eventual (key cache)
- ❌ Requires HTTPS
- ❌ Clock skew issues possible

**Related Patterns:**
- [On-Behalf-Of Flow](auth-obo-flow.md)
- [OAuth Scopes](authz-oauth-scopes.md)

**Demo References:**
- demo5: WeatherApi Bearer token validation
- demo5.1: ApiService token validation

---

## Data & Persistence Patterns
