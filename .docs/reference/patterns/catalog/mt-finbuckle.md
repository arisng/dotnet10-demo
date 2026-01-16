# Finbuckle Multi-Tenant


**Introduced:** demo6 (Planned)  
**Category:** Multi-Tenancy  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
SaaS multi-tenancy framework for .NET. Provides tenant resolution, data isolation strategies, and DI helpers for tenant-scoped operations.

**Use Cases:**
- SaaS applications with multiple customers
- Logical data isolation (queries filtered by tenant)
- Physical isolation (separate databases per tenant)
- Per-tenant feature/configuration management

**Tenant Resolution:**
```
HTTP Request
    ↓ (Finbuckle Middleware)
├─ Host resolution (subdomain: tenant.saas.com)
├─ Route resolution (path: /tenants/{tenantId})
├─ Header resolution (X-Tenant-Id header)
└─ Claim resolution (JWT claim)
    ↓
ITenantInfo resolved
    ↓
Application code accesses via ITenantInfo
```

**Data Isolation:**
- **Shared Database**: Query filters by TenantId
- **Dedicated Database**: Connection string varies per tenant
- **Hybrid**: Mix of shared and dedicated tables

**Strengths:**
- ✅ Flexible tenant resolution
- ✅ Supports multiple isolation strategies
- ✅ DI integration
- ✅ Automatic tenant context passing

**Weaknesses:**
- ❌ Learning curve
- ❌ Query filter complexity
- ❌ Performance tuning needed

**Related Patterns:**
- Multi-Identity

**Demo References:**
- demo6: Finbuckle for tenant isolation
- demo7: Per-tenant feature flags

---

## Observability & Feature Management Patterns
