# Permission-Based RBAC


**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Fine-grained authorization where roles contain explicit permission mappings. Users are assigned roles, roles contain permissions (often many-to-many), and authorization checks validate permissions rather than roles directly.

**Use Cases:**
- Medium to large permission sets (20-500 permissions)
- Tenant-specific permission customization
- Feature-based authorization (not role-based UI)
- Complex business rules requiring atomic permissions

**Data Model:**
```
User → Role (1..many)
Role → Permission (many..many via RolePermission junction table)
```

**Implementation Details:**
- Permission table: `{ Id, Name (e.g., "weather.read"), Description }`
- RolePermission junction table: `{ RoleId, PermissionId }`
- `IPermissionService`: aggregates user roles → permissions
- Claims transformation adds permission claims
- Authorization handlers validate permission claims

**Strengths:**
- ✅ Scales to many permissions
- ✅ Easy permission auditing
- ✅ Role-independent permission changes
- ✅ Clear API endpoint declarations

**Weaknesses:**
- ❌ More complex data model
- ❌ Larger claims payload
- ❌ Claims caching required for scale

**Related Patterns:**
- [Claims Transformation](authz-claims-transformation.md)
- [Authorization Handlers](authz-authorization-handlers.md)
- [OAuth Scopes](authz-oauth-scopes.md)

**Demo References:**
- demo3: Foundation with permissions table + claims
- demo4: Works with Entra roles + App Roles
- demo5+: Unified across auth sources
- demo6: Per-tenant permission customization

