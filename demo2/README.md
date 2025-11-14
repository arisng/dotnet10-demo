# demo2 – Dual-Mode Authentication State Handoff

## Goal

Instrument the baseline Identity app so you can watch authentication state travel from the server prerender phase to the InteractiveAuto WebAssembly instance. The new diagnostics answer “is my cookie still valid when the client reconnects?” by logging the claims principal at every step and rendering server-only and WASM-only components side-by-side.

## Prerequisites

- Completion of **demo1 – Identity Foundation** (the database and passkey-ready Identity bits are reused here)
- .NET 10 SDK (Preview) with EF Core tools (`dotnet tool install --global dotnet-ef` if needed)
- A browser with DevTools network tracing so you can confirm when the WASM handoff occurs
- Basic familiarity with Blazor render modes and the Identity UI scaffolding from demo1

## How to Run

1. Apply the Identity migration inside the cloned demo2 solution:

```powershell
cd demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff
dotnet ef database update
```

1. Launch the server with hot reload:

```powershell
dotnet watch
```

1. Visit `https://localhost:7210`, register/sign in, then open the **Auth State Probe** entry from the nav menu. Keep the DevTools Network tab open so you can correlate SignalR reconnections with the timeline rendered on the page.
1. Trigger a refresh or navigate between components that use `InteractiveServer`, `InteractiveAuto`, and `InteractiveWebAssembly`. Confirm that both the server and WASM-only panels show the same claims and that the timeline logs a **WASM handshake** event.

## What’s New

- Dedicated `AuthStateProbe.razor` page that subscribes to `AuthenticationStateProvider.AuthenticationStateChanged`, logs checkpoints (“Server prerender”, “Server render completed”, “WASM handshake”), and renders a visual timeline.
- `<CascadingAuthenticationState>` now wraps the entire app so authentication flows through prerender and reconnect phases, and the probe page logs the client reconnection inside `OnAfterRenderAsync`.
- Reusable `AuthStateSurface` diagnostic component, rendered once with `InteractiveServer` and once with `InteractiveWebAssembly`, to prove that Identity cookies guard both render modes simultaneously.
- Updated navigation and docs so teams know exactly where to observe the dual-mode handoff before moving on to passkeys in demo3.
