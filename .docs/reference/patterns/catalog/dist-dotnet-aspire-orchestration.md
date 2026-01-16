# .NET Aspire Orchestration


**Introduced:** demo5.1  
**Category:** Infrastructure / Orchestration  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
.NET Aspire is a modern cloud-native application stack for building observable, production-ready distributed applications. Simplifies orchestration, service discovery, and telemetry setup.

**Components:**
- **AppHost:** Orchestration code (C#) defining services, ports, dependencies
- **ServiceDefaults:** Shared telemetry, health checks, service discovery configuration
- **Dashboard:** Visual monitoring of services, logs, traces, metrics

**Service Discovery (Magic!):**
```
AppHost.cs:
var apiService = builder.AddProject<Projects.Demo51_ApiService>("apiservice");
var web = builder.AddProject<Projects.Demo51_Web>("webfrontend")
    .WithReference(apiService);  // Automatic service discovery!

// In webfrontend:
// HttpClient automatically resolves apiservice → http://apiservice
```

**Use Cases:**
- Local development of microservices
- Cloud-native app templates
- Observability from day one
- Reproducible infrastructure as code

**Strengths:**
- ✅ No manual service discovery config
- ✅ Unified logs/metrics dashboard
- ✅ C# first (no YAML)
- ✅ Easy containerization

**Weaknesses:**
- ❌ Relatively new (potential breaking changes)
- ❌ Requires learning Aspire patterns
- ❌ Production story still evolving

**Related Patterns:**
- [Distributed Modular Monolith](dist-modular-monolith.md)

**Demo References:**
- demo5.1: AppHost + ServiceDefaults

