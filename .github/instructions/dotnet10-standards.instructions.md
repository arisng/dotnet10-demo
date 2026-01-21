---
description: 'Coding standards and best practices for .NET 10 and C# 14 development, including ASP.NET Core, EF Core, and Blazor patterns.'
applyTo: '**/*.cs, **/*.razor, **/*.csproj, **/*.slnx'
---

# .NET 10 & C# 14 Coding Standards

## Overview
This document defines the mandatory coding standards and architectural patterns for .NET 10 development in this workspace. GitHub Copilot must adhere to these rules to ensure consistency across all demos.

## Rules & Constraints
- **Solution Format:** Always use the XML-based solution format (`.slnx`) instead of the legacy `.sln` format.
- **Namespaces:** Must match the project name exactly (e.g., `namespace Demo3.BffRbac;`).
- **Permissions:** Use lowercase dot-notation (e.g., `weather.read`, `orders.create`).

## Code Standards

### C# 14 Language Features
- **Backing Fields:** Use the `field` keyword in properties for compiler-generated backing fields.
  ```csharp
  public string Name { get; set => field = value.Trim(); }
  ```
- **Extension Members:** Use the `extension` syntax for adding properties and static methods.
- **Null-Conditional Assignment:** Use `?.` for assignments.
  ```csharp
  options?.Timeout = 30;
  ```
- **nameof for Generics:** Use `nameof(List<>)` for unbound generic types.

### ASP.NET Core 10
- **OpenAPI:** Standardize on **OpenAPI 3.1** using native generation.
- **Minimal APIs:** 
  - Use `AddAuthorizationBuilder()` for policy registration.
  - Use `.RequirePermission("permission.name")` for RBAC.
- **Identity:** Always use `IdentitySchemaVersions.Version3` for Passkey support.
- **Dependency Injection:** Use **Keyed Services** (`[FromKeyedServices("name")]`) for multiple implementations.

### EF Core 10
- **LINQ Joins:** Use `.LeftJoin()` and `.RightJoin()` operators.
- **Query Filters:** Use **Named Query Filters** for multi-tenant or soft-delete logic.
  ```csharp
  context.Blogs.IgnoreQueryFilters(["SoftDeleteFilter"]).ToListAsync();
  ```
- **Raw SQL:** Use `FromSqlInterpolated` or `FromSql` with parameters. Avoid `FromSqlRaw`.

## Best Practices

### Blazor & Frontend
- **Interactivity:** Default to `InteractiveAuto` render mode.
- **Performance:** Enable **WASM preloading** in the host page.
- **Service Abstraction:** Use the **Prerender-Safe Service Abstraction** pattern:
  - Interfaces in `.Shared` or `.Client`.
  - `ServerService` in Server project.
  - `ClientService` (using `HttpClient`) in `.Client` project.

### Architecture
- **Vertical Slices:** Organize code by feature (e.g., `Features/Orders/`) rather than technical layers.
- **BFF Pattern:** All external API calls from WASM must go through the Server's `/api/*` endpoints.
- **Entra ID & OBO:** Use `Microsoft.Identity.Web` with the **On-Behalf-Of (OBO)** flow for downstream APIs.

## Validation
- **Build:** Run `dotnet build` on the solution file (`.slnx`).
- **Commit:** Use the `git-atomic-commit` skill to group changes into logical atomic commits.
