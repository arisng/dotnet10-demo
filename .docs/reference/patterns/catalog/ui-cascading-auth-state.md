# Cascading Authentication State


**Introduced:** demo2  
**Category:** Blazor / UI  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Blazor pattern using `<CascadingAuthenticationState>` to provide `AuthenticationState` to all child components. Enables consistent auth state across server and WASM renders.

**Use Cases:**
- Blazor Web Apps with mixed render modes
- Consistent authentication across component tree
- Progressive enhancement support

**Implementation:**
```csharp
<CascadingAuthenticationState>
    <Router>
        <!-- app routes -->
    </Router>
</CascadingAuthenticationState>
```

**Strengths:**
- ✅ Built-in Blazor support
- ✅ Works across render modes
- ✅ Automatic state passing
- ✅ Simple to use

**Weaknesses:**
- ❌ Can't customize cascade path
- ❌ All components receive auth state

**Demo References:**
- demo2+: Foundation in all subsequent demos

