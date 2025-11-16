# Blazor InteractiveAuto SignalR Phase Not Visible Due to WASM Caching

**Date:** 2025-11-16
**Issue Type:** Learning Insight
**Severity:** Medium
**Status:** Resolved

## 📋 Summary

When demonstrating Blazor's `InteractiveAuto` render mode lifecycle, the expected SignalR interactive phase was not visible in the timeline, even with browser DevTools cache disabled and network throttling enabled. The root cause: **InteractiveAuto only uses SignalR on the first visit when WASM isn't cached**. On subsequent visits, WASM is retrieved from cache and SignalR is skipped entirely. 

**Critical Discovery**: Blazor stores a **"Blazor resource hash"** in the browser's **Local Storage** (not just HTTP cache or service worker). This hash determines whether WASM is considered "cached" and available. DevTools "Disable cache" checkbox **does not clear Local Storage**, which is why the SignalR phase remained hidden even with cache disabled.

## 🔍 Analysis / Context

**Expected InteractiveAuto Lifecycle (First Visit):**
1. Server prerender (static HTML)
2. Server interactive via SignalR (fallback while WASM downloads)
3. WASM initialized (client takeover)
4. WASM first render complete

**Actual Observed Lifecycle (Subsequent Visits):**
1. Server prerender
2. ~~Server interactive (SignalR)~~ ← Skipped when WASM cached
3. WASM initialized (immediate)
4. WASM first render complete

**Key Findings:**
- InteractiveAuto uses progressive enhancement **only on first visit**
- **Blazor stores resource hash in browser Local Storage** (key: `blazor-resource-hash`) to track WASM cache state
- WASM runtime files are cached by browser HTTP cache and service worker
- **DevTools "Disable cache" checkbox does NOT clear Local Storage**
- Deleting the `blazor-resource-hash` from Local Storage forces InteractiveAuto to treat it as a first visit
- Network throttling has no effect if Local Storage indicates WASM is already cached
- `OnAfterRenderAsync(firstRender: true)` on server side only fires during SignalR phase (first visit)
- Component lifecycle logging showed `OperatingSystem.IsBrowser()` returning false never triggered the expected server-side event on subsequent visits

**Microsoft Documentation on Caching:**
> "The component is initially rendered with interactive server-side rendering (interactive SSR) using the Blazor Server hosting model. The .NET runtime and app bundle are downloaded to the client in the background and **cached so that they can be used on future visits**."

Blazor's boot configuration contains "a fingerprinted manifest of the files that make up the app that must be downloaded along with a hash of each file's content. The app's files are preloaded and cached by the browser."

## ✅ Resolution / Decision

**Three methods to observe the SignalR phase:**

**Method 1: Delete Local Storage (Most Direct)**
1. Open browser DevTools → Application tab → Local Storage
2. Find key: `blazor-resource-hash`
3. Right-click → Delete
4. Refresh the page
5. SignalR phase will now appear!

**Method 2: Incognito/Private Mode**
1. Use **Incognito/Private browsing mode** (fresh browser session with no Local Storage)
2. Navigate to the page for the **first time**
3. Optional: Apply network throttling (Slow 3G) to extend the SignalR phase duration

**Method 3: Hard Refresh (Less Reliable)**
- Ctrl+Shift+R or Ctrl+F5 may work but doesn't always clear Local Storage

**Updated demo documentation to clarify:**
- SignalR phase is **first-visit only** behavior (determined by Local Storage hash presence)
- WASM caching via Local Storage causes InteractiveAuto to skip directly to WASM on subsequent visits
- Clear instructions for **deleting Local Storage** to force first-visit behavior
- Accurate 4-phase vs 3-phase lifecycle description
- Explicit warning that DevTools "Disable cache" **does not affect Local Storage**

**Code changes:**
- Updated `AuthStateProbe.razor` with accurate alert explaining Local Storage caching behavior
- Fixed `OnAfterRenderAsync` to properly detect and log SignalR phase when it occurs
- Updated README.md with "first visit only" clarification and Local Storage instructions
- Added App.razor comments explaining InteractiveAuto progression

## 📚 Lessons Learned

- **InteractiveAuto is not deterministic**: Behavior changes based on Local Storage cache state
- **WASM caching uses Local Storage**: Blazor stores `blazor-resource-hash` in Local Storage to track cached state
- **Three-layer caching**: Blazor uses HTTP cache + service worker + **Local Storage** for WASM caching
- **DevTools "Disable cache" is insufficient**: Only affects HTTP cache, not Local Storage or service workers
- **Local Storage persists across sessions**: Even hard refresh (Ctrl+F5) may not clear Local Storage
- **Testing methodology matters**: Must either use Incognito mode OR manually delete Local Storage keys
- **Component lifecycle varies by render mode**: `OnAfterRenderAsync` server-side behavior differs between InteractiveAuto (conditional) vs InteractiveServer (always)
- **Documentation must be precise**: "May skip" language is insufficient; must state "first visit only" and explain Local Storage role
- **Browser Application tab is essential**: DevTools → Application → Local Storage reveals the true cache state

## 🛠️ Prevention / Implementation

**For future Blazor render mode demos:**

1. **Always inspect Local Storage first** before testing render mode behavior
2. **Document Local Storage caching** explicitly in user-facing instructions
3. **Provide clear cache-clearing steps**:
   - DevTools → Application → Local Storage → Delete `blazor-resource-hash`
   - Alternative: Use Incognito mode
4. **Use RendererInfo API** to detect actual render location at runtime:

   ```csharp
   @inject Microsoft.AspNetCore.Components.RendererInfo RendererInfo
   
   @if (RendererInfo.IsInteractive)
   {
       // Component is interactive (SignalR or WASM)
   }
   ```

5. **Add clear visual indicators** distinguishing first-visit from cached-visit behavior
6. **Include Local Storage state in diagnostic UI**:
   ```javascript
   const resourceHash = localStorage.getItem('blazor-resource-hash');
   console.log('WASM cached:', !!resourceHash);
   ```

**Testing checklist for InteractiveAuto:**
- [ ] Test in Incognito mode (first visit)
- [ ] Test with normal browsing (cached visit)
- [ ] **Check Local Storage for `blazor-resource-hash` key before testing**
- [ ] Delete Local Storage → Refresh to simulate first visit
- [ ] Test with Slow 3G throttling after clearing Local Storage (to extend SignalR phase)
- [ ] Verify timeline shows correct phases for each scenario
- [ ] Check browser Application tab → Local Storage for `blazor-resource-hash`
- [ ] Check browser Application tab → Service Workers for cache status
- [ ] Verify DevTools Console for WASM download logs

## 🔗 Related Files

- `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff.Client/Pages/AuthStateProbe.razor` - Main diagnostic component
- `demo2/Demo2.DualModeHandoff/Demo2.DualModeHandoff/Components/App.razor` - InteractiveAuto configuration
- `demo2/README.md` - Updated documentation with caching behavior
- Lines 150-172 in `AuthStateProbe.razor` - `OnAfterRenderAsync` SignalR detection logic

## 📖 Additional Resources

- [ASP.NET Core Blazor render modes - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0#automatic-auto-rendering)
- [ASP.NET Core Razor component lifecycle - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0#stateful-reconnection-after-prerendering)
- [Blazor WebAssembly caching and service workers](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app?view=aspnetcore-10.0)

## 🏷️ Tags

`blazor` `dotnet` `interactiveauto` `render-modes` `caching` `signalr` `wasm` `troubleshooting` `development-workflow` `learning-insight` `browser-cache` `service-worker` `medium-priority` `demo-project`
