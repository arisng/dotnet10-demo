# Demo 3 Architecture and Security Understanding

**Date:** 2025-11-22
**Issue Type:** Learning Insight
**Severity:** Medium
**Status:** Documented

## 📋 Summary

This document outlines key questions and answers designed to validate a developer's understanding of the Demo 3 project, which implements a Backend-for-Frontend (BFF) pattern with fine-grained permission-based authorization (RBAC) in .NET 10. It serves as a knowledge check for the architectural transition from simple authentication to complex authorization.

## 🔍 Analysis / Context

To ensure a solid grasp of the architectural decisions and security models introduced in Demo 3, the following questions were formulated. These cover architecture, authorization internals, implementation details, and future extensibility.

### Key Questions

1. **BFF Rationale**: Why choose a monolithic BFF pattern instead of a separated client-server architecture?
2. **Service Abstraction**: How does the Service Abstraction Pattern solve the `HttpClient` dependency issue in prerendering?
3. **Cookie vs. Token**: What are the security advantages of using cookies over tokens in this specific setup?
4. **Claims Transformation Flow**: When and how does `IClaimsTransformation` augment the user identity?
5. **Role vs. Permission**: Why authorize against granular permissions instead of roles?
6. **Performance Implications**: How do we mitigate the performance cost of database lookups on every request?
7. **Minimal API Security**: How is the custom `RequirePermission` extension implemented?
8. **Client-Side Error Handling**: How does the client distinguish between 401 and 403?
9. **.NET 10 Features**: How do `AddAuthorizationBuilder` and automatic 401/403 handling improve the code?
10. **Identity Agnosticism**: How does this prepare us for external identity providers in Demo 4?

## ✅ Resolution / Decision

Below are the answers and architectural decisions corresponding to the questions above.

### 1. BFF Rationale

A monolithic BFF was chosen to simplify the security model. By keeping the API and the frontend in the same domain/process, we can use **Cookie Authentication** instead of managing tokens (JWTs) in the browser. This eliminates the risk of token theft (XSS) and avoids complex CORS configurations. It acts as a "true" BFF where the server-side component proxies and secures access to data.

### 2. Service Abstraction Pattern

The Service Abstraction Pattern (e.g., `IWeatherService`) decouples the UI from the data fetching mechanism.

* **Server (Prerendering)**: The `ServerWeatherService` implementation injects the database context or service directly, avoiding an HTTP call to itself (which would fail or be inefficient during prerendering).
* **Client (WASM)**: The `ClientWeatherService` implementation uses `HttpClient` to call the BFF API.

This ensures the same Razor component works seamlessly in both environments without `if (OperatingSystem.IsBrowser())` checks.

### 3. Cookie vs. Token

Cookies (specifically `HttpOnly`, `Secure`, `SameSite=Strict`) are managed automatically by the browser and cannot be accessed by JavaScript, making them immune to XSS attacks that try to steal credentials. Tokens stored in `localStorage` or `sessionStorage` are accessible to JS and vulnerable. For a Blazor WASM app served from the same origin, cookies provide a more robust default security posture.

### 4. Claims Transformation Flow

`IClaimsTransformation` runs **after** successful authentication but **before** authorization checks.

1. User logs in (cookie validated).
2. `PermissionClaimsTransformation.TransformAsync` is called.
3. It reads the user's ID, queries the database for assigned Roles, and expands them into granular Permissions.
4. These permissions are added as `permission` claims to the `ClaimsPrincipal` for the duration of the request.

### 5. Role vs. Permission

We authorize against **Permissions** (e.g., `weather.read`) to decouple code from business logic. If we checked for `Roles="Admin"`, changing what an Admin can do would require code changes. By checking `RequirePermission("weather.read")`, we can change the *mapping* of Admin -> `weather.read` in the database without touching the compiled code. It allows for flexible, data-driven security policies.

### 6. Performance Implications

Database hits on every request can be costly. Strategies include:

* **Caching**: Cache the User -> Permissions mapping (e.g., in Redis or MemoryCache) with a sliding expiration. Invalidate cache on role changes.
* **User Claims**: Store permissions directly in the authentication cookie (if the list is small) to avoid DB lookups entirely, though this increases cookie size.

### 7. Minimal API Security

The `RequirePermission` extension method builds a standard ASP.NET Core `AuthorizationPolicy`. It adds a `PermissionRequirement` to the endpoint's metadata. The `PermissionAuthorizationHandler` then checks the current `ClaimsPrincipal` for the specific `permission` claim. If missing, the framework returns 403.

### 8. Client-Side Error Handling

The client `HttpClient` checks the status code.

* **401**: Means the user is not authenticated (cookie missing/expired). The app redirects to login.
* **403**: Means the user is authenticated but lacks the specific permission. The app shows a "Forbidden" message or UI element.

Returning status codes (API behavior) instead of 302 Redirects (HTML behavior) is crucial for the `HttpClient` to programmatically detect the failure reason.

### 9. .NET 10 Features

* `AddAuthorizationBuilder()`: Provides a cleaner, more discoverable fluent API for registering policies, replacing the older nested delegate pattern.
* **Auto 401/403**: .NET 10's cookie handler automatically detects API endpoints (via metadata) and suppresses the default behavior of redirecting 401s to the login page, ensuring APIs behave like APIs (returning JSON/Status codes) without custom event plumbing.

### 10. Identity Agnosticism

The authorization system relies on the `ClaimsPrincipal`. It doesn't care *how* the user got there (Passkey, Password, Entra ID). As long as the identity provider resolves to a User ID that exists in our `AspNetUserRoles` table, the `IClaimsTransformation` will attach the correct permissions. This allows us to swap or add auth providers (Demo 4) without rewriting a single line of authorization logic.

## 📚 Lessons Learned

* **Decoupling**: Separating Authorization (Permissions) from Authentication (Identity) is critical for scalable systems.
* **Security**: BFF patterns with cookies significantly reduce client-side security complexity compared to token management.
* **Architecture**: Service Abstraction is mandatory for clean Blazor Hybrid/Prerendering architectures to avoid "hairpin" HTTP calls.

## 🛠️ Prevention / Implementation

* **Implementation**: Always use `RequirePermission` on new endpoints; do not rely on implicit role checks.
* **Configuration**: Ensure `IClaimsTransformation` is registered as Scoped.
* **Observability**: Monitor `aspnetcore.authorization` metrics to detect permission failures or attack attempts.

## 🔗 Related Files

* `Demo3.BffRbac/Authorization/PermissionClaimsTransformation.cs`
* `Demo3.BffRbac/Authorization/AuthorizationExtensions.cs`
* `Demo3.BffRbac/Program.cs`

## 📖 Additional Resources

* [Claims-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims)
* [Backend for Frontend pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends)

## 🏷️ Tags

`dotnet` `blazor` `architecture-decision` `security` `authorization` `bff`
