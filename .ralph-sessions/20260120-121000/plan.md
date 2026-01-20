# Implementation Plan: Demo4 .docs Cleanup

## Goal & Success Criteria
Restructure `demo4/.docs/` so the folder follows the new guidance → reference → support workflow, migrates the legacy top-level guides into their respective directories, and documents the expected maintenance rituals for future contributors. Success means a new `guidance/`, `reference/`, `setup/`, and `support/` layout exists, each with updated content, and `framework.md` explains how the new pieces work together.

## Context & Analysis
The original Demo4 `.docs` folder relied on a handful of top-level Markdown files (`IMPLEMENTATION_*`, `QUICK_REFERENCE`, `SETUP_GUIDE`) that no longer reflect the strategic framework we just added. The user explicitly asked to "cleanup" the folder and treat existing files as malleable. We already introduced `demo4/.docs/framework.md` earlier, so the next step is to reorganize the remaining docs, remove the redundant files, and refresh the navigation notes and issue guidelines.

## Proposed Design/Changes
1. Create `guidance/implementation-patterns.md` and `guidance/implementation-summary.md` to host the pattern rationale and post-implementation scorecard currently scattered across the old guides.
2. Move the Quick Reference into `reference/quick-reference.md`, the setup instructions into `setup/setup-guide.md`, and capture troubleshooting steps in `support/troubleshooting.md` while retiring the legacy files.
3. Update `framework.md` and add `issues/README.md` plus `research/README.md` so everything points to the new structure and the `support` playbook.

## Verification & Testing
- Confirm `demo4/.docs` lists only the new directories plus `framework.md`.
- Run searches for old filenames (`SETUP_GUIDE`, `QUICK_REFERENCE`, etc.) to ensure they no longer exist or are referenced.
- Read through `framework.md` to verify each section references the new paths and that the curated folders now hold the expected content.

## Risks & Assumptions
- Assumes no other scripts or automation expect the old top-level filenames; the restructure could briefly break references until downstream docs are updated.
- Assumes the new troubleshooting playbook captures the critical issues that surfaced from the previous iteration.
