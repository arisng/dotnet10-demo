# Service Abstraction Pattern


**Introduced:** demo3  
**Category:** Data Access  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Abstraction pattern using interfaces to decouple components from concrete service implementations. Solves the "prerendering dependency injection" problem in Blazor where different implementations are needed at server vs. client.

**Problem:**
- Blazor SSR prerender needs database access (no HttpClient)
- Blazor WASM needs HttpClient (no database)
- Same component code, different implementations needed

**Solution:**
```
IWeatherService (interface)
    ├─ ServerWeatherService (database)
    └─ ClientWeatherService (HttpClient)

Component injects IWeatherService
    ├─ At server prerender: uses ServerWeatherService
    └─ At WASM runtime: uses ClientWeatherService
```

**Implementation:**
- Define shared interfaces in `.Shared` project
- Implement for server (database access)
- Implement for client (HttpClient)
- Register appropriately in each DI container

**Strengths:**
- ✅ Single component code works everywhere
- ✅ Type-safe at compile time
- ✅ Easy to test with mocks
- ✅ Clear interface contracts

**Weaknesses:**
- ❌ Requires multiple implementations
- ❌ Potential for implementation skew
- ❌ More code to maintain

**Related Patterns:**
- Dependency Injection

**Demo References:**
- demo3: IWeatherService abstraction
- demo4+: Consistent across all demos

---

## Messaging & Event Patterns
