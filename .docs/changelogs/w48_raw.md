# Raw Changelog: November 24-30, Week 48, 2025

## Commits

Aris Nguyen | 2025-11-29 | docs(demo5/research): add implementation verification research

Add verification document comparing actual implementation against documentation, identifying that GraphService is configured but not used, and confirming README accuracy for demo5.

Aris Nguyen | 2025-11-29 | docs(demo5/research): add downstream API patterns research

Add research document covering .NET 10 downstream API patterns, IDownstreamApi usage, Entra ID configuration, and implementation examples for demo5.

Aris Nguyen | 2025-11-29 | docs(demo5/research): add BFF-to-API architecture research

Add comprehensive research document analyzing internal vs external API categorization, CORS necessity in server-to-server calls, and OAuth scope vs RBAC permission naming alignment for demo5.

Aris Nguyen | 2025-11-29 | docs(demo5/architecture): move deep dive to issues folder

Move ARCHITECTURE_DEEP_DIVE.md from demo5 root to .docs/issues/ for better organization of documentation within the demo5 project.

Aris Nguyen | 2025-11-29 | docs(demo5): update API naming references

Update README.md to reflect the new API naming convention and clarify the architecture patterns. Remove references to CORS in internal API and update configuration examples for demo5.

Aris Nguyen | 2025-11-29 | refactor(demo5/security): remove CORS from internal API

Remove CORS configuration from ProtectedApi/WeatherApi since CORS is not required for server-to-server HTTP calls. This enforces the BFF pattern by preventing direct browser access to internal APIs within demo5.

- Remove CORS policy and middleware from Program.cs
- Remove RequireCors from weather endpoint

Aris Nguyen | 2025-11-29 | refactor(demo5/api): rename downstream APIs for clarity

Rename DownstreamApi to MicrosoftGraph and ProtectedApi to WeatherApi to distinguish external SaaS APIs from internal domain APIs. Updates configuration, service registrations, and code references within demo5 project.

- Update Program.cs downstream API registrations
- Modify GraphService to use MicrosoftGraph name
- Update appsettings.json configuration sections
- Update appsettings.Development.json

Aris Nguyen | 2025-11-25 | chore(demo4): update app.db file

Aris Nguyen | 2025-11-25 | docs(demo4): cleanup docs structure

Aris Nguyen | 2025-11-25 | docs(demo5): add research findings

Aris Nguyen | 2025-11-25 | docs(agent): update workspace's copilot instructions

Aris Nguyen | 2025-11-25 | docs(agent): update custom agents for correctly utilize tool rubSubagent

Aris Nguyen | 2025-11-25 | feat(demo5): init demo5 project

Aris Nguyen | 2025-11-24 | docs(agent): update subagents

Aris Nguyen | 2025-11-24 | feat(demo4): finish demo4 project focusing on Microsoft Entra ID integration

Aris Nguyen | 2025-11-24 | docs(agent): add new custom agents including Meta-Agent, Microsoft-Docs-Agent, Research-Agent, Verifier-Agent, and Web-Search-Agent

- Created Meta-Agent for designing custom agents in VS Code.
- Introduced Microsoft-Docs-Agent for querying official Microsoft documentation.
- Enhanced Research-Agent to focus on .NET 10 features and implementation guidance.
- Added Verifier-Agent for testing and validating .NET 10 demo implementations.
- Implemented Web-Search-Agent for finding authoritative web resources related to .NET 10.

