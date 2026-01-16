# Authorization Handlers


**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Custom handlers that evaluate `IAuthorizationRequirement` objects to make authorization decisions. Pluggable, testable, and decoupled from endpoints.

**Use Cases:**
- Custom permission logic beyond claims
- Contextual authorization (time-based, location-based)
- Resource-based authorization (can user edit this resource?)
- Complex business rules

**Implementation Details:**
```csharp
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; set; }
}

public class PermissionAuthorizationHandler : 
    AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

**Strengths:**
- ✅ Testable in isolation
- ✅ Reusable across endpoints
- ✅ Supports complex logic
- ✅ Extension point for custom scenarios

**Weaknesses:**
- ❌ More code than role checks
- ❌ Debugging can be complex
- ❌ Performance impact if logic heavy

**Related Patterns:**
- [Permission-Based RBAC](authz-permission-rbac.md)
- [Claims Transformation](authz-claims-transformation.md)

**Demo References:**
- demo3: PermissionAuthorizationHandler + PermissionRequirement
- demo5+: Used consistently across all demos

