# OpenID Connect (OIDC) External Provider


**Introduced:** demo4  
**Category:** Authentication  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
OAuth 2.0 layer for authentication. Browser-based redirect flow where user authenticates with an external provider (Entra ID, Google, GitHub), and the app receives an ID token containing user claims.

**Use Cases:**
- Enterprise/B2B applications (Entra ID)
- Consumer apps with social login (Google, GitHub)
- Hybrid identity scenarios (local + external)
- Federated authentication

**Implementation Details:**
- `AddMicrosoftIdentityWebApp()` for Entra ID
- OIDC middleware handles redirect flow
- Claims mapping from provider claims to `ApplicationUser`
- Auto-provisioning via `OnTokenValidated` event

**Strengths:**
- ✅ Centralized identity management
- ✅ No password handling by app
- ✅ Provider-native MFA/compliance
- ✅ Audit trails in provider
- ✅ Supports multiple providers

**Weaknesses:**
- ❌ Dependency on external service
- ❌ Complex configuration in Azure portal
- ❌ Requires app registration per provider
- ❌ Logout redirect complications

**Related Patterns:**
- [On-Behalf-Of (OBO) Flow](auth-obo-flow.md)
- [Auto-Provisioning](authz-auto-provisioning.md)
- [Claims Mapping](authz-claims-mapping.md)
- Multi-Identity

**Demo References:**
- demo4: Single Entra ID provider
- demo5: Entra ID + downstream API
- demo5.1: Distributed monolith with Entra ID
- demo6: Entra ID toggle per tenant

