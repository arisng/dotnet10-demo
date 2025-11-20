# demo2 – Dual-Mode Authentication State Handoff

## Goal

Master authentication state flow through InteractiveAuto phases AND implement complete passkey infrastructure, establishing the **comprehensive baseline** for all subsequent demos.

This demo serves dual purposes:
1. **Diagnostics:** Watch authentication state travel from server prerender to WASM handoff with detailed lifecycle visualization
2. **Passkeys:** Implement the full .NET 10 passkey experience (registration, management, passwordless login)

The diagnostics answer "is my cookie still valid when the client reconnects?" by logging the claims principal at every step. The passkey implementation provides the production-ready authentication foundation that demo3-6 will build upon with BFF APIs, Entra ID integration, and permission-based authorization.

## Prerequisites

- Completion of **demo1 – Identity Foundation** (the database and passkey-ready Identity bits are reused here)
- .NET 10 SDK (Preview) with EF Core tools (`dotnet tool install --global dotnet-ef` if needed)
- A browser with DevTools network tracing so you can confirm when the WASM handoff occurs
- Basic familiarity with Blazor render modes and the Identity UI scaffolding from demo1

## How to Run

### Development Mode (Standard)

1. Apply the Identity migration inside the cloned demo2 solution:

```powershell
cd demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff
dotnet ef database update
```

2. Launch the server with hot reload:

```powershell
dotnet watch
```

3. Visit `https://localhost:7210`, register/sign in, then open the **Auth State Probe** entry from the nav menu. Keep the DevTools Network tab open so you can correlate SignalR reconnections with the timeline rendered on the page.

4. Trigger a refresh or navigate between components that use `InteractiveServer`, `InteractiveAuto`, and `InteractiveWebAssembly`. Confirm that both the server and WASM-only panels show the same claims and that the timeline logs a **WASM handshake** event.

### Published Mode (WASM Caching Enabled)

To observe proper HTTP caching behavior with `Cache-Control: max-age=31536000, immutable` and `304 Not Modified` responses:

```powershell
# From the demo2/Demo2.DualModeHandoff directory
cd Demo2.DualModeHandoff

# Publish the app in Release configuration
dotnet publish -c Release

# Navigate to the publish folder
cd bin\Release\net10.0\publish

# Run the published app
dotnet Demo2.DualModeHandoff.dll --urls "https://*:7210;http://*:5210"
```

**What to verify:**
- Open DevTools → Network tab (uncheck "Disable cache")
- First visit: WASM files show `200 OK` with `Cache-Control: max-age=31536000, immutable`
- Refresh page: WASM files show `304 Not Modified` or `(from disk cache)`
- Files are properly cached and not re-downloaded

**Note:** WASM HTTP caching (`max-age=31536000`) only works in published builds. During development (`dotnet run`/`dotnet watch`), WASM files use `max-age=0` to support Hot Reload and incremental compilation. This is expected behavior. See `.docs/issues/251116-blazor-wasm-http-caching.md` for details.

## Why demo2 Is the Real Baseline

While demo1 provides the initial scaffolding, demo2 represents the **true starting point** for production development:
- ✅ Complete passkey implementation ready for real users
- ✅ Authentication diagnostics for troubleshooting complex scenarios
- ✅ Production published mode validation with HTTP caching
- ✅ Foundation for BFF APIs (demo3), Entra ID integration (demo4-5), and production hardening (demo6)

Think of demo1 as "proof of concept" and demo2 as "production baseline."

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
