# Downstream API Pattern


**Introduced:** demo5  
**Category:** API Architecture  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Architecture where applications call remote APIs protected by OAuth 2.0 Bearer tokens. Often used for microservices, third-party integrations, and distributed systems.

**Architecture:**
```
Client (Blazor)
    ↓ (Cookie)
BFF (Token acquisition via OBO)
    ↓ (Bearer token)
Downstream API (Token validation)
    ↓
Data / External Services
```

**Use Cases:**
- Microservice architectures
- Separate API scaling from UI
- Reusable APIs across multiple frontends
- Partner/third-party integrations

**Implementation Details:**
- `AddMicrosoftIdentityWebApi()` for API token validation
- `IDownstreamApi` for BFF token acquisition
- Bearer token validation with key set discovery
- Scope-based authorization at API layer

**Strengths:**
- ✅ Loose coupling between frontend and backend
- ✅ APIs reusable by multiple clients
- ✅ Independent scaling
- ✅ Clear API contract

**Weaknesses:**
- ❌ Extra network hop
- ❌ Latency from token exchange
- ❌ Token refresh complexity
- ❌ CORS configuration needed

**Related Patterns:**
- [On-Behalf-Of Flow](auth-obo-flow.md)
- [OAuth Scopes](authz-oauth-scopes.md)
- [Bearer Token Validation](api-bearer-token-validation.md)

**Demo References:**
- demo5: Custom Weather API with OBO
- demo5.1: Downstream API in Modular Monolith

