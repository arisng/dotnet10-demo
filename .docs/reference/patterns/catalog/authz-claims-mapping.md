# Claims Mapping


**Introduced:** demo4  
**Category:** Authorization  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Transformation of claims from an external identity provider to application-specific claims. Maps provider claims (e.g., Entra App Roles) to local role/permission structures.

**Use Cases:**
- Entra ID App Roles → Local Roles
- Google/GitHub claims → Custom permissions
- Attribute-based access control (ABAC)
- Multi-provider claim normalization

**Implementation Details:**
- Enhanced `IClaimsTransformation`
- Detects authentication source (Entra vs. Local)
- Looks up provider-specific claims (e.g., `roles`)
- Maps to local role system
- Loads associated permissions

**Example Flow:**
```
Entra ID Token: { roles: ["GlobalAdmin"] }
    ↓ (Claims Transformation)
Local Role: Admin
    ↓ (Permission Service)
Permissions: [weather.read, weather.write, users.delete, ...]
    ↓ (Authorization Handler)
Decision: Allow/Deny
```

**Strengths:**
- ✅ Unified authorization regardless of source
- ✅ Centralized mapping rules
- ✅ Easy to change role mappings
- ✅ Audit trail of who mapped what

**Weaknesses:**
- ❌ Runs on every request
- ❌ Database lookup overhead
- ❌ Complex mapping logic

**Related Patterns:**
- [Claims Transformation](authz-claims-transformation.md)
- [Permission-Based RBAC](authz-permission-rbac.md)

**Demo References:**
- demo4: Entra App Roles → Local Roles
- demo5+: Consistent mapping across all sources

---

## API Architecture Patterns
