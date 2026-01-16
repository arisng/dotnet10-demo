# OAuth Scopes


**Introduced:** demo5  
**Category:** Authorization  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
OAuth 2.0 concept representing coarse API permissions granted by users during authentication. Scopes define *what an application can do* on behalf of a user. Different from local RBAC permissions (which define *what a user can do*).

**Use Cases:**
- Delegated access to external APIs (Microsoft Graph)
- Multi-client API access with different permission tiers
- Partner/third-party integrations
- SaaS APIs with consent flows

**Scope Hierarchy:**
```
OAuth Scopes (Platform/API Boundary) ← Coarse, granted once by user
    ↓
Local RBAC Permissions (Business/Domain) ← Fine-grained, per-user configurable
```

**Example:**
- Scope: `Forecast.Read` (app can access forecast data)
- Permission: `weather.read` (user can view forecasts)
- Authorization: Both must be true

**Strengths:**
- ✅ Clear API contract
- ✅ User consent + audit trail
- ✅ Works across auth providers
- ✅ Industry standard

**Weaknesses:**
- ❌ Doesn't scale for 100+ fine-grained scopes
- ❌ Requires Entra ID portal configuration
- ❌ Consent fatigue if too many scopes

**Related Patterns:**
- [Permission-Based RBAC](authz-permission-rbac.md)
- [On-Behalf-Of Flow](auth-obo-flow.md)

**Demo References:**
- demo5: `Forecast.Read` scope for custom API
- demo5.1: `access_as_user` scope + local RBAC

