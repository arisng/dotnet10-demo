# demo1 – Identity Foundation

## Goal

Seed the workspace with a Blazor Web App that keeps ASP.NET Core Identity cookies valid regardless of render mode. The app ships with login, registration, manage profile, and passkey-ready infrastructure out of the box, so later demos can focus on incremental hardening rather than scaffolding. You will get comfortable switching between `InteractiveServer`, `InteractiveAuto`, and `InteractiveWebAssembly` components to prove that authentication state flows consistently.

## Prerequisites

- .NET 10 SDK (Preview) with the matching ASP.NET Core workload
- SQLite tooling (bundled with the SDK) and the `dotnet-ef` CLI
- A browser with WebAuthn support if you want to experiment with passkey placeholders early
- Basic familiarity with running `dotnet watch` and inspecting console output for EF Core migrations

## How to Run

1. Restore packages and ensure the development database exists:

```powershell
cd demo1/Demo1.IdentityFoundation/Demo1.IdentityFoundation
dotnet ef database update
```

1. Launch the server with hot reload:

```powershell
dotnet watch
```

1. Visit `https://localhost:7210`, register a user, then sign out/in again to confirm cookies survive page refreshes.
1. Open `Components/App.razor` and toggle pages between `@rendermode InteractiveServer`, `@rendermode InteractiveAuto`, and `@rendermode InteractiveWebAssembly`. The same identity cookie and `AuthenticationState` should remain valid in every mode.
1. Optional: open the browser dev tools Network tab to watch the switch from server-side websockets to WASM-only requests when running `InteractiveAuto`. This becomes important evidence for demo2’s diagnostics.

Once you are satisfied, copy this folder forward to start demo2.
