---
date: 2025-12-14
type: RFC
severity: N/A
status: Open for Comment
tags:
  - architecture
  - demo5
  - dotnet
  - microservices
  - bff
---

# RFC: Co-Hosted vs Separate Process Architectures in Demo5

## Summary

Demo5 introduces a separate process architecture with the Blazor BFF on port 7210 and a downstream WeatherApi on port 7220, contrasting demo3's co-hosted monolithic approach. This RFC explores when to choose co-hosted (monolithic) vs separate process (microservices-inspired) patterns for .NET demos, focusing on trade-offs in scalability, complexity, and development experience.

## Motivation

Architectural decisions impact maintainability, deployment, and scalability. With .NET 10's emphasis on modern patterns like BFF and downstream APIs, we need clear guidelines for when to co-host services in one process vs running them separately. This matters for demo consistency, real-world applicability, and teaching best practices without overcomplicating examples.

## Detailed Design

### Architectural Patterns Compared

- **Co-Hosted (Monolithic)**: All services (BFF, APIs, Identity) run in a single ASP.NET Core process. Used in demo3 (Demo3.BffRbac).
- **Separate Process (Microservices-Inspired)**: BFF and downstream APIs run as independent processes, communicating via HTTP. Implemented in demo5 (Demo5.DownstreamApi with Demo5.DownstreamApi.WeatherApi).

#### Visual Architecture Diagrams

**Separate Process Architecture (Demo5)**
```
┌─────────────────┐    ┌─────────────────┐
│ Blazor BFF App  │    │ WeatherApi      │
│ (Port 7210)     │◄──►│ (Port 7220)     │
│ - UI Components │    │ - Business Logic│
│ - BFF APIs      │    │ - Data Access   │
│ - Auth/Cookies  │    │ - Bearer Tokens │
└─────────────────┘    └─────────────────┘
     ▲                        ▲
     │                        │
  Cookies                  Bearer Token
  (HttpOnly)               (JWT)
```

**Co-Hosted Architecture (Demo3)**
```
┌──────────────────────────────┐
│  Blazor Web App (Port 7210)  │
│  ┌────────────────────────┐  │
│  │  UI Components (WASM)  │  │
│  ├────────────────────────┤  │
│  │  BFF APIs (Minimal)    │  │
│  │  - /api/weather        │  │
│  │  - /api/users          │  │
│  │  - /api/reports        │  │
│  ├────────────────────────┤  │
│  │  Business Logic Layer  │  │
│  │  - Weather Service     │  │
│  │  - User Service        │  │
│  ├────────────────────────┤  │
│  │  Data Access Layer     │  │
│  │  - Database Access     │  │
│  └────────────────────────┘  │
└──────────────────────────────┘
```

### Decision Matrix: Key Trade-Offs

| Criteria                   | Separate Process                            | Co-Hosted                     | Winner/Recommendation     |
| -------------------------- | ------------------------------------------- | ----------------------------- | ------------------------- |
| **Deployment Complexity**  | High - Multiple projects, ports, networking | Low - Single project          | Co-Hosted for simple apps |
| **Scalability**            | Excellent - Independent scaling             | Limited - Scales as unit      | Separate for high-scale   |
| **Token Management**       | Complex - JWT/OBO flows                     | Simple - Cookie auth          | Co-Hosted for simplicity  |
| **Security Boundaries**    | Strong - Process isolation                  | Weaker - Shared process       | Separate for security     |
| **Development Experience** | Isolated - Easier testing                   | Integrated - Faster iteration | Depends on team size      |
| **Performance**            | Network overhead per call                   | In-process method calls       | Co-Hosted for latency     |
| **Maintenance**            | Complex - Multiple services to manage       | Simple - Single codebase      | Co-Hosted for small teams |

### Architecture Comparison Table

| Aspect                | Co-Hosted (Monolithic)                   | Separate Process (Microservices-Inspired) |
| --------------------- | ---------------------------------------- | ----------------------------------------- |
| **Processes**         | 1 (everything in one)                    | 2+ (BFF + APIs on different ports)        |
| **Deployment**        | Single .dll or executable                | Multiple executables/services             |
| **Communication**     | In-process (method calls)                | HTTP/REST (Bearer tokens)                 |
| **Scaling**           | Vertical only (bigger server)            | Horizontal (add more API instances)       |
| **Token Flow**        | Cookie-based (no tokens exposed)         | OAuth scopes + Bearer tokens (JWT)        |
| **Complexity**        | Low - simpler to develop/deploy          | High - more moving parts                  |
| **Separation**        | Logical only (different classes/folders) | Physical & logical (separate processes)   |
| **Team Structure**    | Single team or tightly coupled           | Independent/loosely coupled teams         |
| **Example in Series** | demo3 (Demo3.BffRbac)                    | demo5 (BFF + WeatherApi separate)         |

### Decision Factors and Trade-Offs

- **Simplicity vs Scalability**: Co-hosted is simpler for demos (single project, easier debugging), but separate processes allow independent scaling and deployment.
- **Development Experience**: Co-hosted reduces setup overhead; separate processes mimic production microservices but increase complexity (multiple ports, CORS, token validation).
- **Security**: Separate processes enable better isolation (e.g., Bearer tokens for API calls); co-hosted relies on in-process auth.
- **Performance**: Co-hosted avoids network latency; separate processes introduce HTTP overhead but support load balancing.
- **Maintenance**: Co-hosted is easier for small teams; separate processes require more orchestration (Docker, Kubernetes).

### Current Implementations

- **Demo3**: Co-hosted BFF with embedded weather API endpoints in the same process.
- **Demo5**: Separate BFF (port 7210) calling WeatherApi (port 7220) server-to-server with Bearer tokens.

## Alternatives Considered

### Hybrid Approach: Pragmatic Middle Ground

The **Hybrid Architecture** is a compromise between full co-hosting and complete separation. It acknowledges that not all services need to be extracted.

#### Hybrid Model: What Would It Look Like?

```
┌──────────────────────────┐    ┌──────────────────┐
│   Blazor BFF + Core      │    │  WeatherApi      │
│   (Port 7210)            │◄──►│  (Port 7220)     │
│ ┌──────────────────────┐ │    │ ┌──────────────┐ │
│ │ UI Components        │ │    │ │ Business     │ │
│ ├──────────────────────┤ │    │ │ Logic        │ │
│ │ Auth (Cookies)       │ │    │ ├──────────────┤ │
│ ├──────────────────────┤ │    │ │ Bearer Token  │ │
│ │ Core APIs:           │ │    │ │ Validation   │ │
│ │ - /api/users         │ │    │ └──────────────┘ │
│ │ - /api/reports       │ │    │                  │
│ ├──────────────────────┤ │    └──────────────────┘
│ │ In-Process Services  │ │
│ │ - User Service       │ │
│ │ - Report Service     │ │
│ └──────────────────────┘ │
└──────────────────────────┘
```

#### Hybrid Approach Rationale

**When Hybrid Makes Sense:**
1. **Core vs Non-Core Split**: Keep frequently-accessed, security-critical features (user management, auth) in the BFF; externalize computationally expensive or specialty features (weather, reporting, integrations)
2. **Graduated Scaling**: Start monolithic, externalize services that become bottlenecks
3. **Team Structure**: Small core team owns BFF; specialized teams own external APIs
4. **Incremental Modernization**: Migrate from monolith to microservices gradually

**Hybrid Trade-Offs:**

| Aspect | Hybrid Benefit | Hybrid Cost |
|--------|----------------|------------|
| **Complexity** | Lower than full separation | Higher than pure co-hosting |
| **Performance** | In-process core calls + HTTP for specialty APIs | Some latency on external calls |
| **Scalability** | External APIs scale independently | Core BFF still vertically scaled |
| **Deployment** | Deploy BFF and external APIs independently | Coordinate two deployments |
| **Token Management** | Cookies for core, Bearer for external | Mixed auth models to manage |
| **Team Velocity** | Core team fast, external team independent | Coordination overhead |

#### Why Hybrid Was Rejected for Demo5

Demo5 deliberately chose **full separation** over hybrid because:

1. **Educational Clarity**: Demonstrates complete microservices pattern, not a partial one
2. **OBO Flow Teaching**: Full separation requires comprehensive OBO flow implementation—hybrid would obscure this
3. **Production Pattern**: Shows enterprise-grade architecture, not evolutionary/incremental approach
4. **Architectural Purity**: Avoids "kitchen sink" problem where BFF becomes bloated with "core" features

However, hybrid could be valuable for a **future demo** (e.g., demo6) showing:
- How to migrate from monolith (demo3) to microservices (demo5)
- Pragmatic compromises for real-world constraints
- Handling mixed authentication models (cookies + Bearer tokens)

#### Hybrid vs Alternatives Summary

```
Full Co-Hosting (Demo3)
    ↓
    Hybrid (Good for transition, not yet demonstrated)
    ↓
Full Separation (Demo5)
```

**Decision**: Demo5 went straight to full separation to maximize learning value. Hybrid is a valid architectural pattern for production systems but adds complexity without clear demo benefits.

- **In-Process Communication**: Use libraries like gRPC instead of HTTP. Not chosen to keep demos HTTP-focused.

## Unresolved Questions

- [ ] How do we balance demo simplicity with real-world patterns?
- [ ] Should future demos default to separate processes for scalability lessons?
- [ ] What tooling (e.g., Docker Compose) should we add for multi-process demos?
- [ ] How to handle cross-process debugging in development?