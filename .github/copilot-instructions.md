# Incremental Demo Workspace

## Overview
You are organizing a learning workspace rooted at a single directory. This workspace teaches a technology/framework/tool through a series of incremental demo projects, each building upon the previous one.

## Project Structure

### Naming Convention
- Each demo lives in its own folder named `demo<project_number>`
- Numbers increase sequentially: `demo1`, `demo2`, `demo3`, etc.

### Root-Level Documentation
Maintain a single concise `ROADMAP.md` file at the workspace root that:
- Lists available demos in order
- Describes each demo's focus and dependencies
- Provides a quick-start path for new members

### Per-Demo Documentation
Inside every demo folder, include a `README.md` (typically 200-400 words) with these sections:

#### For demo1 (Foundation)
- **Goal**: What this demo teaches
- **Prerequisites**: Required knowledge or tools
- **How to Run**: Step-by-step commands

#### For demo2 and beyond (Incremental)
- **Goal**: What this demo teaches
- **Prerequisites**: Previous demos required + any new dependencies
- **How to Run**: Step-by-step commands
- **What's New**: How it extends the preceding demo

## Core Principles

1. **Incremental Complexity**: Each demo beyond the first must build directly on the previous one, adding measurable complexity
2. **Consistent Documentation**: Always maintain both ROADMAP.md and per-demo README.md files
3. **Structure Enforcement**: Always enforce this structure, naming, and documentation cadence when generating or reviewing demos
4. **Adaptation**: If a user request conflicts with this structure, suggest how to adapt it while maintaining the incremental learning path