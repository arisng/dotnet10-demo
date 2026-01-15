# Patterns Catalog: .NET 10 Progressive Demos

**Purpose:** Single source of truth for all architectural, design, and system patterns used across demo1-demo5.1 and planned for demo6-demo7. Use this catalog to understand, compare, and select patterns for new demos.

**Last Updated:** 2026-01-12  
**Scope:** Demos 1-7 (completed and planned)

---

## Quick Navigation

### By Category
- [Authentication Patterns](#authentication-patterns)
- [Authorization Patterns](#authorization-patterns)
- [API Architecture Patterns](#api-architecture-patterns)
- [Data & Persistence Patterns](#data--persistence-patterns)
- [Messaging & Event Patterns](#messaging--event-patterns)
- [Multi-Tenancy Patterns](#multi-tenancy-patterns)
- [Observability & Feature Management Patterns](#observability--feature-management-patterns)
- [Component & UI Patterns](#component--ui-patterns)

### By Demo Introduction
- [demo1](#patterns-introduced-in-demo1)
- [demo2](#patterns-introduced-in-demo2)
- [demo3](#patterns-introduced-in-demo3)
- [demo4](#patterns-introduced-in-demo4)
- [demo5](#patterns-introduced-in-demo5)
- [demo5.1](#patterns-introduced-in-demo51)
- [demo6 (Planned)](#patterns-planned-for-demo6)
- [demo7 (Planned)](#patterns-planned-for-demo7)

---

## Authentication Patterns

### 1. Cookie-Based Authentication (Local Identity)

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
- BFF (Backend-for-Frontend) [#2-bff-backend-for-frontend](#2-bff-backend-for-frontend)
- ClaimsTransformation [#6-claims-transformation](#6-claims-transformation)

**Demo References:**
- demo1: Foundation
- demo2: Enhanced with passkeys
- demo3: Paired with permission-based RBAC
- demo5.1: Used in Blazor Web Frontend

---

### 2. Passkey Authentication (WebAuthn)

**Introduced:** demo2  
**Category:** Authentication  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Passwordless authentication using the WebAuthn API. Users register a security key or biometric, then authenticate by proving possession of the private key.

**Use Cases:**
- Consumer-facing applications requiring high security
- Organizations reducing password-related breaches
- Multi-platform authentication (desktop, mobile, web)
- Compliance-driven environments (financial services, healthcare)

**Implementation Details:**
- ASP.NET Core Identity with IdentitySchemaVersion3
- `.NET 10`: New `MapAdditionalIdentityEndpoints()` wires `/PasskeyCreationOptions` and `/PasskeyRequestOptions`
- Full Manage UI components: `Passkeys.razor`, `RenamePasskey.razor`
- Credential registration and assertion validation handled by framework

**Strengths:**
- ✅ Stronger security than passwords
- ✅ Better UX (biometric/device unlock)
- ✅ Phishing-resistant
- ✅ Cross-platform support
- ✅ Built-in .NET 10 support

**Weaknesses:**
- ❌ Requires WebAuthn-capable browser/device
- ❌ Learning curve for users
- ❌ Recovery procedures needed if key lost

**Related Patterns:**
- Multi-Identity [#10-multi-identity](#10-multi-identity)
- Claims Transformation [#6-claims-transformation](#6-claims-transformation)

**Demo References:**
- demo2: Complete passkey implementation and diagnostics
- demo3: Passkey users assigned roles/permissions
- demo4: Passkeys coexist with Entra ID
- demo6: Per-tenant passkey/Entra toggle

---

### 3. OpenID Connect (OIDC) External Provider

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
- On-Behalf-Of (OBO) Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)
- Auto-Provisioning [#7-auto-provisioning](#7-auto-provisioning)
- Claims Mapping [#11-claims-mapping](#11-claims-mapping)
- Multi-Identity [#10-multi-identity](#10-multi-identity)

**Demo References:**
- demo4: Single Entra ID provider
- demo5: Entra ID + downstream API
- demo5.1: Distributed monolith with Entra ID
- demo6: Entra ID toggle per tenant

---

### 4. On-Behalf-Of (OBO) Flow

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
- Bearer Token Validation [#3-bearer-token-validation](#3-bearer-token-validation)
- OAuth Scopes [#12-oauth-scopes](#12-oauth-scopes)
- Token Caching [#13-token-caching](#13-token-caching)

**Demo References:**
- demo4: Graph API calls via OBO
- demo5: Custom downstream API via OBO
- demo5.1: YARP + OBO for API forwarding

---

## Authorization Patterns

### 5. Role-Based Access Control (RBAC)

**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Authorization model where users are assigned roles, and roles grant access to features/endpoints. Simplest authorization strategy; good for small permission sets (Admin, Manager, User).

**Use Cases:**
- Simple hierarchical permission structures
- Small number of distinct permission levels
- Role-based UI rendering (show/hide based on role)
- Legacy systems with role-based thinking

**Implementation Details:**
- User → Role mapping via `AspNetUserRoles` table
- Check role with `[Authorize(Roles = "Admin")]` or `User.IsInRole("Admin")`
- Role inheritance via custom code
- Fast permission checks (usually in-memory)

**Strengths:**
- ✅ Simple to understand and implement
- ✅ Fast permission checks
- ✅ Built-in ASP.NET Core support
- ✅ Good for small role sets (3-10 roles)

**Weaknesses:**
- ❌ Doesn't scale to many permissions (100+)
- ❌ Can't represent complex authorization rules
- ❌ Role explosion: new combinations require new roles
- ❌ Hard to audit per-user permission changes

**Related Patterns:**
- Permission-Based RBAC [#8-permission-based-rbac](#8-permission-based-rbac)
- Claims Transformation [#6-claims-transformation](#6-claims-transformation)

**Demo References:**
- demo3: Basic RBAC structure
- demo4: Entra ID App Roles mapped to RBAC
- demo5+: Paired with OAuth scopes

---

### 6. Claims Transformation

**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Middleware that runs on every request, extracting claims from the authentication ticket and enriching them with application-specific data. Used to add derived claims (roles, permissions) before authorization handlers evaluate them.

**Use Cases:**
- Adding permission claims from database
- Mapping external provider roles to local roles
- Caching permission lookups per request
- Identity source-agnostic authorization

**Implementation Details:**
```csharp
public class PermissionClaimsTransformation : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Load user roles from database
        // Add permission claims
        return enrichedPrincipal;
    }
}
```

- Registered as `services.AddScoped<IClaimsTransformation, ...>()`
- Runs before authorization handlers
- Read-only operation (no side effects)
- Idempotent across requests

**Strengths:**
- ✅ Clean separation: auth vs. authz
- ✅ Works with any authentication method
- ✅ Centralizes permission logic
- ✅ Testable in isolation

**Weaknesses:**
- ❌ Runs on every request (performance impact)
- ❌ Caching adds complexity
- ❌ Not for database mutations

**Related Patterns:**
- Permission-Based RBAC [#8-permission-based-rbac](#8-permission-based-rbac)
- Authorization Handlers [#9-authorization-handlers](#9-authorization-handlers)

**Demo References:**
- demo3: Load permissions from database
- demo4: Map Entra roles to local permissions
- demo5+: Unified authorization across auth sources

---

### 7. Auto-Provisioning

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
- OpenID Connect [#3-openid-connect-oidc-external-provider](#3-openid-connect-oidc-external-provider)
- Claims Mapping [#11-claims-mapping](#11-claims-mapping)
- Multi-Identity [#10-multi-identity](#10-multi-identity)

**Demo References:**
- demo4: Entra user auto-provisioning on first login
- demo6: Per-tenant provisioning behavior

---

### 8. Permission-Based RBAC

**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Fine-grained authorization where roles contain explicit permission mappings. Users are assigned roles, roles contain permissions (often many-to-many), and authorization checks validate permissions rather than roles directly.

**Use Cases:**
- Medium to large permission sets (20-500 permissions)
- Tenant-specific permission customization
- Feature-based authorization (not role-based UI)
- Complex business rules requiring atomic permissions

**Data Model:**
```
User → Role (1..many)
Role → Permission (many..many via RolePermission junction table)
```

**Implementation Details:**
- Permission table: `{ Id, Name (e.g., "weather.read"), Description }`
- RolePermission junction table: `{ RoleId, PermissionId }`
- `IPermissionService`: aggregates user roles → permissions
- Claims transformation adds permission claims
- Authorization handlers validate permission claims

**Strengths:**
- ✅ Scales to many permissions
- ✅ Easy permission auditing
- ✅ Role-independent permission changes
- ✅ Clear API endpoint declarations

**Weaknesses:**
- ❌ More complex data model
- ❌ Larger claims payload
- ❌ Claims caching required for scale

**Related Patterns:**
- Claims Transformation [#6-claims-transformation](#6-claims-transformation)
- Authorization Handlers [#9-authorization-handlers](#9-authorization-handlers)
- OAuth Scopes [#12-oauth-scopes](#12-oauth-scopes)

**Demo References:**
- demo3: Foundation with permissions table + claims
- demo4: Works with Entra roles + App Roles
- demo5+: Unified across auth sources
- demo6: Per-tenant permission customization

---

### 9. Authorization Handlers

**Introduced:** demo3  
**Category:** Authorization  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Custom handlers that evaluate `IAuthorizationRequirement` objects to make authorization decisions. Pluggable, testable, and decoupled from endpoints.

**Use Cases:**
- Custom permission logic beyond claims
- Contextual authorization (time-based, location-based)
- Resource-based authorization (can user edit this resource?)
- Complex business rules

**Implementation Details:**
```csharp
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; set; }
}

public class PermissionAuthorizationHandler : 
    AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

**Strengths:**
- ✅ Testable in isolation
- ✅ Reusable across endpoints
- ✅ Supports complex logic
- ✅ Extension point for custom scenarios

**Weaknesses:**
- ❌ More code than role checks
- ❌ Debugging can be complex
- ❌ Performance impact if logic heavy

**Related Patterns:**
- Permission-Based RBAC [#8-permission-based-rbac](#8-permission-based-rbac)
- Claims Transformation [#6-claims-transformation](#6-claims-transformation)

**Demo References:**
- demo3: PermissionAuthorizationHandler + PermissionRequirement
- demo5+: Used consistently across all demos

---

### 10. OAuth Scopes

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
- Permission-Based RBAC [#8-permission-based-rbac](#8-permission-based-rbac)
- On-Behalf-Of Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)

**Demo References:**
- demo5: `Forecast.Read` scope for custom API
- demo5.1: `access_as_user` scope + local RBAC

---

### 11. Claims Mapping

**Introduced:** demo4  
**Category:** Authorization  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Transformation of claims from an external identity provider to application-specific claims. Maps provider claims (e.g., Entra App Roles) to local role/permission structures.

**Use Cases:**
- Entra ID App Roles → Local Roles
- Google/GitHub claims → Custom permissions
- Attribute-based access control (ABAC)
- Multi-provider claim normalization

**Implementation Details:**
- Enhanced `IClaimsTransformation`
- Detects authentication source (Entra vs. Local)
- Looks up provider-specific claims (e.g., `roles`)
- Maps to local role system
- Loads associated permissions

**Example Flow:**
```
Entra ID Token: { roles: ["GlobalAdmin"] }
    ↓ (Claims Transformation)
Local Role: Admin
    ↓ (Permission Service)
Permissions: [weather.read, weather.write, users.delete, ...]
    ↓ (Authorization Handler)
Decision: Allow/Deny
```

**Strengths:**
- ✅ Unified authorization regardless of source
- ✅ Centralized mapping rules
- ✅ Easy to change role mappings
- ✅ Audit trail of who mapped what

**Weaknesses:**
- ❌ Runs on every request
- ❌ Database lookup overhead
- ❌ Complex mapping logic

**Related Patterns:**
- Claims Transformation [#6-claims-transformation](#6-claims-transformation)
- Permission-Based RBAC [#8-permission-based-rbac](#8-permission-based-rbac)

**Demo References:**
- demo4: Entra App Roles → Local Roles
- demo5+: Consistent mapping across all sources

---

## API Architecture Patterns

### 12. Backend-for-Frontend (BFF)

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
- Cookie-Based Authentication [#1-cookie-based-authentication-local-identity](#1-cookie-based-authentication-local-identity)
- On-Behalf-Of Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)

**Demo References:**
- demo3: BFF pattern with local Identity
- demo4: BFF + Entra ID
- demo5.1: BFF with YARP proxy

---

### 13. Downstream API Pattern

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
- On-Behalf-Of Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)
- OAuth Scopes [#12-oauth-scopes](#12-oauth-scopes)
- Bearer Token Validation [#3-bearer-token-validation](#3-bearer-token-validation)

**Demo References:**
- demo5: Custom Weather API with OBO
- demo5.1: Downstream API in Modular Monolith

---

### 14. YARP (Reverse Proxy) Pattern

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
- Distributed Modular Monolith [#15-distributed-modular-monolith](#15-distributed-modular-monolith)
- On-Behalf-Of Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)

**Demo References:**
- demo5.1: YARP forwarding `/api/*` to ApiService

---

### 15. Bearer Token Validation

**Introduced:** demo5  
**Category:** API Architecture  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Process of cryptographically verifying an incoming Bearer token (usually JWT) to ensure it was issued by a trusted authority, hasn't expired, and has the required claims/scopes.

**Use Cases:**
- Protecting API endpoints from unauthorized calls
- Enforcing OAuth scopes
- Multi-tenant API isolation
- Service-to-service authentication

**Implementation Details:**
```csharp
builder.Services.AddMicrosoftIdentityWebApi(configuration);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/weather", GetWeather)
    .RequireAuthorization(policy => 
        policy.RequireClaim("scp", "Forecast.Read"));
```

- Uses OIDC key set discovery (automatic public key rotation)
- Validates issuer, audience, expiration
- Extracts claims for authorization

**Strengths:**
- ✅ Stateless (no session needed)
- ✅ Automatic key rotation
- ✅ Distributed API support
- ✅ Audit trail via JWT claims

**Weaknesses:**
- ❌ Token revocation is eventual (key cache)
- ❌ Requires HTTPS
- ❌ Clock skew issues possible

**Related Patterns:**
- On-Behalf-Of Flow [#4-on-behalf-of-obo-flow](#4-on-behalf-of-obo-flow)
- OAuth Scopes [#12-oauth-scopes](#12-oauth-scopes)

**Demo References:**
- demo5: WeatherApi Bearer token validation
- demo5.1: ApiService token validation

---

## Data & Persistence Patterns

### 16. Outbox Pattern

**Introduced:** demo6 (Planned)  
**Category:** Data Persistence / Messaging  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Reliable event publishing pattern where outgoing events are stored in the same database transaction as aggregate state changes. A background process then publishes events to a message broker, ensuring atomicity.

**Problem Solved:**
- Publishing directly to broker can fail (network issue)
- Transaction commits but publishing fails → lost events
- Events lost = data inconsistency

**Implementation:**
```
Aggregate State Change
    + Outbox Entry
    ↓ (same transaction)
Database Commit
    ↓
Background Publisher
    ↓
Message Broker
```

**Use Cases:**
- Reliable domain event publishing
- Event-driven architectures
- Eventually consistent systems
- Multi-service coordination

**Data Model:**
```csharp
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

**Strengths:**
- ✅ Guaranteed event persistence
- ✅ Atomic with business state
- ✅ Natural ordering per aggregate
- ✅ No external dependencies during write

**Weaknesses:**
- ❌ Adds database table + polling overhead
- ❌ Eventually consistent (not immediate)
- ❌ Requires idempotent consumers

**Related Patterns:**
- Inbox Pattern [#17-inbox-pattern](#17-inbox-pattern)
- Choreographed Saga [#18-choreographed-saga](#18-choreographed-saga)

**Demo References:**
- demo6: Outbox for tenant-scoped events
- demo7+: Foundation for saga patterns

---

### 17. Inbox Pattern

**Introduced:** demo6 (Planned)  
**Category:** Data Persistence / Messaging  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Idempotency pattern where message consumers record consumed messages in a database table. On retry, consumers check if message was already processed, preventing duplicate processing.

**Problem Solved:**
- Message broker retries can cause duplicate processing
- Idempotent consumers prevent duplicate side effects

**Implementation:**
```
Message Received
    ↓
Check Inbox: message ID already processed?
    ├─ Yes → Return cached result
    └─ No → Process + Record in Inbox
```

**Data Model:**
```csharp
public class InboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

**Strengths:**
- ✅ Handles retries gracefully
- ✅ Prevents duplicate processing
- ✅ Guaranteed exactly-once semantics
- ✅ Audit trail of processed events

**Weaknesses:**
- ❌ Requires database per consumer
- ❌ Adds processing latency
- ❌ Cleanup of old records needed

**Related Patterns:**
- Outbox Pattern [#16-outbox-pattern](#16-outbox-pattern)
- Choreographed Saga [#18-choreographed-saga](#18-choreographed-saga)

**Demo References:**
- demo6: Inbox for idempotent event handlers
- demo7+: Saga event processing

---

### 18. Service Abstraction Pattern

**Introduced:** demo3  
**Category:** Data Access  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Abstraction pattern using interfaces to decouple components from concrete service implementations. Solves the "prerendering dependency injection" problem in Blazor where different implementations are needed at server vs. client.

**Problem:**
- Blazor SSR prerender needs database access (no HttpClient)
- Blazor WASM needs HttpClient (no database)
- Same component code, different implementations needed

**Solution:**
```
IWeatherService (interface)
    ├─ ServerWeatherService (database)
    └─ ClientWeatherService (HttpClient)

Component injects IWeatherService
    ├─ At server prerender: uses ServerWeatherService
    └─ At WASM runtime: uses ClientWeatherService
```

**Implementation:**
- Define shared interfaces in `.Shared` project
- Implement for server (database access)
- Implement for client (HttpClient)
- Register appropriately in each DI container

**Strengths:**
- ✅ Single component code works everywhere
- ✅ Type-safe at compile time
- ✅ Easy to test with mocks
- ✅ Clear interface contracts

**Weaknesses:**
- ❌ Requires multiple implementations
- ❌ Potential for implementation skew
- ❌ More code to maintain

**Related Patterns:**
- Dependency Injection [#19-dependency-injection](#19-dependency-injection)

**Demo References:**
- demo3: IWeatherService abstraction
- demo4+: Consistent across all demos

---

## Messaging & Event Patterns

### 19. Choreographed Saga

**Introduced:** demo8 (Planned)  
**Category:** Messaging  
**Complexity:** ⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Distributed transaction pattern where workflow steps are coordinated through events. Each step processes an event and publishes the next event in sequence. No central coordinator; workflow logic is distributed.

**Use Case: Order Fulfillment**
```
OrderPlaced Event
    ↓ (Inventory Handler)
InventoryReserved Event
    ↓ (Payment Handler)
PaymentCaptured Event
    ↓ (Shipping Handler)
OrderShipped Event
```

**Per-Aggregate Ordering:**
- All events for same OrderId processed sequentially
- Different orders process in parallel (scalability)
- Achieves "ordered per aggregate" without bottleneck

**Implementation:**
- Message broker with per-aggregate ordering (Sessions, Partitions, FIFO Groups)
- Each handler publishes the next event
- Outbox + Inbox for reliability + idempotency
- Compensation for failure scenarios

**Strengths:**
- ✅ Per-aggregate ordering with horizontal scale
- ✅ Natural workflow expression
- ✅ Decoupled handlers
- ✅ Automatic failure recovery

**Weaknesses:**
- ❌ Workflow implicit (hard to visualize)
- ❌ Complex debugging
- ❌ Eventual consistency
- ❌ Compensation logic needed

**Related Patterns:**
- Outbox Pattern [#16-outbox-pattern](#16-outbox-pattern)
- Inbox Pattern [#17-inbox-pattern](#17-inbox-pattern)
- State Machine Saga [#20-state-machine-saga](#20-state-machine-saga)

**Demo References:**
- demo8 (Planned): Full choreographed saga with order processing

---

### 20. State Machine Saga

**Introduced:** demo9 (Planned)  
**Category:** Messaging  
**Complexity:** ⭐⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Orchestration pattern for long-running workflows. A saga state machine holds the workflow state and decides what happens next. More explicit than choreography but adds complexity.

**Use Case: Order Processing State Machine**
```
[Submitted]
    ↓ OrderPlaced
[AwaitingPayment]
    ├─ PaymentSucceeded → [ReadyToShip]
    └─ PaymentFailed → [Compensating] → [Failed]

[ReadyToShip]
    ↓ OrderShipped
[Completed]
```

**Implementation (MassTransit):**
- Define state machine with states, events, transitions
- Saga instance holds workflow data
- Events drive state transitions
- Saga decides what message to publish
- Saga handles timeouts and compensation

**Strengths:**
- ✅ Workflow is explicit and visible
- ✅ Centralized state tracking
- ✅ Easier debugging ("where are we stuck?")
- ✅ Timeouts + compensation explicit
- ✅ Observability dashboards possible

**Weaknesses:**
- ❌ More complex implementation
- ❌ State machine library required
- ❌ Latency from state persistence
- ❌ More operational overhead

**Related Patterns:**
- Choreographed Saga [#19-choreographed-saga](#19-choreographed-saga)
- Outbox Pattern [#16-outbox-pattern](#16-outbox-pattern)

**Demo References:**
- demo9 (Planned): Order processing state machine

---

## Multi-Tenancy Patterns

### 21. Finbuckle Multi-Tenant

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
- Multi-Identity [#10-multi-identity](#10-multi-identity)

**Demo References:**
- demo6: Finbuckle for tenant isolation
- demo7: Per-tenant feature flags

---

## Observability & Feature Management Patterns

### 22. Feature Flags (Feature Management)

**Introduced:** demo7 (Planned)  
**Category:** Feature Management  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Runtime toggles that control feature visibility or behavior. Allow deploying code without enabling features; useful for A/B testing, gradual rollouts, and kill switches.

**Use Cases:**
- Gradual feature rollout (dark launch)
- A/B testing new features
- Emergency kill switches
- Subscription tier features (premium only)
- Canary deployments

**Implementation (Microsoft.FeatureManagement):**
```csharp
if (await featureManager.IsEnabledAsync("PremiumReports"))
{
    // Show premium report features
}
```

**Integration with Azure AppConfig:**
- Centralized flag management
- Real-time updates (no redeploy)
- Per-environment flags
- Per-tenant feature overrides

**Strengths:**
- ✅ Quick enable/disable without deploy
- ✅ Per-tenant customization
- ✅ A/B testing support
- ✅ Gradual rollout capability

**Weaknesses:**
- ❌ Flag proliferation without cleanup
- ❌ Testing complexity (many combinations)
- ❌ Operational overhead

**Related Patterns:**
- Multi-Tenancy [#21-finbuckle-multi-tenant](#21-finbuckle-multi-tenant)

**Demo References:**
- demo7: Feature flags for premium features
- demo6+: Per-tenant feature toggles

---

### 23. Structured Logging with Correlation IDs

**Introduced:** demo6+ (Planned)  
**Category:** Observability  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Logging with structured data (key-value pairs) and correlation IDs to trace requests across services. Enables efficient searching and debugging in distributed systems.

**Use Cases:**
- Distributed tracing across microservices
- Root cause analysis from logs
- Performance profiling
- Security audit trails

**Implementation (Serilog):**
```csharp
Log.Information("Processing order {OrderId} for tenant {TenantId}", 
    orderId, tenantId);
    
// Log context with correlation ID
LogContext.PushProperty("CorrelationId", correlationId);
```

**Strengths:**
- ✅ Machine-parseable logs
- ✅ Cross-service tracing
- ✅ Easy filtering/searching
- ✅ Audit trail support

**Weaknesses:**
- ❌ Requires structured logging setup
- ❌ Log volume management
- ❌ PII protection needed

**Related Patterns:**
- OpenTelemetry Integration [#24-opentelemetry-integration](#24-opentelemetry-integration)

**Demo References:**
- demo6+: Structured logging throughout

---

### 24. OpenTelemetry Integration

**Introduced:** demo5.1 (AppHost uses it)  
**Category:** Observability  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Unified observability framework for collecting metrics, traces, and logs. Integrates with ASP.NET Core built-in instrumentation.

**Use Cases:**
- Distributed tracing across services
- Performance metrics collection
- Anomaly detection
- Operational dashboards

**Implementation:**
- Metrics: Authorization checks, HTTP requests, custom business metrics
- Traces: Request flow through services
- Logs: Structured logging with context

**Strengths:**
- ✅ Unified instrumentation
- ✅ Vendor-agnostic (OTEL standard)
- ✅ Built-in ASP.NET Core support
- ✅ Low overhead

**Weaknesses:**
- ❌ Complex configuration
- ❌ Sampling strategy needed
- ❌ Storage/cost at scale

**Related Patterns:**
- Structured Logging [#23-structured-logging-with-correlation-ids](#23-structured-logging-with-correlation-ids)

**Demo References:**
- demo5.1: OpenTelemetry via ServiceDefaults
- demo6+: Enhanced metrics and tracing

---

## Component & UI Patterns

### 25. Cascading Authentication State

**Introduced:** demo2  
**Category:** Blazor / UI  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Blazor pattern using `<CascadingAuthenticationState>` to provide `AuthenticationState` to all child components. Enables consistent auth state across server and WASM renders.

**Use Cases:**
- Blazor Web Apps with mixed render modes
- Consistent authentication across component tree
- Progressive enhancement support

**Implementation:**
```csharp
<CascadingAuthenticationState>
    <Router>
        <!-- app routes -->
    </Router>
</CascadingAuthenticationState>
```

**Strengths:**
- ✅ Built-in Blazor support
- ✅ Works across render modes
- ✅ Automatic state passing
- ✅ Simple to use

**Weaknesses:**
- ❌ Can't customize cascade path
- ❌ All components receive auth state

**Demo References:**
- demo2+: Foundation in all subsequent demos

---

### 26. InteractiveAuto Render Mode Progression

**Introduced:** demo2  
**Category:** Blazor / UI  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Blazor Web App render mode that progressively enhances: server prerender + interactive server (SSR with SignalR) → WASM when loaded. Optimizes for fast first view while supporting full WASM after download.

**4-Phase Lifecycle (First Visit):**
```
1. Server Prerender (HTML → client)
2. Interactive Server (SignalR → client, WASM downloading)
3. WASM Initialized (WASM → ready)
4. Interactive WASM (WASM → active)
```

**Subsequent Visits:**
- WASM cached (Local Storage check)
- Skip phases 1, 2 → go straight to 3, 4

**Use Cases:**
- Optimal perceived performance
- SEO-friendly rendering
- Works offline after WASM cached
- Mobile-friendly

**Strengths:**
- ✅ Fast initial load
- ✅ Seamless transition
- ✅ SEO support
- ✅ Works without JavaScript initially

**Weaknesses:**
- ❌ Complex render mode switching
- ❌ Requires service abstraction
- ❌ Caching behavior unintuitive

**Related Patterns:**
- Service Abstraction Pattern [#18-service-abstraction-pattern](#18-service-abstraction-pattern)
- Cascading Authentication State [#25-cascading-authentication-state](#25-cascading-authentication-state)

**Demo References:**
- demo2: Full diagnostics of InteractiveAuto phases
- demo3+: Standard render mode for all demos

---

### 27. AuthorizeView for UI Authorization

**Introduced:** demo3  
**Category:** Blazor / UI  
**Complexity:** ⭐ (Foundational)

**Definition:**
Blazor built-in component for conditional rendering based on authorization state. Simplifies showing/hiding UI elements based on user roles or policies.

**Use Cases:**
- Hide admin features from non-admins
- Show feature based on user role
- Simple permission-based UI (alongside authorization handlers)

**Implementation:**
```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <p>Admin content</p>
    </Authorized>
    <NotAuthorized>
        <p>Not authorized</p>
    </NotAuthorized>
</AuthorizeView>
```

**Strengths:**
- ✅ Built-in component
- ✅ Declarative syntax
- ✅ Handles async auth state
- ✅ Simple use cases

**Weaknesses:**
- ❌ UX: doesn't prevent API access (hide doesn't mean secure)
- ❌ Limited for complex rules
- ❌ Not a replacement for server-side checks

**Demo References:**
- demo3+: Used throughout for UI gating

---

## Distributed Architecture Patterns

### 28. Distributed Modular Monolith

**Introduced:** demo5.1  
**Category:** Architecture  
**Complexity:** ⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Architecture combining modular monolith patterns (vertical slices, domain-driven design) with distributed deployment (separate Frontend and Backend services). Balances monolith simplicity with distributed system benefits.

**Components:**
- **Frontend (BFF):** Blazor UI + authentication + YARP proxy
- **Backend (API Service):** Modular monolith with vertical slices
- **Orchestrator:** .NET Aspire for service discovery + configuration

**Vertical Slices (Example):**
```
Weather Domain
├── WeatherController/Endpoints
├── WeatherService
├── WeatherEntity
└── WeatherRepository

User Domain
├── UserController/Endpoints
├── UserService
├── UserEntity
└── UserRepository
```

**Service Topology (Local):**
```
AppHost (Orchestrator)
├─ Frontend (port 7210)
│  └─ YARP → ApiService
├─ ApiService (port 7220)
│  └─ Database
└─ ServiceDefaults (shared observability)
```

**Use Cases:**
- Monolith becoming complex → split frontend/backend
- Multiple frontend variants (web, mobile)
- Independent frontend/backend team autonomy
- Cloud-native deployments (Kubernetes-ready)

**Strengths:**
- ✅ Cleaner separation of concerns
- ✅ Frontend can scale independently
- ✅ Vertical slices enable clear ownership
- ✅ Easier testing (unit + integration)

**Weaknesses:**
- ❌ More complex than monolith
- ❌ Network latency (frontend → backend)
- ❌ Distributed debugging difficulty
- ❌ Operational complexity

**Related Patterns:**
- YARP Proxy [#14-yarp-reverse-proxy-pattern](#14-yarp-reverse-proxy-pattern)
- Aspire Orchestration [#29-net-aspire-orchestration](#29-net-aspire-orchestration)

**Demo References:**
- demo5.1: Complete implementation with Aspire + YARP

---

### 29. .NET Aspire Orchestration

**Introduced:** demo5.1  
**Category:** Infrastructure / Orchestration  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
.NET Aspire is a modern cloud-native application stack for building observable, production-ready distributed applications. Simplifies orchestration, service discovery, and telemetry setup.

**Components:**
- **AppHost:** Orchestration code (C#) defining services, ports, dependencies
- **ServiceDefaults:** Shared telemetry, health checks, service discovery configuration
- **Dashboard:** Visual monitoring of services, logs, traces, metrics

**Service Discovery (Magic!):**
```
AppHost.cs:
var apiService = builder.AddProject<Projects.Demo51_ApiService>("apiservice");
var web = builder.AddProject<Projects.Demo51_Web>("webfrontend")
    .WithReference(apiService);  // Automatic service discovery!

// In webfrontend:
// HttpClient automatically resolves apiservice → http://apiservice
```

**Use Cases:**
- Local development of microservices
- Cloud-native app templates
- Observability from day one
- Reproducible infrastructure as code

**Strengths:**
- ✅ No manual service discovery config
- ✅ Unified logs/metrics dashboard
- ✅ C# first (no YAML)
- ✅ Easy containerization

**Weaknesses:**
- ❌ Relatively new (potential breaking changes)
- ❌ Requires learning Aspire patterns
- ❌ Production story still evolving

**Related Patterns:**
- Distributed Modular Monolith [#28-distributed-modular-monolith](#28-distributed-modular-monolith)

**Demo References:**
- demo5.1: AppHost + ServiceDefaults

---

## Pattern Usage by Demo

### Patterns Introduced in demo1

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Cookie-Based Authentication | ⭐ | [#1](#1-cookie-based-authentication-local-identity) |
| Service Abstraction | ⭐⭐ | [#18](#18-service-abstraction-pattern) |

**Focus:** Foundational authentication and render mode compatibility.

---

### Patterns Introduced in demo2

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Passkey Authentication | ⭐⭐ | [#2](#2-passkey-authentication-webauthn) |
| Cascading Authentication State | ⭐⭐ | [#25](#25-cascading-authentication-state) |
| InteractiveAuto Progression | ⭐⭐ | [#26](#26-interactiveauto-render-mode-progression) |

**Focus:** Production-ready passkeys and authentication diagnostics.

---

### Patterns Introduced in demo3

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Role-Based Access Control | ⭐⭐ | [#5](#5-role-based-access-control-rbac) |
| Claims Transformation | ⭐⭐ | [#6](#6-claims-transformation) |
| Permission-Based RBAC | ⭐⭐⭐ | [#8](#8-permission-based-rbac) |
| Authorization Handlers | ⭐⭐ | [#9](#9-authorization-handlers) |
| Backend-for-Frontend (BFF) | ⭐⭐ | [#12](#12-backend-for-frontend-bff) |
| AuthorizeView for UI | ⭐ | [#27](#27-authorizeview-for-ui-authorization) |

**Focus:** Fine-grained permission-based authorization with BFF pattern.

---

### Patterns Introduced in demo4

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| OpenID Connect (OIDC) | ⭐⭐⭐ | [#3](#3-openid-connect-oidc-external-provider) |
| Auto-Provisioning | ⭐⭐⭐ | [#7](#7-auto-provisioning) |
| Claims Mapping | ⭐⭐⭐ | [#11](#11-claims-mapping) |

**Focus:** External identity provider integration with automatic user provisioning.

---

### Patterns Introduced in demo5

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| On-Behalf-Of (OBO) Flow | ⭐⭐⭐ | [#4](#4-on-behalf-of-obo-flow) |
| OAuth Scopes | ⭐⭐⭐ | [#10](#10-oauth-scopes) |
| Downstream API Pattern | ⭐⭐⭐ | [#13](#13-downstream-api-pattern) |
| Bearer Token Validation | ⭐⭐⭐ | [#15](#15-bearer-token-validation) |

**Focus:** Custom downstream APIs with bearer token authentication and OBO.

---

### Patterns Introduced in demo5.1

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| YARP Proxy Pattern | ⭐⭐⭐ | [#14](#14-yarp-reverse-proxy-pattern) |
| Distributed Modular Monolith | ⭐⭐⭐⭐ | [#28](#28-distributed-modular-monolith) |
| .NET Aspire Orchestration | ⭐⭐⭐ | [#29](#29-net-aspire-orchestration) |

**Focus:** Production-ready distributed monolith with service orchestration.

---

### Patterns Planned for demo6

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Finbuckle Multi-Tenant | ⭐⭐⭐ | [#21](#21-finbuckle-multi-tenant) |
| Outbox Pattern | ⭐⭐⭐ | [#16](#16-outbox-pattern) |
| Inbox Pattern | ⭐⭐⭐ | [#17](#17-inbox-pattern) |
| Structured Logging | ⭐⭐ | [#23](#23-structured-logging-with-correlation-ids) |

**Focus:** Multi-tenant data isolation with reliable event publishing.

---

### Patterns Planned for demo7

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Feature Flags | ⭐⭐ | [#22](#22-feature-flags-feature-management) |
| OpenTelemetry Integration | ⭐⭐⭐ | [#24](#24-opentelemetry-integration) |
| Choreographed Saga (Foundation) | ⭐⭐⭐⭐ | [#19](#19-choreographed-saga) |

**Focus:** Feature management and production hardening.

---

### Patterns Planned for demo8

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| Choreographed Saga | ⭐⭐⭐⭐ | [#19](#19-choreographed-saga) |

**Focus:** Full choreographed saga with MassTransit.

---

### Patterns Planned for demo9

| Pattern | Complexity | Reference |
|---------|-----------|-----------|
| State Machine Saga | ⭐⭐⭐⭐⭐ | [#20](#20-state-machine-saga) |

**Focus:** Orchestrated workflows with state machine sagas.

---

## Pattern Selection Framework

### Choosing Authentication

| Scenario | Pattern | Why |
|----------|---------|-----|
| Simple web app, single frontend | Cookie | Low overhead, simple model |
| High security requirement | Passkey | Phishing-resistant, modern |
| Enterprise/federated | OIDC | Centralized identity, compliance |
| Multiple auth sources | Multi-Identity | Flexibility, gradual migration |
| API for external clients | Bearer Token | Stateless, reusable |

### Choosing Authorization

| Scenario | Pattern | Why |
|----------|---------|-----|
| 3-5 roles, simple hierarchy | RBAC | Simple to understand and implement |
| 20-500 permissions, business rules | Permission-Based RBAC | Scalable, auditable, flexible |
| Complex conditional logic | Authorization Handlers | Custom rules, testable |
| API access control | OAuth Scopes | Clear contract, user consent |
| Multi-tenant, per-tenant rules | Multi-Tenant + RBAC | Tenant isolation + fine-grained control |

### Choosing API Architecture

| Scenario | Pattern | Why |
|----------|---------|-----|
| Monolithic, single frontend | BFF | Tight coupling OK, simpler security |
| Microservices, multiple clients | Downstream API | Loose coupling, reusable APIs |
| Distributed monolith, one BFF | BFF + YARP | Clean separation, transparent routing |
| Legacy systems, gradual migration | BFF + Downstream | Hybrid approach, flexible integration |

### Choosing Data Patterns

| Scenario | Pattern | Why |
|----------|---------|-----|
| Events must be published reliably | Outbox | Atomic with state, guaranteed delivery |
| Consumers might retry messages | Inbox | Idempotent processing, no duplicates |
| Simple eventual consistency | Choreographed Saga | Distributed workflow, no coordinator |
| Complex workflows, visibility needed | State Machine Saga | Central state, observability, control |

---

## Cross-Cutting Concerns

### Observability Across Patterns

All patterns benefit from:
- **Structured Logging:** Context + correlation IDs
- **OpenTelemetry:** Traces + metrics + logs
- **Health Checks:** Service readiness/liveness
- **Dashboards:** Real-time monitoring

### Security Considerations

Every pattern must address:
- **HTTPS Everywhere:** No HTTP in production
- **Token Rotation:** Refresh before expiration
- **Scope Validation:** Least-privilege principle
- **Audit Trails:** Log who did what when
- **CORS Security:** Explicit origin validation
- **PII Protection:** Scrub sensitive data from logs

### Performance & Scaling

Pattern implications:
- **Stateless > Stateful:** Easier to scale
- **Caching:** Token caching, permission caching, query caching
- **Async/Await:** Non-blocking I/O
- **Batching:** Reduce roundtrips
- **Monitoring:** Identify bottlenecks

---

## References

For detailed implementations of each pattern, refer to:
- Individual demo README files
- `.docs/research/` for architectural analysis
- `.docs/issues/` for specific decisions
- Source code in each demo folder

---

**Last Updated:** 2026-01-12  
**Next Review:** After demo6 completion
