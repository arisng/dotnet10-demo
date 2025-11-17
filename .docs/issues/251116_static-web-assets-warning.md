# Static Web Assets Not Enabled Warning

**Date:** 2025-11-16  
**Issue Type:** Technical Issue  
**Severity:** Low  
**Status:** Resolved

## 📋 Summary

When running a Blazor WebAssembly application in non-Development environments, the application emitted a warning about Static Web Assets not being enabled. This occurred because the static assets loader was not explicitly configured for Production and other environments.

## 🔍 Analysis / Context

- **Warning Message:**

  ```text
  warn: Microsoft.AspNetCore.StaticAssets.StaticAssetsInvoker[17]
  The application is not running against the published output and Static Web Assets are not enabled.
  ```

- The warning appeared when running the application without being in Development mode
- Blazor WebAssembly apps with InteractiveWebAssembly render mode require static web assets to be properly loaded
- By default, static web assets are only automatically enabled in Development environment
- The warning indicated potential issues with loading client-side resources

## ✅ Resolution / Decision

Added explicit static web assets configuration in `Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure Static Web Assets for non-Development environments
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
```

This ensures static assets are properly loaded across all environments (Development, Staging, Production).

## 📚 Lessons Learned

- Static Web Assets are not automatically enabled outside Development environment
- Explicit configuration is required for Blazor WebAssembly apps in Production
- The `StaticWebAssetsLoader.UseStaticWebAssets()` method should be called early in the application startup
- This is particularly important for dual-mode (Server + WebAssembly) Blazor applications

## 🛠️ Prevention / Implementation

1. Always add `StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration)` after creating the `WebApplicationBuilder` in Blazor WebAssembly or hybrid apps
2. Include the `Microsoft.AspNetCore.Components.Web` namespace
3. Test the application in non-Development environments to catch configuration issues early
4. Consider this a standard pattern for all Blazor WebAssembly projects

## 🔗 Related Files

- [`demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff/Program.cs`](../../demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff/Program.cs#L12)
- [`demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff/appsettings.Production.json`](../../demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff/appsettings.Production.json)

## 📖 Additional Resources

- [ASP.NET Core Static Web Assets documentation](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/ui-class#consume-content-from-a-referenced-rcl)
- [Blazor hosting models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)

## 🏷️ Tags

`dotnet` `blazor` `configuration` `static-assets` `webassembly` `troubleshooting` `low-priority` `production-config`
