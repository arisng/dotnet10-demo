# On-Behalf-Of (OBO) Flow


**Introduced:** demo5  
**Category:** Authentication / Token Exchange  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
OAuth 2.0 grant type where an application exchanges a user's access token for a new token scoped to a downstream API. Preserves user identity across service boundaries while maintaining proper scoping.

**Use Cases:**
- Calling downstream APIs on behalf of authenticated user
- Microservice architectures with API delegation
- Microsoft Graph access in OIDC flows
- Multi-tier API chains maintaining user context

**Implementation Details:**
- `AddMicrosoftIdentityWebApp()` + `EnableTokenAcquisitionToCallDownstreamApi()`
- `IDownstreamApi` abstracts token acquisition and refresh
- Token caching in `AddInMemoryTokenCaches()` or distributed caches
- Automatic token refresh before expiration

**Token Lifecycle:**
```
1. User logs in → receives refresh token + access token (BFF)
2. BFF calls API → IDownstreamApi.GetForUserAsync()
3. Entra ID exchanges token → new access token (API scoped)
4. BFF attaches Bearer token to API request
5. API validates token + scopes
6. Response sent to user
```

**Strengths:**
- ✅ User identity preserved end-to-end
- ✅ Automatic token management
- ✅ Granular scopes per API
- ✅ Supports multiple downstream APIs

**Weaknesses:**
- ❌ Token refresh adds latency
- ❌ Requires token caching strategy
- ❌ Complex scope management
- ❌ Token lifetime coordination

**Related Patterns:**
- [Bearer Token Validation](api-bearer-token-validation.md)
- [OAuth Scopes](authz-oauth-scopes.md)
- Token Caching

**Demo References:**
- demo4: Graph API calls via OBO
- demo5: Custom downstream API via OBO
- demo5.1: YARP + OBO for API forwarding

---

## Authorization Patterns
