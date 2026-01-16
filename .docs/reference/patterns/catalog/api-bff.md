# Backend-for-Frontend (BFF)


**Introduced:** demo3  
**Category:** API Architecture  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Architectural pattern where a server-side backend provides tailored APIs for a specific frontend client. The backend handles authentication, token management, and API composition; the frontend consumes simple HTTP APIs without managing credentials.

**Architecture:**
```
Browser
    ↓ (Cookie + CORS same-origin)
BFF Backend (Authentication + RBAC + APIs)
    ↓ (Bearer token)
Downstream/Internal APIs
    ↓
Data Services
```

**Use Cases:**
- Single-page applications (SPA) with Blazor WASM
- Monolithic apps with tightly coupled frontend
- Simplifying frontend token management
- Centralizing API security concerns

**Implementation Details:**
- BFF provides `/api/*` endpoints
- Endpoints secured with cookie authentication
- No tokens exposed to frontend JavaScript
- BFF handles token refresh automatically

**Strengths:**
- ✅ No token exposure to browser
- ✅ Centralized security logic
- ✅ Simpler frontend
- ✅ Easy CORS (same-origin)

**Weaknesses:**
- ❌ Tight coupling between frontend and backend
- ❌ Doesn't work for mobile/CLI clients
- ❌ Requires server for each frontend variant

**Related Patterns:**
- [Cookie-Based Authentication](auth-cookie-based.md)
- [On-Behalf-Of Flow](auth-obo-flow.md)

**Demo References:**
- demo3: BFF pattern with local Identity
- demo4: BFF + Entra ID
- demo5.1: BFF with YARP proxy

