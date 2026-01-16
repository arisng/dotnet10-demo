# OpenTelemetry Integration


**Introduced:** demo5.1 (AppHost uses it)  
**Category:** Observability  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Unified observability framework for collecting metrics, traces, and logs. Integrates with ASP.NET Core built-in instrumentation.

**Use Cases:**
- Distributed tracing across services
- Performance metrics collection
- Anomaly detection
- Operational dashboards

**Implementation:**
- Metrics: Authorization checks, HTTP requests, custom business metrics
- Traces: Request flow through services
- Logs: Structured logging with context

**Strengths:**
- ✅ Unified instrumentation
- ✅ Vendor-agnostic (OTEL standard)
- ✅ Built-in ASP.NET Core support
- ✅ Low overhead

**Weaknesses:**
- ❌ Complex configuration
- ❌ Sampling strategy needed
- ❌ Storage/cost at scale

**Related Patterns:**
- [Structured Logging](obs-structured-logging-correlation-ids.md)

**Demo References:**
- demo5.1: OpenTelemetry via ServiceDefaults
- demo6+: Enhanced metrics and tracing

---

## Component & UI Patterns
