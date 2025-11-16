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

## What's New

- Dedicated `AuthStateProbe.razor` page with incremental timeline visualization showing the complete 4-phase InteractiveAuto lifecycle
- Visual delay controls via `RenderDelayMs` query parameter to slow down UI updates, making each phase transition observable
- Real-time status indicators with spinners showing when delays are active between timeline events
- `<CascadingAuthenticationState>` wrapping the entire app so authentication flows through all render phases
- Reusable `AuthStateSurface` diagnostic component, rendered once with `InteractiveServer` and once with `InteractiveWebAssembly`, to prove that Identity cookies guard both render modes simultaneously
- **Key learning**: InteractiveAuto uses progressive enhancement on FIRST VISIT ONLY: (1) Server prerender → (2) Server interactive via SignalR (fallback while WASM downloads) → (3) WASM initialized → (4) WASM first render complete. On subsequent visits, Blazor checks Local Storage (`blazor-resource-hash` key) to determine if WASM is cached, skipping directly to WASM (phases 1, 3, 4 only).
- **Critical discovery**: WASM cache state is tracked in browser **Local Storage**, not just HTTP cache. DevTools "Disable cache" doesn't affect Local Storage!
- **Testing tip**: To observe all 4 phases including SignalR, delete `blazor-resource-hash` from Local Storage (DevTools → Application tab) or use Incognito/Private mode on your FIRST visit to the page
- Updated navigation and detailed inline documentation with accurate phase descriptions, Local Storage caching behavior, and precise cache-clearing instructions
