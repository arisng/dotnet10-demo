# YARP (Reverse Proxy) Pattern


**Introduced:** demo5.1  
**Category:** API Architecture  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Yet Another Reverse Proxy (YARP) implemented as middleware in the BFF to forward client API requests to backend services. Removes business logic from frontend layer while maintaining transparent token management.

**Architecture:**
```
Browser
    ↓
BFF Frontend (Blazor + YARP)
    ├─ Authentication (OIDC / Cookie)
    ├─ Token Acquisition (OBO)
    └─ Request Routing (YARP)
        ↓
Backend API Service
```

**Use Cases:**
- Distributed monolith BFF layer
- Simplifying frontend to pure presentation
- Transparent API forwarding
- Token injection per request

**Implementation Details:**
- YARP middleware in BFF
- Routes `/api/*` requests to backend
- Injects Bearer token in `Authorization` header
- Handles response transformation

**Strengths:**
- ✅ Decouples frontend from API schema
- ✅ No business logic in frontend
- ✅ Transparent routing
- ✅ Centralized token management

**Weaknesses:**
- ❌ Extra network hop
- ❌ Proxy overhead
- ❌ Debugging complexity

**Related Patterns:**
- [Distributed Modular Monolith](dist-modular-monolith.md)
- [On-Behalf-Of Flow](auth-obo-flow.md)

**Demo References:**
- demo5.1: YARP forwarding `/api/*` to ApiService

