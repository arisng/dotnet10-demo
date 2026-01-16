# AuthorizeView for UI Authorization


**Introduced:** demo3  
**Category:** Blazor / UI  
**Complexity:** ⭐ (Foundational)

**Definition:**
Blazor built-in component for conditional rendering based on authorization state. Simplifies showing/hiding UI elements based on user roles or policies.

**Use Cases:**
- Hide admin features from non-admins
- Show feature based on user role
- Simple permission-based UI (alongside authorization handlers)

**Implementation:**
```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <p>Admin content</p>
    </Authorized>
    <NotAuthorized>
        <p>Not authorized</p>
    </NotAuthorized>
</AuthorizeView>
```

**Strengths:**
- ✅ Built-in component
- ✅ Declarative syntax
- ✅ Handles async auth state
- ✅ Simple use cases

**Weaknesses:**
- ❌ UX: doesn't prevent API access (hide doesn't mean secure)
- ❌ Limited for complex rules
- ❌ Not a replacement for server-side checks

**Demo References:**
- demo3+: Used throughout for UI gating

---

## Distributed Architecture Patterns
