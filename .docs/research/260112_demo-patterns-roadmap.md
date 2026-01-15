# Demo Patterns Roadmap: Selecting & Sequencing Patterns for demo6-demo9

**Purpose:** Strategic roadmap for which patterns to implement in upcoming demos, with justification, dependencies, and milestone criteria.

**Date:** 2026-01-12  
**Scope:** demo6 (Multi-Tenant SaaS), demo7 (Feature Flags & Hardening), demo8 (Choreographed Sagas), demo9 (State Machine Sagas)

---

## Philosophy: Pattern Selection Criteria

### 1. **Dependency Chain** 
Each demo builds on earlier patterns. No pattern introduced until its prerequisites are solid.

**Example:** State Machine Sagas (demo9) require Outbox + Inbox (demo6) + Choreographed Sagas (demo8).

### 2. **Complexity Progression**
Introduce increasingly advanced patterns, giving users time to absorb foundational concepts.

**Trajectory:**
- demo1-2: Foundation (authentication)
- demo3: Authorization (permissions)
- demo4: Multi-source identity (Entra ID)
- demo5: Token-based APIs (Bearer + OBO)
- demo5.1: Distributed systems (Aspire + YARP)
- **demo6: Multi-tenancy + Reliability (Finbuckle + Outbox/Inbox)**
- **demo7: Business control + Observability (Feature Flags + Logs + Correlation)**
- **demo8: Distributed workflows (Choreographed Sagas)**
- **demo9: Workflow visibility (State Machine Sagas)**

### 3. **Business Value Timing**
Introduce patterns when they solve real problems revealed by earlier demos.

**Example:** Feature flags (demo7) solve "how do we toggle features per tenant?" - a problem only visible after multi-tenancy (demo6).

### 4. **Operational Readiness**
Ensure patterns support running the demos locally and in Aspire dashboard.

---

## demo6: Multi-Tenant SaaS (The Foundation)

**Goal:** Add SaaS capabilities: per-tenant isolation, per-tenant configuration, multi-identity toggle.

**Current State:** demo5.1 is single-tenant with Distributed Modular Monolith.

**Roadmap for demo6:**

### Patterns to Introduce

#### 1. **Finbuckle Multi-Tenant** (Primary)
- **Why Now:** Essential for SaaS foundation
- **Complexity:** ⭐⭐⭐ (Advanced, but well-documented)
- **Dependencies:** None (orthogonal to auth)
- **Implementation Timeline:** Weeks 1-2

**Key Components:**
- Tenant resolution (host, header, route)
- Data isolation strategy (Shared DB vs. Dedicated DB)
- ITenantInfo injection into services
- Per-tenant connection strings

**Success Criteria:**
- ✅ Two simultaneous tenants with isolated data
- ✅ Tenant resolution from headers
- ✅ Query filters prevent cross-tenant leaks
- ✅ Aspire dashboard shows tenant context

**Example Setup:**
```
Tenant A: acme.localhost:7210
Tenant B: contoso.localhost:7210

Each tenant has isolated:
- Users
- Permissions
- Orders/Weather data
- Feature configuration
```

---

#### 2. **Multi-Identity per Tenant** (Secondary)
- **Why Now:** Complement Finbuckle; SaaS requirement
- **Complexity:** ⭐⭐ (Moderate)
- **Dependencies:** Finbuckle (above), demo4 (Entra ID foundation)
- **Implementation Timeline:** Weeks 2-3

**Key Components:**
- Per-tenant identity configuration (Entra vs. Passkey)
- Dynamic auth handler selection based on tenant
- Mixed auth in same app

**Example Scenarios:**
```
Tenant A (Enterprise): Entra ID only
├─ Users sign in via "Sign in with Microsoft"
└─ Default role assignment from Entra App Roles

Tenant B (SMB): Passkey only
├─ Users register with passkey
└─ Manual role assignment (not Entra)

Tenant C (Hybrid): Both Entra + Passkey
├─ User chooses at login
└─ Role mapping per source
```

**Success Criteria:**
- ✅ Tenant can toggle Entra vs. Passkey at startup
- ✅ Auth state probe shows active provider
- ✅ Permissions flow through regardless of auth source
- ✅ Future (demo6+): Runtime toggle via feature flags

---

#### 3. **Outbox Pattern** (Foundational for Reliability)
- **Why Now:** Foundation for demo7+ saga patterns
- **Complexity:** ⭐⭐⭐ (Advanced, but isolated)
- **Dependencies:** None (new tables, background service)
- **Implementation Timeline:** Weeks 3-4

**Key Components:**
- OutboxEvent table (EventType, Payload, PublishedAt)
- Background publisher service (polling)
- Tenant-scoped event publishing

**Use Case:**
```
User Signs Up
├─ Create ApplicationUser
├─ Insert OutboxEvent: "UserSignedUp"
└─ Commit (atomic)
    ↓
Background Publisher (every 5 seconds)
├─ Read unpublished OutboxEvents
├─ Publish to message broker (or in-memory bus for demo)
└─ Mark as PublishedAt
```

**Success Criteria:**
- ✅ Events reliably persisted (no lost events)
- ✅ Background publisher works with Aspire
- ✅ Multi-tenant isolation (events scoped by TenantId)
- ✅ Dashboard shows event count/latency

---

#### 4. **Inbox Pattern** (Idempotent Consumption)
- **Why Now:** Protect against duplicate processing
- **Complexity:** ⭐⭐⭐ (Advanced)
- **Dependencies:** Outbox (above)
- **Implementation Timeline:** Week 4

**Key Components:**
- InboxEvent table (Id, EventType, ProcessedAt)
- Check inbox before processing
- Mark processed after side effects

**Use Case:**
```
OutboxEvent published: "UserSignedUp" (event ID = abc123)
    ↓
Consumer Handler
├─ Check Inbox: has abc123 been processed?
├─ No → Process + Record in Inbox
└─ Yes → Idempotent retry (return cached result)
```

**Success Criteria:**
- ✅ Retry of same event is idempotent
- ✅ No duplicate side effects (e.g., email sent twice)
- ✅ Multi-tenant Inbox isolation

---

#### 5. **Structured Logging with Correlation IDs** (Observability)
- **Why Now:** Support debugging + tenant context tracing
- **Complexity:** ⭐⭐ (Intermediate, familiar from demo5.1)
- **Dependencies:** None
- **Implementation Timeline:** Week 4

**Key Components:**
- Correlation ID extraction from requests
- Serilog structured logging
- TenantId + UserId in all logs
- OpenTelemetry integration (from demo5.1)

**Example Log:**
```
[INF] Outbox publisher processed event
  CorrelationId: 550e8400-e29b-41d4-a716-446655440000
  TenantId: acme
  EventType: UserSignedUp
  EventId: abc123
  PublishedAt: 2026-01-12T10:15:30Z
```

**Success Criteria:**
- ✅ Logs include TenantId + CorrelationId
- ✅ Aspire dashboard shows correlated logs
- ✅ Distributed trace follows request → event → side effect

---

### demo6 Architecture

```
AppHost (Aspire)
├─ Frontend (Blazor + YARP + Tenant Resolver)
│  ├─ Tenant resolution (header, host, etc.)
│  ├─ Auth: Passkey or Entra (per tenant)
│  └─ Cookie session
│
├─ ApiService (Modular Monolith)
│  ├─ Vertical Slice: Users (Finbuckle isolated)
│  ├─ Vertical Slice: Orders (Finbuckle isolated)
│  ├─ Vertical Slice: Weather (Finbuckle isolated)
│  ├─ OutboxPublisher Background Service
│  ├─ Database (Finbuckle query filters or dedicated DB)
│  └─ Structured logging + Correlation IDs
│
└─ ServiceDefaults (Observability)
   ├─ OpenTelemetry
   └─ Health Checks
```

### demo6 Success Criteria (Milestone)

- ✅ Two simultaneous tenants with completely isolated data
- ✅ Multi-identity toggle: Tenant A = Entra, Tenant B = Passkey
- ✅ Outbox events published reliably (visible in dashboard)
- ✅ Inbox prevents duplicate processing (verified in logs)
- ✅ Structured logs with TenantId + CorrelationId
- ✅ All BFF APIs (weather, users, reports) work per-tenant
- ✅ Aspire dashboard shows service health + event metrics

---

## demo7: Feature Flags & Hardening (Business Control)

**Goal:** Add dynamic feature control and production hardening.

**Current State:** demo6 has multi-tenancy + reliable events.

**Roadmap for demo7:**

### Patterns to Introduce

#### 1. **Feature Flags (Microsoft.FeatureManagement)** (Primary)
- **Why Now:** Control features per tenant without redeployment
- **Complexity:** ⭐⭐ (Intermediate, well-documented)
- **Dependencies:** Finbuckle (to scope flags per tenant)
- **Implementation Timeline:** Weeks 1-2

**Key Components:**
- FeatureToggle table (FeatureName, Enabled, TenantId)
- IFeatureManager injection
- Blazor component conditional rendering
- API endpoint gating

**Example Use Case: Premium Reports**
```
Feature Name: PremiumReports
├─ Enabled for Tenant A (enterprise plan)
├─ Disabled for Tenant B (starter plan)
└─ Disabled globally during maintenance

Feature Check:
[Authorize(Roles = "Manager")]
[FeatureGate("PremiumReports")]
public async Task<IResult> ExportReports(...)
{
    // Only runs if feature enabled for tenant
}
```

**Success Criteria:**
- ✅ Feature flag table created + seeded
- ✅ At least 2 features (e.g., PremiumReports, AdvancedAnalytics)
- ✅ Feature gates in BFF APIs
- ✅ Blazor components conditionally render per feature
- ✅ Per-tenant feature toggle (no redeployment)
- ✅ Aspire dashboard shows feature status

---

#### 2. **Azure App Configuration Integration** (Optional, Future Graduation)
- **Why Now:** Centralized feature management (cloud-ready)
- **Complexity:** ⭐⭐⭐ (Advanced)
- **Dependencies:** Feature Flags (above), Azure subscription
- **Implementation Timeline:** Deferred to production migration

**Note for demo7:** Use in-memory feature store (FeatureToggle table). Azure AppConfig integration can be added during production hardening phase.

---

#### 3. **Hardened Authorization: Scope Validation** (Defense-in-Depth)
- **Why Now:** Implement "Two Locks" model from demo5.1 research
- **Complexity:** ⭐⭐⭐ (Advanced, but demo5.1 foundation)
- **Dependencies:** Bearer token validation (demo5), OAuth scopes
- **Implementation Timeline:** Week 2

**Key Components:**
- Outer Lock: OAuth scope validation (e.g., `access_as_user`)
- Inner Lock: Local RBAC permissions (existing)

**Example Endpoint:**
```csharp
app.MapGet("/api/reports/export", ExportReports)
    .RequireAuthorization(policy => 
        policy.RequireClaim("scp", "access_as_user"))  // Outer lock
    .Produce<byte[]>();

// In handler:
[Authorize(Roles = "Manager")]  // Inner lock (local RBAC)
private static async Task ExportReports(HttpContext context, ...)
{
    // Both checks must pass
}
```

**Success Criteria:**
- ✅ ApiService validates OAuth scopes (outer lock)
- ✅ Permission handlers validate RBAC (inner lock)
- ✅ Endpoint returns 403 if either lock fails
- ✅ Logs show which lock denied the request

---

#### 4. **Enhanced Observability: Custom Metrics** (Production-Ready)
- **Why Now:** Monitor feature flags + authorization patterns
- **Complexity:** ⭐⭐ (Intermediate, OpenTelemetry foundation from demo5.1)
- **Dependencies:** OpenTelemetry (demo5.1), Structured Logging (demo6)
- **Implementation Timeline:** Week 3-4

**Key Metrics to Add:**
- `feature_flag_check` (counter): Which flags checked, enabled/disabled
- `authorization_scope_failure` (counter): Outer lock failures
- `authorization_permission_failure` (counter): Inner lock failures
- `outbox_publish_latency` (histogram): Event publishing duration
- `inbox_idempotent_retry` (counter): Duplicate event handling

**Example:**
```csharp
public class PremiumReportsMetrics
{
    private readonly Counter<int> _featureFlagChecks;
    
    public PremiumReportsMetrics(IMeterFactory factory)
    {
        var meter = factory.Create("DemoApp.Features");
        _featureFlagChecks = meter.CreateCounter<int>(
            "feature_flag_checks",
            description: "Count of feature flag evaluations");
    }
    
    public async Task<bool> IsPremiumReportsEnabled(string tenantId)
    {
        var enabled = await _featureManager.IsEnabledAsync("PremiumReports");
        _featureFlagChecks.Add(1, new KeyValuePair<string, object?>("tenant", tenantId), 
            new KeyValuePair<string, object?>("enabled", enabled));
        return enabled;
    }
}
```

**Success Criteria:**
- ✅ Feature flag checks emitted as metrics
- ✅ Authorization failures tracked per tenant
- ✅ Event publishing latency measured
- ✅ Aspire dashboard shows custom metrics

---

#### 5. **Content Security Policy (CSP) Headers** (Security Hardening)
- **Why Now:** Prevent XSS attacks in distributed monolith
- **Complexity:** ⭐⭐ (Intermediate)
- **Dependencies:** None (middleware)
- **Implementation Timeline:** Week 4

**Implementation:**
```csharp
// Program.cs (ApiService)
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; script-src 'self'";
    await next();
});
```

**Success Criteria:**
- ✅ CSP headers present in API responses
- ✅ HSTS (Strict-Transport-Security) configured
- ✅ X-Frame-Options set to prevent clickjacking

---

### demo7 Architecture

```
Same as demo6, but enhanced with:

Frontend (Blazor)
├─ Feature flag checks in components
│  └─ Conditional rendering for PremiumReports
└─ Permission-based UI gating

ApiService
├─ Feature flag middleware
│  ├─ Check before entering endpoint
│  └─ Return 404 if feature disabled
├─ Outer lock: Scope validation
├─ Inner lock: RBAC permissions
├─ Custom metrics collection
└─ CSP + HSTS headers
```

### demo7 Success Criteria (Milestone)

- ✅ At least 2 feature flags working (per-tenant toggle)
- ✅ API returns 404 if feature disabled (graceful degradation)
- ✅ Blazor UI conditionally renders per feature
- ✅ "Two Locks" authorization model implemented (scope + RBAC)
- ✅ Custom metrics visible in Aspire dashboard
- ✅ Structured logs with CorrelationId + TenantId
- ✅ CSP headers present, no console security warnings
- ✅ Tenant data isolation still perfect (no cross-tenant leaks)

---

## demo8: Choreographed Sagas (Distributed Workflows)

**Goal:** Implement event-driven workflows with choreographed saga pattern.

**Current State:** demo7 has feature flags + hardening + reliable events.

**Roadmap for demo8:**

### Patterns to Introduce

#### 1. **Choreographed Saga with Message Broker** (Primary)
- **Why Now:** Real-world workflow use case (demo6+7 foundation)
- **Complexity:** ⭐⭐⭐⭐ (Very Advanced)
- **Dependencies:** Outbox (demo6), Inbox (demo6), Feature Flags (demo7)
- **Implementation Timeline:** Weeks 1-3

**Key Components:**
- Message Broker (Azure Service Bus with Sessions or In-Memory for demo)
- Per-aggregate ordering (OrderId → sequential processing)
- Event handlers that publish next step
- Compensation logic for failures

**Use Case: Order Fulfillment Saga**
```
[OrderPlaced Event]
  ├─ Data: OrderId, CustomerId, Items, Amount
  └─ Published to OutboxEvent table
    
Background Publisher
  └─ Read unpublished events → Publish to broker (Session: OrderId)

Broker (Session: order-123)
  ├─ Consumer 1: OrderPlacedHandler
  │  ├─ Validate order
  │  ├─ Reserve inventory
  │  └─ Publish: InventoryReservedEvent
  │
  ├─ Consumer 2: InventoryReservedHandler
  │  ├─ Request payment
  │  └─ Publish: PaymentProcessingEvent
  │
  └─ Consumer 3: PaymentProcessingHandler
     ├─ Charge customer
     └─ Publish: PaymentCapturedEvent OR PaymentFailedEvent
```

**Per-Aggregate Ordering (Key!):**
```
Order-123 Events:
  1. OrderPlaced (t=10:00:00)
  2. InventoryReserved (t=10:00:01) ← Sequential!
  3. PaymentCaptured (t=10:00:02)

Order-456 Events (parallel):
  1. OrderPlaced (t=10:00:00.5)
  2. InventoryReserved (t=10:00:01.5) ← Independent!
  3. PaymentCaptured (t=10:00:02.5)
```

**Message Broker Choice (Demo):**
- Azure Service Bus Sessions (if Azure resources available)
- In-Memory pub/sub (simpler for local demo)
- Reference: See `.docs/research/260112_message-ordering-saga-patterns.md`

**Compensation Logic:**
```
PaymentFailed Event
  └─ CompensationHandler
     ├─ Release reserved inventory
     ├─ Notify customer
     └─ Publish: OrderCancelledEvent
```

**Success Criteria:**
- ✅ Order saga completes: OrderPlaced → Reserved → Paid → Shipped
- ✅ Per-aggregate ordering enforced (no out-of-order processing)
- ✅ Different orders process in parallel (horizontal scaling)
- ✅ Compensation works (payment fails → inventory released)
- ✅ Saga events visible in Aspire dashboard
- ✅ Multi-tenant isolation: events scoped by TenantId

---

#### 2. **Per-Tenant Saga Isolation** (Secondary)
- **Why Now:** Multi-tenant sagas must not cross boundaries
- **Complexity:** ⭐⭐ (Moderate, builds on Finbuckle)
- **Dependencies:** Finbuckle (demo6), Choreographed Saga (above)
- **Implementation Timeline:** Week 2

**Key Components:**
- TenantId in all saga events
- Message broker filters by TenantId
- Handlers check tenant context

**Example:**
```csharp
[OutboxEvent(TenantId = "acme", AggregateType = "Order", AggregateId = order123)]
public class OrderPlacedEvent
{
    public string TenantId { get; set; }
    public Guid OrderId { get; set; }
    // ...
}
```

**Success Criteria:**
- ✅ Tenant A orders don't affect Tenant B orders
- ✅ Handler logs include TenantId
- ✅ Saga compensation respects tenant boundaries

---

#### 3. **Saga Monitoring & Dashboarding** (Observability)
- **Why Now:** Complex workflows need visibility
- **Complexity:** ⭐⭐⭐ (Advanced, custom dashboards)
- **Dependencies:** OpenTelemetry (demo5.1), Structured Logging (demo6)
- **Implementation Timeline:** Week 3

**Key Components:**
- Saga step counter (started, completed, failed)
- Saga duration histogram (e.g., 100ms-5s range)
- Failure reasons tracking
- Aspire dashboard custom widget

**Example Metrics:**
```
saga_order_steps_total{step="reserve_inventory", status="success"} 1500
saga_order_steps_total{step="reserve_inventory", status="failure"} 12
saga_order_duration_seconds{p50=0.5, p95=2.3, p99=4.8}
saga_compensation_total{tenant="acme", reason="payment_failed"} 3
```

**Dashboard Shows:**
- Orders in flight (count by step)
- Success rate per step
- Compensation rate
- Average latency per step
- Failures by reason

**Success Criteria:**
- ✅ Saga start/completion events emitted
- ✅ Metrics visible in Aspire dashboard
- ✅ Can answer: "How many orders are stuck in inventory reservation?"
- ✅ Can identify: "Which saga step fails most often?"

---

### demo8 Architecture

```
Same as demo7, but enhanced:

Message Broker (In-Memory or Azure Service Bus)
├─ Sessions by OrderId (per-aggregate ordering)
└─ Per-tenant message filtering

ApiService
├─ Saga Handlers (choreographed)
│  ├─ OrderPlacedHandler
│  ├─ InventoryReservedHandler
│  ├─ PaymentProcessingHandler
│  ├─ CompensationHandler
│  └─ (all publish next event)
│
├─ Saga Events → OutboxEvent table
├─ Compensation logic
├─ Saga metrics collection
└─ Multi-tenant saga isolation
```

### demo8 Success Criteria (Milestone)

- ✅ End-to-end order fulfillment saga works
- ✅ Per-aggregate ordering enforced (no race conditions)
- ✅ Multiple orders process in parallel
- ✅ Payment failure triggers compensation (inventory released)
- ✅ Saga events visible in Aspire dashboard
- ✅ Saga metrics (duration, failure rate) tracked
- ✅ Multi-tenant sagas completely isolated
- ✅ All demo6+7 features still work (feature flags, RBAC, etc.)

---

## demo9: State Machine Sagas (Orchestrated Workflows)

**Goal:** Mature choreographed sagas into explicit, observable state machines.

**Current State:** demo8 has choreographed sagas.

**Roadmap for demo9:**

### Patterns to Introduce

#### 1. **State Machine Saga with MassTransit** (Primary)
- **Why Now:** Choreography becomes complex → explicit state machine
- **Complexity:** ⭐⭐⭐⭐⭐ (Very Advanced)
- **Dependencies:** Choreographed Saga (demo8), MassTransit NuGet package
- **Implementation Timeline:** Weeks 1-3

**Key Components:**
- Saga state machine definition (states, events, transitions)
- Saga instance state persistence
- Correlation ID linking events to saga instances
- Timeout handling
- Compensation explicit in state machine

**Example: Order Fulfillment State Machine**
```csharp
public class OrderFulfillmentSagaStateMachine : MassTransitStateMachine<OrderFulfillmentState>
{
    public State Submitted { get; private set; }
    public State PaymentProcessing { get; private set; }
    public State Shipping { get; private set; }
    public State Completed { get; private set; }
    public State Compensating { get; private set; }
    public State Failed { get; private set; }

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; }
    public Event<PaymentCaptured> PaymentCapturedEvent { get; private set; }
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; }
    public Event<OrderShipped> OrderShippedEvent { get; private set; }

    public OrderFulfillmentSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        During(Submitted,
            When(OrderPlacedEvent)
                .Then(context => {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.Amount = context.Data.Amount;
                })
                .Schedule(
                    PaymentTimeout,
                    context => context.Init<PaymentTimeoutExpired>(new { context.Data.OrderId }),
                    TimeSpan.FromMinutes(5))
                .TransitionTo(PaymentProcessing)
                .Publish(context => new CapturePayment(context.Instance.OrderId, context.Instance.Amount))
        );

        During(PaymentProcessing,
            When(PaymentCapturedEvent)
                .Unschedule(PaymentTimeout)
                .TransitionTo(Shipping)
                .Publish(context => new ShipOrder(context.Instance.OrderId)),
            
            When(PaymentFailedEvent)
                .Unschedule(PaymentTimeout)
                .TransitionTo(Compensating)
                .PublishAsync(context => PublishCompensation(context))
        );

        During(Shipping,
            When(OrderShippedEvent)
                .TransitionTo(Completed)
                .Finalize()
        );

        During(Compensating,
            When(CompensationCompletedEvent)
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}

public class OrderFulfillmentState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
```

**State Diagram:**
```
[Submitted]
    ↓ OrderPlaced
[PaymentProcessing]
    ├─ PaymentCaptured (success) → [Shipping]
    ├─ PaymentFailed → [Compensating] → [Failed]
    └─ Timeout (5 min) → [Compensating] → [Failed]

[Shipping]
    ↓ OrderShipped
[Completed]
```

**Differences from Choreography:**
| Aspect | Choreographed (demo8) | State Machine (demo9) |
|--------|---------------------|---------------------|
| **Workflow Definition** | Implicit (handlers publish next) | Explicit (state machine) |
| **State** | Distributed (no central record) | Centralized (saga instance) |
| **Visibility** | Hard to debug | Easy to debug (query saga state) |
| **Timeouts** | Manual in each handler | Built-in, automatic |
| **Compensation** | Manual logic in handler | Explicit in state machine |
| **Observability** | Event logs only | Event logs + saga state history |

**Success Criteria:**
- ✅ Order saga state machine defined + registered in MassTransit
- ✅ State transitions logged and queryable
- ✅ Timeout after 5 minutes → compensation triggered
- ✅ Payment failure → compensation → state = Failed
- ✅ Saga instance state persisted in database
- ✅ Aspire dashboard shows saga instance states

---

#### 2. **Saga Instance Querying & Debugging** (Secondary)
- **Why Now:** Ops teams need to find stuck orders
- **Complexity:** ⭐⭐ (Moderate)
- **Dependencies:** State Machine Saga (above)
- **Implementation Timeline:** Week 2

**Key Components:**
- SQL queries to find sagas in specific state
- API endpoint: `GET /api/sagas/orders?state=PaymentProcessing`
- Manual intervention endpoint: `POST /api/sagas/orders/{sagaId}/resume`

**Example Query:**
```sql
SELECT CorrelationId, OrderId, CurrentState, CreatedAt, Amount
FROM OrderFulfillmentState
WHERE CurrentState = 'PaymentProcessing'
  AND CreatedAt < DATEADD(minute, -5, GETDATE())
ORDER BY CreatedAt ASC;
```

**Manual Intervention Example:**
```csharp
[HttpPost("/api/sagas/orders/{sagaId}/resume")]
[Authorize(Roles = "Admin")]
public async Task ResumeSaga(Guid sagaId)
{
    var saga = await dbContext.OrderFulfillmentStates
        .FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
    
    if (saga?.CurrentState == "Compensating")
    {
        // Publish event to retry payment
        await publishEndpoint.Publish(new RetryPayment(saga.OrderId));
    }
}
```

**Success Criteria:**
- ✅ Query stuck sagas (WHERE state = X AND age > 5 min)
- ✅ Admin UI shows stuck order sagas
- ✅ Manual resume capability
- ✅ Logs show intervention (who, when, why)

---

#### 3. **Saga Versioning & Evolution** (Advanced Topic)
- **Why Now:** Long-running sagas can't change mid-flight
- **Complexity:** ⭐⭐⭐ (Advanced)
- **Dependencies:** State Machine Saga (above)
- **Implementation Timeline:** Deferred to production guide

**Note for demo9:** Implement basic saga versioning (v1) with migration guide for future versions. Document the challenge of evolving sagas.

---

#### 4. **Production Observability: Saga Traces** (Observability)
- **Why Now:** End-to-end tracing of multi-step workflows
- **Complexity:** ⭐⭐⭐ (Advanced, OpenTelemetry + custom activities)
- **Dependencies:** OpenTelemetry (demo5.1), Structured Logging (demo6)
- **Implementation Timeline:** Week 3

**Key Components:**
- Activity (OpenTelemetry) per saga step
- Span parent-child relationships
- Duration per step
- Failure details (exception + context)

**Example Trace:**
```
Trace: OrderFulfillment (order-123)
├─ Activity: OrderPlaced (10:00:00, duration 10ms)
├─ Activity: PaymentCapture (10:00:01, duration 500ms)
│  └─ Span: CallPaymentGateway (duration 450ms)
│  └─ Span: ValidateResponse (duration 50ms)
├─ Activity: ShipOrder (10:00:02, duration 200ms)
│  └─ Span: CreateShippingLabel (duration 100ms)
│  └─ Span: NotifyCarrier (duration 100ms)
└─ Total: 710ms
```

**Success Criteria:**
- ✅ OpenTelemetry trace shows saga step progression
- ✅ Aspire dashboard displays trace with durations
- ✅ Exception details visible in failed steps
- ✅ Can identify bottlenecks (longest steps)

---

### demo9 Architecture

```
Same as demo8, but:

Message Broker
└─ Routes events to MassTransit saga

MassTransit
├─ State Machine: OrderFulfillmentSagaStateMachine
├─ Saga Instances: OrderFulfillmentState (DB-backed)
├─ Saga Container: Dependency injection
└─ Timeouts: Scheduled via MassTransit

Database
├─ OrderFulfillmentState table (saga instance state)
├─ OrderFulfillmentState_CurrentState index (fast queries)
└─ MassTransit_TimeoutState table (timeout persistence)

Admin API
├─ GET /api/sagas/orders (query by state/age)
├─ GET /api/sagas/orders/{sagaId} (state details)
└─ POST /api/sagas/orders/{sagaId}/resume (manual intervention)
```

### demo9 Success Criteria (Milestone)

- ✅ State machine saga processes orders end-to-end
- ✅ Saga states persisted (queryable)
- ✅ Timeout after 5 min triggers compensation
- ✅ Can find stuck orders via admin API
- ✅ Manual intervention (resume) works
- ✅ OpenTelemetry traces show full saga flow + durations
- ✅ Saga versioning documented (v1, path to v2)
- ✅ All demo6-8 features still work (feature flags, RBAC, choreography concepts understood)
- ✅ Production-ready saga patterns demonstrated

---

## Cross-Demo Pattern Dependencies

```
demo1 (Auth Foundation)
    ↓
demo2 (Passkeys + Diagnostics)
    ↓
demo3 (Permission-Based RBAC)
    ↓
demo4 (Entra ID + Multi-Identity)
    ↓
demo5 (OBO + Bearer Tokens)
    ↓
demo5.1 (Distributed Monolith + Aspire + YARP)
    ↓
demo6 (Finbuckle + Outbox/Inbox + Multi-Identity Toggle)
    ├─ (Feature Flags foundation)
    ├─ (Saga foundation)
    ↓
demo7 (Feature Flags + Hardening + Scope Validation)
    ├─ (Saga foundation)
    ↓
demo8 (Choreographed Sagas)
    ↓
demo9 (State Machine Sagas)
```

---

## Complexity Progression Summary

| Demo | Auth | AuthZ | API | Data | Messaging | Multi-Tenancy | Observability |
|------|------|-------|-----|------|-----------|---------------|---------------|
| 1    | ⭐   | -     | -   | -    | -         | -             | -             |
| 2    | ⭐⭐ | -     | -   | -    | -         | -             | ⭐            |
| 3    | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | -    | -         | -             | ⭐⭐          |
| 4    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | -    | -         | -             | ⭐⭐          |
| 5    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | -    | -         | -             | ⭐⭐          |
| 5.1  | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | -    | -         | -             | ⭐⭐⭐        |
| 6    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐  | ⭐⭐⭐        | ⭐⭐⭐        |
| 7    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐  | ⭐⭐⭐        | ⭐⭐⭐        |
| 8    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐        | ⭐⭐⭐        |
| 9    | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐        | ⭐⭐⭐⭐      |

---

## Success Metrics

### Per-Demo Validation

Each demo must meet:
1. **Functional:** All features work end-to-end
2. **Architectural:** Patterns implemented per spec
3. **Multi-Tenant:** Complete isolation verified
4. **Observable:** Aspire dashboard shows metrics + traces
5. **Backward Compatible:** All previous patterns still work

### Workshop-Level Success (All 9 Demos)

- ✅ 28 distinct patterns demonstrated
- ✅ Authentication: Local + Entra ID + Passkeys
- ✅ Authorization: Cookie + Bearer + RBAC + Scopes
- ✅ APIs: BFF + Downstream + YARP
- ✅ Data: Outbox + Inbox + Distributed
- ✅ Messaging: Choreography → Orchestration (demo8 → demo9)
- ✅ Multi-Tenancy: Complete isolation + per-tenant config
- ✅ Observability: Logs + Traces + Metrics + Dashboard
- ✅ All runnable locally with Aspire
- ✅ Production patterns clearly documented

---

## Next Steps

1. **Immediate:** Begin demo6 implementation (Finbuckle + Outbox/Inbox)
2. **Plan:** Detailed issue breakdown for each pattern
3. **Document:** `.docs/issues/` for each pattern with decision rationale
4. **Verify:** Validate each demo with checklist before moving to next
5. **Refine:** Gather feedback, update catalog + roadmap

---

**Document Status:** Planning Phase  
**Next Review:** After demo6 Milestone  
**Last Updated:** 2026-01-12
