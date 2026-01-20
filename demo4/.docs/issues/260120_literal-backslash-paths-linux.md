# Bug: Literal Backslash Folders in Git Changes (Linux)

## Summary
The Git workspace shows untracked directories with literal backslashes in their names (e.g., `bin\Debug/`), which bypassing `.gitignore` rules.

## Symptoms
`git status` reports untracked directories like `?? "bin\\Debug/"` even though `bin/` is in `.gitignore`.

## Root Cause
Tools or scripts are generating paths using Windows-style backslashes (`\`) on Linux. On Linux, `\` is a valid filename character rather than a path separator. Git treats `bin\Debug` as a single directory name which does not match the ignore pattern `[Bb]in/`.

## Proposed Fix
- Audit build scripts, Task runners (e.g., `copy-demo.ps1`), and C# code for hardcoded backslashes in path generation.
- Use `Path.Combine` or forward slashes (`/`) for all cross-platform path operations.
- Add a preventive rule or script to detect and warn about literal backslashes in the filesystem.

## Verification
- Run `ls -R | grep '\\'` to find malformed names.
- Ensure `git status` remains clean after builds.
