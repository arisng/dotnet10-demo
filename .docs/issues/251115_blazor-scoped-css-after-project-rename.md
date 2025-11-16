# Blazor Scoped CSS Not Loading After Project Rename

**Date:** 2025-11-15  
**Issue Type:** Technical Issue  
**Severity:** Medium  
**Status:** Resolved

## 📋 Summary

After cloning `demo1` to `demo2` and renaming projects/namespaces, the Blazor Web App loaded with broken layout: navigation sidebar displayed as inline links, the hamburger toggle remained visible on desktop, and the flexbox `.page` container never rendered. The root cause was that Blazor's scoped CSS system generates component-specific attribute selectors (`b-xxxxxxxx`) tied to the project name, so the renamed `Demo2.DualModeHandoff.Client` project inherited stale scoped CSS artifacts from `Demo1.IdentityFoundation.Client` that referenced nonexistent scope attributes.

## 🔍 Analysis / Context

- **Blazor scoped CSS** appends unique attributes (e.g., `b-lvey69klyg`) to component markup and generates corresponding attribute-scoped selectors in `*.styles.css` bundles.
- When you copy a Blazor project folder and rename the `.csproj`, the `obj/` directory still contains cached scoped CSS bundles with the **old project name and old scope attributes**.
- The browser loaded `Demo2.DualModeHandoff.styles.css` which internally referenced selectors like `.page[b-lvey69klyg]`, but the actual rendered HTML had a **different** scope attribute (e.g., `b-iad7vz1xph`) because the build regenerated it after the rename.
- Static assets (Bootstrap, `app.css`) loaded correctly (200 OK), but component-specific layout rules from `MainLayout.razor.css` and `NavMenu.razor.css` never matched any DOM elements.
- DevTools network inspection confirmed all CSS files downloaded successfully; the issue was **selector mismatch**, not a 404.

## ✅ Resolution / Decision

Running `dotnet clean` followed by `dotnet build` removed stale intermediate files in `obj/Debug/net10.0/scopedcss/` and regenerated the scoped CSS bundles with the new project scope. The layout immediately rendered correctly after forcing a browser refresh.

**Key commands:**

```powershell
cd demo2/Demo2.DualModeHandoff
dotnet clean Demo2.DualModeHandoff.sln
dotnet build Demo2.DualModeHandoff.sln
```

## 📚 Lessons Learned

- **Scoped CSS is tightly coupled to the project identity**: Blazor generates scope attributes from the assembly name/hash, so renaming a project without cleaning build artifacts leaves orphaned CSS.
- **`obj/` and `bin/` must be regenerated after structural changes**: Copying a project folder brings stale intermediate files that can silently break scoped styles.
- **Visual bugs don't always mean missing assets**: If DevTools shows 200 OK for CSS files but layout is broken, check for **selector mismatches** caused by attribute scope drift.
- **Clean builds are non-negotiable after project clones/renames**: Always run `dotnet clean` immediately after renaming projects or copying solution folders to purge cached artifacts.

## 🛠️ Prevention / Implementation

1. **After cloning or renaming any Blazor project**, immediately run:

   ```powershell
   dotnet clean <solution>.sln
   dotnet build <solution>.sln
   ```

2. **Add a checklist to the ROADMAP or README** for incremental demo workflows:
   - Clone previous demo folder
   - Rename projects/namespaces
   - **Run `dotnet clean` before first build**
   - Apply new feature changes
3. **Consider `.gitignore` hygiene**: Ensure `bin/` and `obj/` are excluded so copied folders never commit stale artifacts.
4. **Use browser DevTools Elements panel** to inspect actual scope attributes on rendered markup vs. loaded CSS selectors when debugging layout issues.

## 🔗 Related Files

- `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff.Client/Layout/MainLayout.razor.css` – flexbox `.page` and `.sidebar` rules
- `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff.Client/Layout/NavMenu.razor.css` – responsive nav toggle logic
- `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff.Client/obj/Debug/net10.0/scopedcss/projectbundle/Demo2.DualModeHandoff.Client.bundle.scp.css` – generated scoped CSS bundle

## 📖 Additional Resources

- [Blazor CSS isolation (scoped CSS) documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [How Blazor generates scoped CSS attributes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0#css-isolation-bundling)

## 🏷️ Tags

`blazor` `dotnet` `scoped-css` `css-isolation` `project-rename` `build-artifacts` `troubleshooting` `medium-priority` `local-development` `incremental-demos`
