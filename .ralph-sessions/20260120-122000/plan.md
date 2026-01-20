# Implementation Plan: Demo4 Documentation Reorg v2

## Goal & Success Criteria
Merge the setup material into the guidance folder, split the monolithic research findings into topic-based files, and move the architecture diagram out of the research directory. Success means the guidance folder hosts the setup guide plus implementation notes, the research folder contains focused files with a brief index, and no references remain to the deleted top-level `setup` path.

## Context & Analysis
Previous cleanup left some duplication between setup and guidance artifacts. The user now wants the guidance and setup material together, flatten research so new entries can be addressed separately, and remove the architecture diagram from the research folder for clarity. We already reorganized the docs, but this new request requires additional restructuring: moving files, merging content, splitting research content, and updating README references.

## Proposed Design/Changes
1. Move all setup-content into the guidance folder, merge `azure-entra-setup.md` into `setup-guide.md`, and remove the old `setup/` directory. Also ensure `guidance/` now contains the setup instructions along with implementation patterns/summary.
2. Split `RESEARCH_FINDINGS.md` into multiple files (e.g., `microsoft-identity-web.md`, `graph-integration.md`, `hybrid-auth.md`, `metrics-and-security.md`) while retaining an updated `research/README.md` that indexes the new entries and their focus.
3. Move `architecture-c4-model-diagrams.md` out of `research/` into a suitable location such as `.docs/diagrams/` (create folder if necessary) and update any references.
4. Update `framework.md`, `guidance/` files, and the research README to point at the new file layout and remove obsolete path references.

## Verification & Testing
- Confirm `guidance/` now contains setup-guide plus other docs; `setup/` directory no longer exists.
- Ensure the research folder lists only the new per-topic files and that `research/README.md` describes each file.
- Verify no references remain to the old paths (`.docs/setup`, `SETUP_GUIDE.md`, `RESEARCH_FINDINGS.md` as a single file, etc.) via `grep` or manual review.

## Risks & Assumptions
- Downstream README or automation may still expect previous filenames; double-check any cross references.
- Breaking research into multiple files should keep the same content but reorganized; validate no sections were dropped.
