# Implementation Plan: Demo4 .docs Framework

## Goal & Success Criteria
Draft a pragmatic framework that lives inside `demo4/.docs/` and explains how to keep research, implementation plans, follow-up tasks, and onboarding notes aligned with the workshop conventions.
Success means the framework document summarizes required sections, references the root README and demo README template, and lists common maintenance questions so the next developer can immediately contribute to demo4.

## Context & Analysis
The workshop always documents strategy per demo in `demo<N>/.docs/`, with research, implementation planning, and pattern references. The master README already spells out the progression map & the requirement that per-demo `.docs` capture research plus implementation plans. The demo README template reinforces the expectation of high-level documentation, but there is no explicit guide for what belongs in the demo `.docs` folder itself. Demo4 is a completed pilot, so we can use it to validate what future demos need inside their `.docs`.

## Proposed Design/Changes
- Create a `demo4/.docs/framework.md` that: (a) maps existing folders to their intent (research, implementation plans, issues, onboarding notes), (b) prescribes the documentation workflow for new patterns or follow-ups, and (c) includes a troubleshooting log and maintenance playbook for recurring problems.
- Outline how to link back to the patterns catalog, seed research summaries, and keep implementation plans updated with references.
- Flag potential onboarding pain points (missing research links, unclear verification steps, environment issues) and prescribe how `.docs` entries should capture them.

## Verification & Testing
- Review the new framework doc for completeness and alignment with requirements.
- Ensure the doc references `.docs/reference/patterns/` and the demo README template.
- Share a brief summary of what to document next so future developers can verify the README quickly.

## Risks & Assumptions
- Risk: Demo4 may already have some `.docs` content; verify we are augmenting rather than duplicating.
- Assumption: New framework doc is acceptable without code changes.
