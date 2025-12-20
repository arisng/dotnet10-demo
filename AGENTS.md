# Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

## Troubleshooting

**If bd command not found:**
- Check installation: `bd version`
- Verify PATH includes bd binary location

**If issues seem lost:**
- Use `bd list` to see all issues
- Filter by status: `bd list --status closed`
- Closed issues remain in database permanently

**If bd show can't find issue by name:**
- `bd show` requires issue IDs, not issue titles
- Workaround: `bd list | grep -i "search term"` to find ID first
- Then: `bd show issue-id` with the discovered ID
- For glossaries/reference databases where names matter more than IDs, consider using markdown format alongside the database

**If dependencies seem wrong:**
- Use `bd show issue-id` to see full dependency tree
- Use `bd dep tree issue-id` for visualization
- Dependencies are directional: `bd dep add from-id to-id` means from-id blocks to-id
- See [references/DEPENDENCIES.md](references/DEPENDENCIES.md#common-mistakes)

**If database seems out of sync:**
- bd auto-syncs JSONL after each operation (5s debounce)
- bd auto-imports JSONL when newer than DB (after git pull)
- Manual operations: `bd export`, `bd import`

## Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
```

### Issue Types

```bash
bd create "Title" -t task        # Standard work item (default)
bd create "Title" -t bug         # Defect or problem
bd create "Title" -t feature     # New functionality
bd create "Title" -t epic        # Large work with subtasks
bd create "Title" -t chore       # Maintenance or cleanup
```

### Priority Levels

```bash
bd create "Title" -p 0    # Highest priority (critical)
bd create "Title" -p 1    # High priority
bd create "Title" -p 2    # Normal priority (default)
bd create "Title" -p 3    # Low priority
```

### Bulk Operations

```bash
# Close multiple issues at once
bd close issue-1 issue-2 issue-3 --reason "Completed in sprint 5"

# Create multiple issues from markdown file
bd create --file issues.md
```

### Hierarchical Issues (Epics)

For large features, use hierarchical IDs to organize work:

```bash
# Create epic (generates parent hash ID)
./bd create "Auth System" -t epic -p 1
# Returns: bd-a3f8e9

# Create child tasks (automatically get .1, .2, .3 suffixes)
./bd create "Design login UI" -p 1       # bd-a3f8e9.1
./bd create "Backend validation" -p 1    # bd-a3f8e9.2
./bd create "Integration tests" -p 1     # bd-a3f8e9.3

# View hierarchy
./bd dep tree bd-a3f8e9
```

Output:
```bash
🌲 Dependency tree for bd-a3f8e9:

→ bd-a3f8e9: Auth System [epic] [P1] (open)
  → bd-a3f8e9.1: Design login UI [P1] (open)
  → bd-a3f8e9.2: Backend validation [P1] (open)
  → bd-a3f8e9.3: Integration tests [P1] (open)
```

#### Add Dependencies

```bash
# API depends on database
./bd dep add bd-2 bd-1

# Auth depends on API
./bd dep add bd-3 bd-2

# View the tree
./bd dep tree bd-3
```

Output:
```
🌲 Dependency tree for bd-3:

→ bd-3: Add authentication [P2] (open)
  → bd-2: Create API [P2] (open)
    → bd-1: Set up database [P1] (open)
```

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **Activate Claude Skill** `git-committer` to create atomic commits with proper messages
5. **Ask for human review** (MANDATORY) - Get a human to review changes before pushing
6. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
7. **Clean up** - Clear stashes, prune remote branches
8. **Verify** - All changes committed AND pushed
9. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
