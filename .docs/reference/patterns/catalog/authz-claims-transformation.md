# Claims Transformation


**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Middleware that runs on every request, extracting claims from the authentication ticket and enriching them with application-specific data. Used to add derived claims (roles, permissions) before authorization handlers evaluate them.

**Use Cases:**
- Adding permission claims from database
- Mapping external provider roles to local roles
- Caching permission lookups per request
- Identity source-agnostic authorization

**Implementation Details:**
```csharp
public class PermissionClaimsTransformation : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Load user roles from database
        // Add permission claims
        return enrichedPrincipal;
    }
}
```

- Registered as `services.AddScoped<IClaimsTransformation, ...>()`
- Runs before authorization handlers
- Read-only operation (no side effects)
- Idempotent across requests

**Strengths:**
- ✅ Clean separation: auth vs. authz
- ✅ Works with any authentication method
- ✅ Centralizes permission logic
- ✅ Testable in isolation

**Weaknesses:**
- ❌ Runs on every request (performance impact)
- ❌ Caching adds complexity
- ❌ Not for database mutations

**Related Patterns:**
- [Permission-Based RBAC](authz-permission-rbac.md)
- [Authorization Handlers](authz-authorization-handlers.md)

**Demo References:**
- demo3: Load permissions from database
- demo4: Map Entra roles to local permissions
- demo5+: Unified authorization across auth sources

