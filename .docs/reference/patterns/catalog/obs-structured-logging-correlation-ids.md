# Structured Logging with Correlation IDs


**Introduced:** demo6+ (Planned)  
**Category:** Observability  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Logging with structured data (key-value pairs) and correlation IDs to trace requests across services. Enables efficient searching and debugging in distributed systems.

**Use Cases:**
- Distributed tracing across microservices
- Root cause analysis from logs
- Performance profiling
- Security audit trails

**Implementation (Serilog):**
```csharp
Log.Information("Processing order {OrderId} for tenant {TenantId}", 
    orderId, tenantId);
    
// Log context with correlation ID
LogContext.PushProperty("CorrelationId", correlationId);
```

**Strengths:**
- ✅ Machine-parseable logs
- ✅ Cross-service tracing
- ✅ Easy filtering/searching
- ✅ Audit trail support

**Weaknesses:**
- ❌ Requires structured logging setup
- ❌ Log volume management
- ❌ PII protection needed

**Related Patterns:**
- [OpenTelemetry Integration](obs-opentelemetry.md)

**Demo References:**
- demo6+: Structured logging throughout

