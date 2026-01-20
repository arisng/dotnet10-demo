# Demo4 Documentation Framework

This document describes how `demo4/.docs/` supports the workshop's rule that every demo carries rich research, implementation plans, and follow-up guidance. Treat this folder as the single-source maintenance cockpit for Demo4, not just a collection of reference files. Use it to orient new contributors, capture why decisions were made, and log the problems we already solved so they don't resurface.

## Why This Folder Matters
- The root README mandates that every demo feeds the patterns catalog (`.docs/reference/patterns/`) via research and implementation plans inside `demo<N>/.docs/`.
- The demo README template assumes `research` and `implementation` artifacts already exist; this framework explains which sections live where so the README stays concise.
- For Demo4 (notionally a completed pilot) we lock down a lightweight workflow so the `.docs` folder can be a living operations journal as the repo evolves.

## Folder Map & Intent
- Use this map when adding or reviewing files in `demo4/.docs/`:
   - `research/`: store topic-based research files like `microsoft-identity-web.md`, `graph-integration.md`, `hybrid-auth-identity.md`, `security-and-metrics.md`, and `AUTO_PROVISIONING_RESEARCH.md`. Each research page should cite the pattern catalog entry that motivated the work and update `research/README.md` with a short summary.
   - `diagrams/`: keep architecture/visualization assets (like `architecture-c4-model-diagrams.md`) here so the analyses stay close to the research but live in their own folder for easy referencing in READMEs and docs.
   - `guidance/`: keep step-by-step instructions, environment variables, and non-technical prerequisites. Files here (like `guidance/setup-guide.md`) should include verification checklists and links back to `reference/quick-reference.md` so new devs can bootstrap quickly.
   - `guidance/`: document the pattern rationale (`implementation-patterns.md`) plus the outcome, testing notes, and outstanding work (`implementation-summary.md`). Always call out the related code files and migration steps.
   - `reference/`: capture reusable snippets (commands, SQL, URLs) that testers/reference-trackers need at a glance (see `reference/quick-reference.md`).
   - `support/`: capture troubleshooting notes, logging strategies, and runbooks that complement the reference snippets, especially `support/troubleshooting.md`.
   - `issues/`: treat Markdown files here as short-lived maintenance tickets. Each entry follows `YYMMDD_short-title.md`, describes an observed problem, and records the fix date with tags like `#investigate` or `#resolved`. Add `issues/README.md` if the workflow needs documentation.

## Documentation Workflow
1. **New pattern adoption or change:** Before touching code, author a research note inside `research/` that answers:
   - What does the pattern solve? (Link to `.docs/reference/patterns/catalog/<pattern>.md`)
   - What evidence/official docs justify this approach? (Cite docs or vendor articles.)
   - What are the risks or known limitations? (Populate `support/` entries as needed.)
2. **Implementation plan:** Draft `guidance/implementation-patterns.md` (if new) or update it with the exact code regions and configuration files involved. Each plan mentions the verification steps and database changes.
3. **Follow-up & verification:** After merging changes, log the completion in `guidance/implementation-summary.md` and update `issues/` or `support/troubleshooting.md` with any new troubleshooting insights.
4. **README ties:** Whenever `demo4/README.md` talks about a pattern or technology, link directly to the supporting `.docs` files so that readers can drill into the reasoning.

## Onboarding & Troubleshooting Rituals
Document the lifecycle of major issues so future contributors can resolve them quickly:

| Symptom                                | Usual Spot                             | First Steps                                                                                                                                   |
| -------------------------------------- | -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| "Microsoft Entra ID" button disappears | `guidance/setup-guide.md`              | Confirm `AzureAd` settings, refresh user secrets, restart the app, and rerun `/auth-state-probe`.                                             |
| AADSTS50011 redirect mismatch          | `support/troubleshooting.md`           | Verify redirect URIs, check that `CallbackPath` is `/signin-oidc`, and rerun `dotnet user-secrets list` after editing `appsettings.json`.     |
| Graph API 401 or missing permissions   | `support/troubleshooting.md`           | Confirm `DownstreamApi:Scopes` include `User.Read`, inspect logs for MSAL errors, and follow the checklist in `reference/quick-reference.md`. |
| Entra user has no permissions          | `issues/YYMMDD_missing-permissions.md` | Run the SQL snippets from `reference/quick-reference.md` to assign Admin/roles, then refresh the auth probe to confirm `permission` claims.   |
| Token cache leaks or lost logins       | `guidance/implementation-patterns.md`  | Reference the distributed cache + encryption guidance; production must call `.AddDistributedTokenCaches()` and configure data protection.     |

Add or update these entries as new problems arise. Each troubleshooting file should include:
- Context (where it was noticed)
- Steps taken (commands, configuration, SQL queries)
- Success criteria (e.g., login succeeds, Graph data syncs)
- Follow-up actions (monitor metrics, revisit doc when upgrading). 

## Maintenance Playbook
When a future developer works on Demo4, they should:
1. **Start in this framework** before modifying README or code: confirm the research note, plan, and verification steps exist.
2. **Log any new pattern or technology:** mention it here, cite the pattern catalog entry, and capture the business/technical lift bullet points used in root README.
3. **Tag related docs:** each `.docs` entry referencing code should include a “Related files” section listing the key project paths (e.g., `Demo4.EntraIntegration/Program.cs`).
4. **Link to verification:** include checklist items inside `guidance/implementation-patterns.md` and `reference/quick-reference.md` so testers know how to prove the change works.
5. **Update onboarding notes:** when a dev raises a pain point (build failure, tooling mismatch), append it to `support/troubleshooting.md` (or create a new `support/<topic>.md`) with the date and a short resolution summary.

## Possible Gotchas to Watch
Even though Demo4 is production-grade, expect these recurring issues:
- **Token cache security:** remind the team that `AddInMemoryTokenCaches()` is development-only and production requires a distributed provider plus encryption (see `guidance/implementation-patterns.md`).
- **Redirect URI drift:** newcomers often misconfigure `CallbackPath`; keep the `guidance/setup-guide.md` checklist updated with exact URIs and port combos.
- **Graph permissions:** if Microsoft Graph calls fail, reference the `DownstreamApi` scopes in `guidance/implementation-summary.md` and rerun `dotnet user-secrets` to refresh secrets.
- **Claims desynchronization:** the claims transformation can re-run per request; log this in `support/troubleshooting.md` so future debugging starts with verifying the `permission_transformed` claim.
- **Role assignment for Entra users:** mention in `issues/` and `reference/quick-reference.md` that Entra users have no permissions until assigned (or until demo6 automates this).

## Trailhead for the Next Demo
Document how Demo4 feeds Demo5 by:
- Linking each pattern entry here to the Demo5 plan (if one exists) in `.docs/issues/` or `.docs/research/` so the progression map remains visible.
- Noting any unresolved tasks (token cache, distributed cache, scopes) in `issues/` so the next demo can address them.
- Keeping this framework alive: copy it to future demos and update the pattern list to match the new focus.

**Tip:** Share this framework with anyone starting on Demo4 and ask them to make at least one contribution-to-docs before touching code; this keeps the `.docs` folder accurate and prevents knowledge loss.