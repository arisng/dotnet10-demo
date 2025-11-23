# Blazor Auth State Persistence in InteractiveAuto

**Date:** 2025-11-23
**Issue Type:** Learning Insight
**Severity:** Medium
**Status:** Documented

## 📋 Summary

In a Blazor Web App using `InteractiveAuto` render mode, the client-side (WASM) authentication state appeared as "Anonymous" despite a successful server-side login. This was caused by a mismatch in the `RenderMode` used for state persistence registration and incorrect usage of the `AuthenticationStateProvider` in client components.

## 🔍 Analysis / Context

* **Scenario**: A BFF (Backend for Frontend) architecture where the server handles login and passes state to the client via `PersistentComponentState`.
* **Root Cause 1 (Client - Primary)**: The `PersistentAuthenticationStateProvider` uses `TryTakeFromJson` to retrieve user data. This method **removes** the data from the store upon retrieval. The framework's `CascadingAuthenticationState` component calls `GetAuthenticationStateAsync` first, consuming the token. When the user's component subsequently called `GetAuthenticationStateAsync` manually, the token was gone, resulting in an "Anonymous" state.
* **Root Cause 2 (Server - Secondary)**: The `RenderMode` parameter in `RegisterOnPersisting` filters *when* the state should be serialized.
  * `RenderMode.InteractiveWebAssembly`: Persists state only when the component will be rendered interactively on WebAssembly.
  * `RenderMode.InteractiveAuto`: Persists state for both Server and WebAssembly interactive modes.
  * **Why it worked**: Even though the app runs in `InteractiveAuto`, the persistence mechanism correctly identifies that the *target* for the state transfer is the WebAssembly client. Therefore, registering with `InteractiveWebAssembly` is sufficient and functionally correct for the handoff.
* **Key Insight**: `PersistentComponentState` data is **single-use**. Direct calls to `GetAuthenticationStateAsync` in client components will fail if the state has already been consumed by the framework. `[CascadingParameter]` is mandatory to share the single resolved state instance.

## ✅ Resolution / Decision

* **Client Fix**: Refactored client components to use `[CascadingParameter] private Task<AuthenticationState> AuthState { get; set; }`. This ensures the component uses the *same* authentication state instance already resolved by the framework, rather than attempting to consume the persistent token again.
* **Server Configuration**: Kept `RenderMode.InteractiveWebAssembly` in `RegisterOnPersisting`. This is valid because the primary goal of this persistence is to hydrate the WebAssembly client.

## 📚 Lessons Learned

* **Single-Use Persistence**: `PersistentComponentState.TryTakeFromJson` is a destructive read. Once the authentication provider (driven by `CascadingAuthenticationState`) reads the user info, it is removed from the store.
* **Avoid Direct Injection**: Never inject `AuthenticationStateProvider` to fetch state in Blazor Client components if that provider relies on `PersistentComponentState`. Subsequent calls will find an empty store.
* **Always Use CascadingParameter**: This pattern ensures all components share the single, consistent authentication state result managed by the framework.
* **RenderMode Parameter**:
  * **Can it be omitted?** Yes, `RegisterOnPersisting` has an overload without parameters. If omitted, the callback runs for *all* render modes.
  * **Why specify it?** Specifying `RenderMode` (e.g., `InteractiveWebAssembly`) is an optimization. It ensures the expensive serialization logic only runs when necessary (i.e., when sending data to the client), avoiding overhead during purely server-side rendering scenarios where the state is already present in memory.

## 🛠️ Prevention / Implementation

* Review `PersistingServerAuthenticationStateProvider` in new projects to ensure `RenderMode` matches the project configuration.
* Use the `AuthorizeView` component or `[CascadingParameter]` as the standard pattern for auth-dependent UI.

## 🔗 Related Files

* `demo3/Demo3.BffRbac/Components/Account/PersistingServerAuthenticationStateProvider.cs`
* `demo3/Demo3.BffRbac.Client/Pages/AuthStateProbe.razor`

## 📖 Additional Resources

* [PersistentComponentState.RegisterOnPersisting Method (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.persistentcomponentstate.registeronpersisting) - Confirms the existence of overloads with and without `RenderMode`.
* [ASP.NET Core Blazor prerendered state persistence (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence) - Discusses state persistence strategies and the `InteractiveAuto` mode.
* [Secure data in Blazor Web Apps with Interactive Auto rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0#secure-data-in-blazor-web-apps-with-interactive-auto-rendering)

## 🏷️ Tags

`blazor` `dotnet` `authentication` `troubleshooting` `interactive-auto` `wasm` `state-management` `learning-insight` `medium-priority`
