# Cookie-Based Authentication (Local Identity)


**Introduced:** demo1  
**Category:** Authentication  
**Complexity:** ⭐ (Foundational)

**Definition:**
Server-side session management using HTTP cookies. User credentials are validated, and a secure, httpOnly cookie is issued. All subsequent requests include this cookie for authentication state.

**Use Cases:**
- Simple web applications with a single frontend
- BFF (Backend-for-Frontend) architectures
- Monolithic applications
- When token management complexity is undesirable

**Implementation Details:**
- ASP.NET Core Identity with cookie authentication middleware
- httpOnly flag prevents JavaScript access (protects against XSS)
- Same-origin requests carry cookies automatically
- No token management or refresh logic needed

**Strengths:**
- ✅ Simplest mental model
- ✅ No token exposure to JavaScript
- ✅ Automatic per-request inclusion
- ✅ Built-in logout/expiration handling

**Weaknesses:**
- ❌ Limited to same-origin requests (CORS complex)
- ❌ Not suitable for native mobile or CLI clients
- ❌ Sessionful (server must maintain state)

**Related Patterns:**
- [BFF (Backend-for-Frontend)](api-bff.md)
- [ClaimsTransformation](authz-claims-transformation.md)

**Demo References:**
- demo1: Foundation
- demo2: Enhanced with passkeys
- demo3: Paired with permission-based RBAC
- demo5.1: Used in Blazor Web Frontend

