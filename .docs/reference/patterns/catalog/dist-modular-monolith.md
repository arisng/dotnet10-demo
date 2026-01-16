# Distributed Modular Monolith


**Introduced:** demo5.1  
**Category:** Architecture  
**Complexity:** ⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Architecture combining modular monolith patterns (vertical slices, domain-driven design) with distributed deployment (separate Frontend and Backend services). Balances monolith simplicity with distributed system benefits.

**Components:**
- **Frontend (BFF):** Blazor UI + authentication + YARP proxy
- **Backend (API Service):** Modular monolith with vertical slices
- **Orchestrator:** .NET Aspire for service discovery + configuration

**Vertical Slices (Example):**
```
Weather Domain
├── WeatherController/Endpoints
├── WeatherService
├── WeatherEntity
└── WeatherRepository

User Domain
├── UserController/Endpoints
├── UserService
├── UserEntity
└── UserRepository
```

**Service Topology (Local):**
```
AppHost (Orchestrator)
├─ Frontend (port 7210)
│  └─ YARP → ApiService
├─ ApiService (port 7220)
│  └─ Database
└─ ServiceDefaults (shared observability)
```

**Use Cases:**
- Monolith becoming complex → split frontend/backend
- Multiple frontend variants (web, mobile)
- Independent frontend/backend team autonomy
- Cloud-native deployments (Kubernetes-ready)

**Strengths:**
- ✅ Cleaner separation of concerns
- ✅ Frontend can scale independently
- ✅ Vertical slices enable clear ownership
- ✅ Easier testing (unit + integration)

**Weaknesses:**
- ❌ More complex than monolith
- ❌ Network latency (frontend → backend)
- ❌ Distributed debugging difficulty
- ❌ Operational complexity

**Related Patterns:**
- [YARP Proxy](api-yarp-reverse-proxy.md)
- [Aspire Orchestration](dist-dotnet-aspire-orchestration.md)

**Demo References:**
- demo5.1: Complete implementation with Aspire + YARP

