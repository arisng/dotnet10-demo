# .NET 10 Workshop - AI Agent Instructions

## Workspace Scope

This workspace explores **.NET 10 features and capabilities**, starting with ASP.NET Core Identity as the foundation. While Identity (passkeys, BFF pattern, Entra ID) is the entry point, demos may expand to cover any .NET 10 topic—prioritizing features directly connected to .NET 10, but not limited to them.

**Current focus areas:** Identity, Blazor InteractiveAuto, Minimal APIs, authorization patterns, Entra ID integration.

## Architecture Overview

This is an **incremental demo workspace** where each `demo<N>` folder builds on the previous—**demo2 is the real baseline** (not demo1).

### Project Structure Pattern (demo3+)
```
demo<N>/
├── Demo<N>.<Name>/           # Server project (Blazor, APIs, Identity)
├── Demo<N>.<Name>.Client/    # WASM client project
├── Demo<N>.<Name>.Shared/    # Shared models/interfaces
└── README.md
```

### Key Data Flow (Identity demos)
```
User Auth (Passkey/Entra) → IClaimsTransformation (adds permission claims) 
  → PermissionAuthorizationHandler → API endpoints via .RequirePermission()
```

## Critical Patterns

### Service Abstraction (Prerendering DI)
WASM components work in both server prerender and client modes via interface abstraction:
```csharp
// Shared: IWeatherService interface
// Client: ClientWeatherService (uses HttpClient → /api/weather)
// Server: ServerWeatherService (direct DB access)
```
Register in `Program.cs`: Server uses `Server*` implementations; Client uses `Client*`.

### Permission-Based Authorization
- Roles aggregate to permissions via `RolePermission` junction table
- `PermissionClaimsTransformation` adds `permission` claims on each request
- API endpoints: `.RequirePermission("weather.read")` (see `Authorization/AuthorizationExtensions.cs`)
- Named policies in `Program.cs` for Blazor `[Authorize(Policy="weather.read")]`

### BFF vs Downstream API
- **BFF (demo3-4):** APIs in same project, cookie auth, no CORS
- **Downstream (demo5):** Separate `ProtectedApi` project on port 7220, Bearer tokens, OBO flow

## Developer Workflows

### Create New Demo
Use VS Code task: `Tasks: Run Task` → "Create New Demo (Copy Previous)"
Or: `.vscode/scripts/copy-demo.ps1 -NewDemoNumber 6 -DemoName MyFeature`

### Run Any Demo
```powershell
cd demo<N>/Demo<N>.<Name>
dotnet ef database update
dotnet watch
```
All demos run on `https://localhost:7210`. Demo5's ProtectedApi runs on `https://localhost:7220`.

### Seeded Test Users (demo3+)
| Email | Password | Role |
|-------|----------|------|
| admin@local.app | Admin123! | Admin |
| manager@local.app | Manager123! | Manager |
| user@local.app | User123! | User |

## Naming Conventions

- Solution: `Demo<N>.<PascalCaseName>.slnx`
- Projects: `Demo<N>.<Name>`, `Demo<N>.<Name>.Client`, `Demo<N>.<Name>.Shared`
- Namespace follows project name exactly
- Permissions: lowercase dot-notation (`weather.read`, `users.delete`)

## Key Files Reference

| Purpose | Location (demo3+ pattern) |
|---------|--------------------------|
| Auth flow | `Authorization/PermissionClaimsTransformation.cs` |
| Permission handler | `Authorization/PermissionAuthorizationHandler.cs` |
| API extension | `Authorization/AuthorizationExtensions.cs` |
| Service interfaces | `*.Client/Services/IAppServices.cs` |
| Data seeding | `Data/DbSeeder.cs` |
| Identity config | `Program.cs` (SchemaVersion3 for passkeys) |

## .NET 10 Framework Notes

- **Identity SchemaVersion:** Must use `IdentitySchemaVersions.Version3` for passkeys
- **Blazor InteractiveAuto:** 4 phases on first visit (SSR→SignalR→WASM init→WASM render), 3 phases when cached
- **WASM Caching:** HTTP caching (`max-age=31536000`) only works in published mode; dev uses `max-age=0`
- **Entra ID:** Uses `Microsoft.Identity.Web` with OBO flow for Graph/downstream APIs
- **Minimal APIs:** Use `AddAuthorizationBuilder()` fluent API for policy registration (.NET 10 pattern)
- **Static Web Assets:** Non-dev environments need `StaticWebAssetsLoader.UseStaticWebAssets()` for .Client assets

## Documentation Requirements

Each demo README must include: Goal, Prerequisites, How to Run, What's New (for demo2+).
Root `README.md` maintains the demo lineup table with focus, dependencies, and highlights.

### `.docs/` Folder Convention

The root `.docs/` folder supports AI-driven development workflow:

| Folder | Purpose | Naming Pattern |
|--------|---------|----------------|
| `issues/` | Investigation docs (GitHub issue format) with root cause analysis and resolutions | `YYMMDD_<kebab-case-title>.md` |
| `research/` | Research findings that ground implementation plans with verified facts | `YYMMDD_<kebab-case-title>.md` |
| `agent/` | Agentic AI workflow docs—custom agent architecture, orchestration patterns | Descriptive names |

**Usage:** Before implementing complex features, check `.docs/research/` for existing findings. Document new discoveries in `.docs/issues/` (for problems) or `.docs/research/` (for implementation guidance).

## Extending This Workspace

When adding demos for new .NET 10 topics beyond Identity:
1. Build incrementally from the latest demo when the topic connects naturally
2. Create a standalone demo branch if the topic is unrelated to prior demos
3. Update root `README.md` demo lineup table with focus, dependencies, and highlights
4. Follow existing naming conventions: `Demo<N>.<TopicName>`