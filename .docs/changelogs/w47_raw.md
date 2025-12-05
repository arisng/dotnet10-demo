# Raw Changelog: November 17-23, Week 47, 2025

## Commits

Aris Nguyen | 2025-11-23 | feat(demo4): init project

Aris Nguyen | 2025-11-23 | docs(agent): add conductor and research agent definitions with project guidelines

Aris Nguyen | 2025-11-23 | refactor(demo3): restructure folder structure

Aris Nguyen | 2025-11-23 | fix(demo3): remove authorization attribute in AuthStateProbe page to demonstrate the auth state changes refresh

Aris Nguyen | 2025-11-23 | chore(demo3): enable browser launch

Updates launchSettings.json to launch browser on start.

Aris Nguyen | 2025-11-23 | fix(demo3): refactor client components to use CascadingParameter

Refactors AuthStateProbe and AuthStateSurface to use [CascadingParameter] instead of injecting AuthenticationStateProvider.
- Prevents destructive reads of PersistentComponentState (which is single-use).
- Ensures reactive UI updates via OnParametersSetAsync.
- Cleans up debug logging in PersistingServerAuthenticationStateProvider.

Aris Nguyen | 2025-11-23 | docs(demo3): add architecture understanding and auth state persistence lessons

Adds comprehensive documentation covering:
- Demo 3 architecture (BFF, Service Abstraction, Cookie vs Token).
- Troubleshooting guide for Blazor Auth State Persistence in InteractiveAuto mode.

Aris Nguyen | 2025-11-21 | chore(demo3): update app.db

Aris Nguyen | 2025-11-21 | feat(demo3): apply new dotnet 10 blazor feature to resolve UI flicker by applying PersistentState attribute to Users property

Aris Nguyen | 2025-11-21 | feat(demo3): migrate .sln to new format .slnx

Aris Nguyen | 2025-11-21 | feat(demo3): finish demo3 project

Aris Nguyen | 2025-11-21 | docs(demo5): update roadmap to add new demo project to implement custom downstream APIs and enhance Entra ID claims mapping

Aris Nguyen | 2025-11-20 | feat(demo3): add README for demo3 with BFF APIs and permission-based RBAC overview

Aris Nguyen | 2025-11-20 | feat(demo2): enhance demo2 README with detailed goals, diagnostics, and passkey implementation notes

Aris Nguyen | 2025-11-17 | docs(demo2): update README with detailed instructions for development and published modes

Aris Nguyen | 2025-11-17 | docs: add lessons learned for Blazor WASM HTTP caching issue and resolution and enabling static web assets loader for non-Development environments

Aris Nguyen | 2025-11-17 | feat(demo2): enhance application configuration with response compression and static web assets support for non-development environment

