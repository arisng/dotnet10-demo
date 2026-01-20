# Bug: Stale Build Artifacts with Windows-specific NuGet Fallback Paths

## Summary
Building the project on Linux fails because the `obj/` folder contains cached `project.assets.json` or `.props` files with hardcoded Windows absolute paths (e.g., `C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`).

## Symptoms
`dotnet ef database update` or `dotnet build` fails with:
`error MSB4018: NuGet.Packaging.Core.PackagingException: Unable to find fallback package folder 'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'.`

## Root Cause
Cross-platform development without proper build clean-up. Artifacts generated on Windows are not compatible with the Linux SDK's package resolution logic when they reference absolute host paths.

## Proposed Fix
- Always perform `dotnet clean` or manually remove `bin/` and `obj/` when switching between OS environments.
- Ensure `.gitignore` effectively excludes all build artifacts.
- Investigate if `Directory.Build.props` or `nuget.config` can be hardened to avoid environment-specific fallback folders.

## Verification
1. `rm -rf **/bin **/obj`
2. `dotnet restore`
3. `dotnet build`
