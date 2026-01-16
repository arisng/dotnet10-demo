# Role-Based Access Control (RBAC)


**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Authorization model where users are assigned roles, and roles grant access to features/endpoints. Simplest authorization strategy; good for small permission sets (Admin, Manager, User).

**Use Cases:**
- Simple hierarchical permission structures
- Small number of distinct permission levels
- Role-based UI rendering (show/hide based on role)
- Legacy systems with role-based thinking

**Implementation Details:**
- User → Role mapping via `AspNetUserRoles` table
- Check role with `[Authorize(Roles = "Admin")]` or `User.IsInRole("Admin")`
- Role inheritance via custom code
- Fast permission checks (usually in-memory)

**Strengths:**
- ✅ Simple to understand and implement
- ✅ Fast permission checks
- ✅ Built-in ASP.NET Core support
- ✅ Good for small role sets (3-10 roles)

**Weaknesses:**
- ❌ Doesn't scale to many permissions (100+)
- ❌ Can't represent complex authorization rules
- ❌ Role explosion: new combinations require new roles
- ❌ Hard to audit per-user permission changes

**Related Patterns:**
- [Permission-Based RBAC](authz-permission-rbac.md)
- [Claims Transformation](authz-claims-transformation.md)

**Demo References:**
- demo3: Basic RBAC structure
- demo4: Entra ID App Roles mapped to RBAC
- demo5+: Paired with OAuth scopes

