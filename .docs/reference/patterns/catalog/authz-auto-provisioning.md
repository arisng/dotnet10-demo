# Auto-Provisioning


**Introduced:** demo4  
**Category:** Authorization / User Lifecycle  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Automatic creation and configuration of user accounts on first authentication with external provider. Reduces manual user management while maintaining security and idempotency.

**Use Cases:**
- Just-in-time (JIT) user provisioning
- Self-service enterprise scenarios
- Reducing support tickets for account creation
- Hybrid local+external identity systems

**Implementation Details:**
- Hook into OIDC `OnTokenValidated` event (not `IClaimsTransformation`)
- Dedicated service: `IEntraUserProvisioningService`
- Idempotent: safe to call multiple times
- Database-backed race condition protection
- Automatic rollback on failure (prevents partial state)

**Provisioning Steps:**
```
1. User authenticates with Entra ID
2. OnTokenValidated event fires
3. Check if local user exists
4. If not, create ApplicationUser record
5. Map external claims to user properties
6. Add external login mapping
7. Sync roles from Entra App Roles
8. Fetch Graph data (optional)
```

**Strengths:**
- ✅ No manual user creation needed
- ✅ Idempotent (safe retries)
- ✅ Automatic role syncing
- ✅ Proper error handling + rollback

**Weaknesses:**
- ❌ Adds latency to first sign-in
- ❌ Requires database access
- ❌ Complex error scenarios
- ❌ Needs monitoring

**Related Patterns:**
- [OpenID Connect](auth-oidc-external-provider.md)
- [Claims Mapping](authz-claims-mapping.md)
- [Multi-Identity](auth-multi-identity.md)

**Demo References:**
- demo4: Entra user auto-provisioning on first login
- demo6: Per-tenant provisioning behavior

