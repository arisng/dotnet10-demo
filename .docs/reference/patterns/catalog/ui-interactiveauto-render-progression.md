# InteractiveAuto Render Mode Progression


**Introduced:** demo2  
**Category:** Blazor / UI  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Blazor Web App render mode that progressively enhances: server prerender + interactive server (SSR with SignalR) → WASM when loaded. Optimizes for fast first view while supporting full WASM after download.

**4-Phase Lifecycle (First Visit):**
```
1. Server Prerender (HTML → client)
2. Interactive Server (SignalR → client, WASM downloading)
3. WASM Initialized (WASM → ready)
4. Interactive WASM (WASM → active)
```

**Subsequent Visits:**
- WASM cached (Local Storage check)
- Skip phases 1, 2 → go straight to 3, 4

**Use Cases:**
- Optimal perceived performance
- SEO-friendly rendering
- Works offline after WASM cached
- Mobile-friendly

**Strengths:**
- ✅ Fast initial load
- ✅ Seamless transition
- ✅ SEO support
- ✅ Works without JavaScript initially

**Weaknesses:**
- ❌ Complex render mode switching
- ❌ Requires service abstraction
- ❌ Caching behavior unintuitive

**Related Patterns:**
- [Service Abstraction Pattern](data-service-abstraction.md)
- [Cascading Authentication State](ui-cascading-auth-state.md)

**Demo References:**
- demo2: Full diagnostics of InteractiveAuto phases
- demo3+: Standard render mode for all demos

