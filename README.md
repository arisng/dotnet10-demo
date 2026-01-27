# .NET 10 Modern Architecture Workshop

This repo is a **progressive workshop** for continuously evolving a modern .NET 10 web application across **both backend and frontend**. It is designed to stay current (today: **January 17, 2026**) and to grow beyond what is already documented. Each demo builds on the previous one so you can learn by doing without losing context.

**Motivation:** turn the workshop into a reusable blueprint that can bootstrap a SaaS business quickly. The goal is to plug in a **dynamic business domain** and produce deployable POCs/MVPs within a day while still following modern architecture standards.

## Patterns Catalog (Single Source of Truth)

We actively maintain `.docs/reference/patterns/` as the curated catalog of modern industry standards and web architecture patterns. It is continuously updated and **drives what we build next**.

- Start here: `.docs/reference/patterns/index.md`
- Pattern entries live in: `.docs/reference/patterns/catalog/`
- Guidance for choosing and applying patterns: `.docs/reference/patterns/guidance/`

## Scope & Glossary (How We Use Terms)

- **Scope:** This workshop is not limited to what is already documented. The catalog and demos evolve as new standards and platform features emerge.
- **Pattern:** A reusable architectural or technical solution (e.g., BFF, OIDC, RBAC) documented in the patterns catalog.
- **Business feature:** Stakeholder-facing capability or outcome the product delivers (e.g., “approve invoices,” “export reports”).
- **Infrastructure capability:** Foundational technical work that enables business features (e.g., identity integration, observability, multi-tenancy).

## Research & Implementation Planning (Required)

- Every selected pattern **must** be researched and grounded with references to **official docs and/or reputable technical blogs**.
- The **implementation plan and references live in the demo’s `.docs/` folder**, not in the root README.
- Root README stays high-level; demo-level `.docs/` files carry the detail.

## Progression Map (How We Track Evolution)

**Progression template (per demo)**

```
Demo: <demoN>
Inherits: <demoN-1>
Adds patterns:
- <Pattern A>
- <Pattern B>
- <Pattern C>
Business lift:
- <New segment, tier, or workflow enabled>
- <Operational or revenue impact>
Technical lift:
- <New runtime boundary, security model, or scalability feature>
- <Key platform capability added>
```

**Example – demo4**

```
Demo: demo4
Inherits: demo3
Adds patterns:
- Entra ID integration
- App Roles → permissions mapping
- OBO (Microsoft Graph)
Business lift:
- Enterprise SSO onboarding
- Centralized role management
Technical lift:
- External identity boundary
- Delegated token flow
```

This progression view is the **strategic map** for building a reusable SaaS blueprint with concrete implementation, strong testing, and production‑ready deployment. Product Owners can provide new business demands, and Tech Leads can quickly map them to an existing demo or select new patterns from the catalog to create the next demo. The Tech Lead continuously maintains and evolves the patterns catalog so the blueprint stays aligned with modern industry standards.

**Decision flow (PO → TL)**

```
PO input → match business lift → find demo
        → if no match → select patterns from catalog → create new demo
        → document plan in demo<N>/.docs/research/ → implement + test
```

**Documentation structure**

- **Demo Lineup (this README):** high-level view of the journey and status.
- **Demo README (`demo<N>/README.md`):** goal, prerequisites, how to run, and what’s new.
- **Demo research & plans (`demo<N>/.docs/research/`):** per‑pattern research notes and implementation plan, with references.
- **Patterns Catalog (`.docs/reference/patterns/`):** the authoritative catalog and guidance used by all demos.

This provides traceability from **catalog → research → implementation**, while keeping the root README focused and scannable.

## Quick Start

1. Install the latest .NET 10.0 SDK (10.0.0) plus the EF Core tools (10.0.0). Run `dotnet new update` so the local template includes the newest Identity scaffolding bits.
2. Clone this repo, then start with `demo1` inside VS Code or JetBrains Rider.
3. Use the commands below to apply the initial migration and run the first demo:

```powershell
cd demo1/Demo1.IdentityFoundation/Demo1.IdentityFoundation
dotnet ef database update
dotnet watch
```

> When you are ready for `demo2`, run the same commands inside `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff`, sign in, and browse to `/auth-state-probe` to watch the InteractiveAuto handoff in action.

> Port convention: all demos run on `https://localhost:7210` (and `http://localhost:5210` for non-TLS callbacks). Update each new demo’s `launchSettings.json` if a template scaffolds different ports.

> Each subsequent demo reuses the previous codebase. Copy the prior folder forward (e.g., `demo1` ➜ `demo2`) before applying the new steps so you always have a working checkpoint.

## Demo Lineup

| Demo    | Status    | Focus                                           | Depends On | Highlights                                                                                                            |
| ------- | --------- | ----------------------------------------------- | ---------- | --------------------------------------------------------------------------------------------------------------------- |
| demo1   | Completed | Identity scaffolding baseline                   | —          | CLI scaffolding, cookie auth foundation                                                                               |
| demo2   | Completed | Dual-mode diagnostics + Passkeys                | demo1      | Auth state probe, full passkey implementation, WASM caching                                                           |
| demo3   | Completed | BFF APIs + Permission-Based RBAC                | demo2      | Fine-grained permissions, role→permission mapping, claims transformation                                              |
| demo4   | Completed | Microsoft Entra ID + Claims Mapping             | demo3      | External provider, Graph API (OBO), Entra App Roles mapping, identity-source agnostic auth, API-to-Navigation Handoff |
| demo4.1 | Completed | Entra + BFF (YARP) + Aspire                     | demo4      | Distributed orchestration, YARP proxy, InteractiveAuto refinements                                                    |
| demo4.2 | Completed | DProcess IdP + BFF + API (OpenIddict + Entra)   | demo4.1    | Dedicated IdP, unified RBAC claims, Aspire orchestration                                                              |
| demo5   | Completed | Custom Downstream APIs (Microservices)          | demo4      | Separate API project, Bearer tokens, OBO flow, Architecture comparison                                                |
| demo5.1 | Completed | Distributed Modular Monolith with Aspire & YARP | demo5      | .NET Aspire orchestration, YARP Proxy, "Two Locks" security model, .NET 10 Built-in OpenAPI + Scalar UI               |
| demo6   | Planned   | The Multi-Tenant SaaS Monolith (SaaS)           | demo5.1    | Finbuckle, Multi-Identity per Tenant, Shared/Dedicated DB Choice                                                      |
| demo7   | Planned   | Feature Flag Management & Hardening             | demo6      | Subscription-based Flags, Azure AppConfig, Operational Hardening                                                      |

## Next Steps

1. Implement `demo6` – The Multi-Tenant SaaS Monolith (SaaS): Build the Multi-Identity pipeline and data isolation layers using Finbuckle.
2. Implement `demo7` – Feature Flag Management & Hardening: Implement subscription-based toggles and production observability.
3. Validate all demos (demo1-demo5.1) for completeness and alignment with changelog achievements.
4. Keep this roadmap updated as new .NET 10 identity features ship.

## Demo Creation Rules

- Every new demo must introduce at least one pattern from `.docs/reference/patterns/catalog/`.
- Pattern selection must build on prior demos and reflect a strategic progression (foundation → integration → distribution → hardening).
- The demo README must list the chosen patterns and link back to their catalog entries.
- Each chosen pattern must have demo-level research + implementation planning documented in `demo<N>/.docs/research/` with references.
- Demo READMEs must follow the standard template at [.docs/reference/templates/demo-readme-template.md](.docs/reference/templates/demo-readme-template.md), enforced by instructions in [.github/instructions/demo-readme.instructions.md](.github/instructions/demo-readme.instructions.md).
