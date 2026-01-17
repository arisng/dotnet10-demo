# Multi-Identity (Hybrid Authentication Providers)

**Introduced:** demo4  
**Category:** Authentication  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Support multiple authentication providers in a single app (local identity plus external IdP) while preserving a unified authorization model and user experience.

**Use Cases:**
- Gradual migration from local identity to enterprise IdP
- Hybrid identity: B2C users + employee SSO
- Per-tenant identity configuration (SaaS)
- Availability fallback when an external provider is down

**Implementation Details:**
- Register multiple auth schemes (cookie + OIDC + passkey)
- Provide explicit login options in UI (user selects provider)
- Normalize identities to a shared `ApplicationUser` model
- Map external identities to local roles/permissions
- Optional account linking to merge local and external identities

**Strengths:**
- ✅ Flexible onboarding across user types
- ✅ Enables staged migrations
- ✅ Preserves existing authorization model
- ✅ Reduces vendor lock-in risk

**Weaknesses:**
- ❌ Higher complexity in auth flows
- ❌ Edge cases around account linking
- ❌ Requires careful UX and support messaging

**Related Patterns:**
- [Cookie-Based Authentication](auth-cookie-based.md)
- [Passkey Authentication](auth-passkey.md)
- [OpenID Connect (OIDC) External Provider](auth-oidc-external-provider.md)
- [Auto-Provisioning](authz-auto-provisioning.md)
- [Claims Mapping](authz-claims-mapping.md)

**Demo References:**
- demo4: Local passkey + Entra ID in one app
- demo6: Per-tenant toggle between local and Entra ID
