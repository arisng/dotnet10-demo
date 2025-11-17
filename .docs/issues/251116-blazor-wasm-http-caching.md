# Blazor WASM HTTP Caching Issue: max-age=0 Even in Production Mode

**Date:** 2025-11-16  
**Issue Type:** Investigation  
**Severity:** Low
**Status:** ✅ Resolved  

## 📋 Summary

**Issue:** WASM framework files (`_framework/*.wasm`, `_framework/*.dll`) consistently return HTTP `200 OK` status with `Cache-Control: max-age=0` on every request, even when running with `ASPNETCORE_ENVIRONMENT=Production`. Browser never caches the files and re-downloads them on every page refresh.

**Root Cause:** `dotnet run` uses incremental compilation which regenerates WASM files with new hashes on every build. This is **intentional by design** for development workflow (Hot Reload support). Only `dotnet publish` generates optimized, fingerprinted assets with stable cache headers.

**Resolution:** ✅ **No configuration issue exists.** Published builds work correctly with proper caching (`max-age=31536000, immutable` and `304 Not Modified` responses). Accept `max-age=0` during development as expected behavior.

---

## 🔍 Detailed Analysis

### Expected vs Actual Behavior

**Expected Behavior:**
- Production: `Cache-Control: public, max-age=31536000, immutable`
- Browser caches files and serves from disk cache (or returns 304 Not Modified)

**Actual Behavior:**
- Both Development and Production (via `dotnet run`): `Cache-Control: max-age=0`
- Status: Always `200 OK` (never `304 Not Modified`)
- Files fully re-download on every request

## 🔍 Root Cause Analysis

### Testing Environment Details

**Environment Configuration:**
- Framework: .NET 10 Preview
- App Type: Blazor Web App with InteractiveWebAssembly
- Running via: `dotnet run` with Production profile
- Static Assets: Using `MapStaticAssets()` + `AddResponseCompression()`
- Configuration: `EnableStaticAssetsDevelopmentCaching: true` in appsettings.Development.json

**Launch Profile Used:**
```json
"https-prod": {
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Production"
  }
}
```

### Key Findings

1. **`dotnet run` vs `dotnet publish` Behavior**
   - `dotnet run` builds to `/bin/{Configuration}/net10.0/` without full optimization
   - Static web assets manifest may not include fingerprinted WASM files
   - Production cache headers only applied to published assets

2. **MapStaticAssets Requirements**
   - Requires static assets to be discovered at build time
   - Fingerprinting happens during `publish`, not `build`
   - Running with Production env doesn't trigger publish optimizations

3. **WASM File Serving Mechanism**
   - WASM files served via `AddInteractiveWebAssemblyComponents()`
   - Not directly managed by `MapStaticAssets()` during development
   - Separate endpoint for `_framework` files

### Microsoft Documentation References

From "Static files in ASP.NET Core":
> "Map Static Assets provides the following benefits:
> - **Fingerprinting** for all assets at build time with a Base64-encoded string of the SHA-256 hash
> - Fingerprinted assets are cached using the `immutable` directive"

**Key Quote:**
> "Gzip compression is used during development. Gzip and **Brotli compression are both used during publish**."

This confirms that optimizations are **publish-time**, not build-time.

## ✅ Testing Plan

### Test 1: Verify Current Behavior (dotnet run Production)
```powershell
# Run with Production environment
dotnet run --project Demo2.DualModeHandoff.csproj --launch-profile https-prod

# Check Network tab:
# - _framework/dotnet.native.wasm
# - Cache-Control header value
# - Status on refresh (200 vs 304)
```

**Expected Result:** max-age=0 (current observation confirmed)

### Test 2: Test Published Build
```powershell
# Publish with Release configuration
dotnet publish -c Release -o ./publish

# Run published app
cd publish
dotnet Demo2.DualModeHandoff.dll

# Check Network tab again
```

**Expected Result:** `Cache-Control: public, max-age=31536000, immutable`

### Test 3: Verify Compression
```powershell
# Check if .br and .gz files exist in publish output
Get-ChildItem ./publish/wwwroot/_framework -Recurse -Include *.br,*.gz

# Check Content-Encoding header in browser
```

**Expected Result:** Files have `.br` or `.gz` versions, `Content-Encoding: br` header

## 🎯 Solution Options

### Option 1: Use Published Build for Production Testing (Recommended)

**Why:**
- `dotnet run` with Production env still uses development build output
- Only `dotnet publish` generates optimized static assets manifest
- Fingerprinting and immutable cache headers require publish process

**Implementation:**
```powershell
# Create publish script
dotnet publish -c Release -o ./bin/publish
cd ./bin/publish
dotnet Demo2.DualModeHandoff.dll
```

**Pros:**
- Matches real production behavior
- Proper cache headers automatically applied
- WASM files cached correctly

**Cons:**
- Slower iteration (must publish for each test)
- Debugging more difficult (no hot reload)

### Option 2: Accept Development Behavior for Demo

**Why:**
- Development intentionally uses `max-age=0` for fresh builds
- Hot Reload and incremental compilation require this
- Demo focuses on InteractiveAuto lifecycle, not caching

**Implementation:**
- No changes needed
- Document that WASM caching only observable in published builds
- Focus demo on SignalR handoff behavior (Local Storage aspect)

**Pros:**
- Simpler development workflow
- Aligns with demo purpose (render mode transitions)

**Cons:**
- Can't demonstrate production caching behavior
- Users might question why files re-download

### Option 3: Add Custom Middleware for _framework Files

**Why:**
- Override default cache headers for _framework during development
- Provide production-like caching in `dotnet run`

**Implementation:**
```csharp
// In Program.cs - AFTER MapStaticAssets
app.Use(async (context, next) =>
{
    await next();
    
    if (context.Request.Path.StartsWithSegments("/_framework") && 
        context.Response.StatusCode == 200)
    {
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});
```

**Pros:**
- Demonstrates caching in development
- No need to publish for testing

**Cons:**
- Hacky workaround, not production code
- May conflict with Hot Reload
- Doesn't represent real production behavior

## 📊 Decision Matrix

| Criteria | Option 1: Publish | Option 2: Accept Dev | Option 3: Middleware |
|----------|-------------------|----------------------|----------------------|
| Accuracy | ✅ Real production | ❌ Dev only | ⚠️ Simulated |
| Ease of use | ❌ Requires publish | ✅ No changes | ✅ Code change |
| Demo relevance | ⚠️ Caching secondary | ✅ Focuses on handoff | ⚠️ May confuse |
| Maintainability | ✅ Standard practice | ✅ No extra code | ❌ Custom code |

## ✅ Final Resolution

**✅ CONFIRMED: Option 2 - Accept Development Behavior**

**Test Results (2025-11-17):**
- `dotnet run` (any environment): `max-age=0` - files re-download ✅ Expected
- `dotnet publish` (Release build): `max-age=31536000, immutable` - 304 responses ✅ Working

**Root Cause Confirmed:**
1. `dotnet run` uses incremental compilation - WASM files regenerate with new hashes on every build
2. `dotnet publish` creates optimized, fingerprinted assets with stable hashes
3. This is **intentional by design** for development workflow (Hot Reload support)
4. Production deployments using published builds work correctly

**Resolution:**
- **No code changes required** - current configuration is correct
- Accept `max-age=0` during development as expected behavior
- Published builds automatically provide proper caching
- Demo focuses on InteractiveAuto render mode transitions, not HTTP caching optimization

## 📝 Action Items

- [x] Create publish build to test proper caching
- [x] Verify compressed files (.br/.gz) exist in publish output  
- [x] ✅ **Run published build and verify cache headers** - CONFIRMED WORKING
  - Test Date: 2025-11-17
  - Result: `Cache-Control: max-age=31536000, immutable` ✅
  - HTTP Status on refresh: `304 Not Modified` ✅
  - Compression: Brotli/Gzip working ✅
- [x] Document difference between `dotnet run` vs `dotnet publish` behavior
- [ ] Update demo README with caching expectations (optional)
- [ ] Add quick note in demo docs about published vs development caching (optional)

## 🧪 Test Results

### Published Build Test (Pending)

**Command:**
```batch
cd c:\Workplace\Demo\dotnet10-demo\demo2\Demo2.DualModeHandoff\publish
set ASPNETCORE_ENVIRONMENT=Production
dotnet Demo2.DualModeHandoff.dll --urls "https://*:7210"
```

**What to Check:**
1. Navigate to https://localhost:7210/auth-probe
2. Open DevTools → Network tab
3. Find `dotnet.native.wasm` or any `.dll` file under `_framework/`
4. Check Response Headers:
   - `Cache-Control`: Should be `public, max-age=31536000, immutable`
   - `Content-Encoding`: Should be `br` (Brotli) or `gzip`
   - `ETag`: Should be present
5. Refresh page (F5):
   - Status: Should be `(from disk cache)` or `304 Not Modified`
   - Size: Should show `(disk cache)` or very small

**Results:** ✅ **CONFIRMED - Published build works correctly!**

**Test Date:** 2025-11-17

**First Load Response Headers:**
- `Cache-Control: max-age=31536000, immutable` ✅
- `Content-Encoding: br` (Brotli compression) ✅
- `ETag: "{hash-value}"` ✅
- Status: `200 OK` ✅

**Subsequent Requests (Page Refresh):**
- Status: `304 Not Modified` ✅
- Files served from browser cache ✅
- No re-download of WASM files ✅

**Conclusion:** 
WASM HTTP caching works perfectly in published builds. The `max-age=0` behavior during `dotnet run` is **intentional and correct** for development workflow. No configuration issues exist.

## 🔗 References

- [Static files in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0)
- [Host and deploy ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0#compression)
- [ASP.NET Core Blazor static files](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0)
