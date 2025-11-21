# UI Flicker in UserManagement Component During Wasm Interactive Rendering

**Date:** 2025-11-21
**Issue Type:** Technical Issue
**Severity:** Medium
**Status:** Resolved

## 📋 Summary

The UserManagement.razor component exhibited UI flicker, showing "Loading..." momentarily when transitioning from server-side prerendering to client-side rendering in InteractiveAuto mode. This was resolved by applying the [PersistentState] attribute to persist user data across renders, eliminating redundant API calls and visual disruptions.

## 🔍 Analysis / Context

- In Blazor's InteractiveAuto render mode, components are prerendered on the server for initial HTML, then hydrated on the client.
- Async data loading in OnInitializedAsync caused the component to render with "Loading..." initially, then re-render with data after client-side execution.
- Without state persistence, this led to flicker and potential performance issues from duplicate API requests.

## ✅ Resolution / Decision

Applied the [PersistentState] attribute to the Users property and updated data loading logic to use null-coalescing assignment (??=). This ensures user data is fetched only once during prerendering and persisted for client-side use, preventing flicker and improving user experience.

## 📚 Lessons Learned

- [PersistentState] is essential for async-loaded data in prerendered Blazor components to maintain visual continuity.
- Always use ??= for conditional data loading when state might already be persisted.
- Test components in Interactive modes to catch prerendering-related issues early.

## 🛠️ Prevention / Implementation

- For new components with async data: Add [PersistentState] to relevant properties and use ??= in OnInitializedAsync.
- Review existing components for flicker by enabling prerendering and testing in InteractiveAuto mode.
- Ensure properties marked with [PersistentState] are public to allow framework access.

## 🔗 Related Files

- `demo3/Demo3.BffRbac.Client/Pages/UserManagement.razor` (lines 40-65 for property and loading logic)

## 📖 Additional Resources

- [.NET 10 Blazor State Management Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence)
- [PersistentState Attribute Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.persistentstateattribute)

## 🏷️ Tags

blazor, dotnet, ui-flicker, persistent-state, wasm, interactive-rendering, prerendering
