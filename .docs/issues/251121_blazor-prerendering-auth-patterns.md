# Blazor Prerendering DI and Authorization Patterns

**Date:** 2025-11-21
**Issue Type:** Architecture Decision
**Severity:** High
**Status:** Resolved

## 📋 Summary

Resolved critical runtime exceptions in a Blazor InteractiveAuto application related to Dependency Injection (DI) during server-side prerendering and established a consistent authorization strategy across Server and Client boundaries.

## 🔍 Analysis / Context

* **Prerendering Failure**: The application crashed with `InvalidOperationException` because `HttpClient` was injected into components but was not registered in the Server DI container. Prerendering executes on the server, where `HttpClient` is typically not used for local API calls.
* **Render Mode Mismatch**: `System.NotSupportedException` occurred when attempting to render an `@rendermode InteractiveWebAssembly` component inside a Server-rendered page.
* **Context Confusion**: `InvalidOperationException` occurred when services tried to call `AuthenticationStateProvider.GetAuthenticationStateAsync()` within a Minimal API endpoint (outside a Blazor Circuit).
* **Authorization Inconsistency**: Initial attempts to manually check permissions in services led to complex code handling both `HttpContext` (API) and `AuthenticationState` (Blazor).

## ✅ Resolution / Decision

1. **Service Abstraction Pattern**:
   * Defined shared interfaces (e.g., `IUserService`) in the Client project.
   * Implemented `ClientUserService` using `HttpClient` for WASM.
   * Implemented `ServerUserService` using `UserManager`/`DbContext` for Server/Prerendering.
   * Registered respective implementations in Client and Server `Program.cs`.
2. **Unified Authorization Policies**:
   * Moved away from manual service-level checks.
   * Registered Authorization Policies in **both** `Demo3.BffRbac` (Server) and `Demo3.BffRbac.Client` (WASM).
   * Server policies use `PermissionRequirement` (DB/Logic check).
   * Client policies use `RequireClaim("permission", ...)` (Token check).
3. **Declarative Component Security**:
   * Applied `@attribute [Authorize(Policy = "...")]` to Blazor pages.
   * This ensures the router handles enforcement consistently in both environments.

## 📚 Lessons Learned

* **Abstract External Calls**: Never inject `HttpClient` directly into shared Blazor components if they might run on the server (Prerendering). Use an interface.
* **Dual Registration**: Shared components require dependencies (like Authorization Policies) to be registered in *both* the Server and Client DI containers.
* **Context Awareness**: API Endpoints use `HttpContext`; Blazor Circuits use `AuthenticationState`. Do not mix them. Services used by both must handle both contexts or (better yet) rely on the caller to handle authorization.
* **Render Modes**: `InteractiveAuto` is the preferred mode for components that need to prerender and then become interactive.

## 🛠️ Prevention / Implementation

* **Check**: Ensure `Program.cs` in both projects registers all shared interfaces.
* **Pattern**: Use `IHttpContextAccessor` only in API-specific services, not in shared Blazor services.
* **Code Snippet (Client Policy Registration)**:

```csharp
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("users.read", policy => policy.RequireClaim("permission", "users.read"));
});
```

## 🔗 Related Files

* `demo3/Demo3.BffRbac/Program.cs` (Server Registration)
* `demo3/Demo3.BffRbac.Client/Program.cs` (Client Registration)
* `demo3/Demo3.BffRbac/Services/ServerServices.cs` (Server Implementation)
* `demo3/Demo3.BffRbac.Client/Services/ClientServices.cs` (Client Implementation)

## 🏷️ Tags

`blazor` `dotnet` `architecture-decision` `troubleshooting` `configuration`
