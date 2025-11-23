# Blazor Project Structure Standardization

**Date:** 2025-11-23
**Issue Type:** Architecture Decision
**Severity:** Medium
**Status:** Resolved

## 📋 Summary

This document establishes the standard project structure for Blazor Web Apps in this workspace, specifically addressing the unification of Client/Server component hierarchies and the placement of feature-specific UI components. The decision was made during the refactoring of `demo3` to align with .NET 10 best practices.

## 🔍 Analysis / Context

* **Inconsistency**: The initial `demo3` structure was a hybrid of legacy Blazor WASM (root `Pages/`) and modern Blazor Web App (root `Components/Pages/`) styles.
* **Refactoring Friction**: Moving a component from Client (WASM) to Server (SSR) required changing namespaces and folder paths.
* **Root Clutter**: Feature-specific folders like `Diagnostics/` were placed at the project root, mixing architectural concerns (Program.cs) with UI features.
* **Ambiguity**: There was uncertainty about where to place helper components like `AuthStateSurface` (Shared vs. Feature folder).

## ✅ Resolution / Decision

We have adopted a **Unified Component Architecture** based on the principle: *"The Project Root is for Architecture. The `Components` folder is for UI."*

1. **Unified Folder Structure**:
   * **Server**: `Components/Pages`, `Components/Layout`
   * **Client**: `Components/Pages`, `Components/Layout` (Mirrors Server)
   * **Benefit**: Seamless portability of components between rendering modes.

2. **Shared Library Strategy**:
   * **Models/DTOs**: Moved to a dedicated class library (`Demo3.BffRbac.Shared`) to prevent Server-to-Client project dependencies.

3. **Feature Component Placement**:
   * **Decision**: `AuthStateSurface.razor` was placed in `Components/Diagnostics/`, NOT `Components/Shared/`.
   * **Reasoning**: `Shared` is reserved for **domain-agnostic** widgets (Buttons, Cards). `AuthStateSurface` is **domain-specific** (tied to Auth logic) and belongs in a semantic feature folder.

## 📚 Lessons Learned

* **Symmetry is Key**: Keeping Server and Client structures identical reduces cognitive load when switching contexts.
* **Scope of Reusability**: Just because a component is used by a page doesn't mean it belongs in `Shared`. If it's tied to a specific feature (like Diagnostics), keep it with that feature.
* **Root Hygiene**: Keep the project root clean. If it renders HTML, it belongs inside `Components/`.

## 🛠️ Prevention / Implementation

**Decision Matrix for New Files:**

| File Type         | Question                  | Location                    |
| :---------------- | :------------------------ | :-------------------------- |
| **UI Component**  | Is it a Page?             | `Components/Pages/`         |
|                   | Is it a Layout?           | `Components/Layout/`        |
|                   | Is it a generic widget?   | `Components/Shared/`        |
|                   | Is it a specific feature? | `Components/[FeatureName]/` |
| **Logic/State**   | Is it business logic?     | `Services/`                 |
| **Data Contract** | Is it a DTO/Model?        | `[Project].Shared/Models/`  |

## 🔗 Related Files

* `demo3/Demo3.BffRbac.Client/Components/Diagnostics/AuthStateSurface.razor`
* `demo3/Demo3.BffRbac.Client/Components/Pages/AuthStateProbe.razor`
* `demo3/Demo3.BffRbac.Shared/`

## 📖 Additional Resources

* [ASP.NET Core Blazor project structure (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/blazor/project-structure)

## 🏷️ Tags

`blazor` `architecture-decision` `dotnet` `refactoring` `best-practices`
