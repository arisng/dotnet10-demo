# Message Ordering & Saga Patterns: First Principles Analysis

**Date:** 2025-01-12  
**Source:** [Solving Message Ordering from First Principles](https://www.milanjovanovic.tech/blog/solving-message-ordering-from-first-principles) by Milan Jovanović  
**Scope:** Architectural patterns for reliable, ordered message processing in distributed systems

---

## Executive Summary

Most systems don't need _global_ message ordering—they need **per-aggregate ordering**. The journey from Domain Events → Outbox → Competing Consumers reveals that ordering constraints naturally evolve into **choreographed sagas**, which mature into **state machine sagas** when control and observability become critical.

This document distills the first-principles reasoning and proposes integration into the .NET 10 progressive demo series.

---

## Problem Evolution

### Layer 1: Domain Events (Appealing but Brittle)

**The mental model:**
```
State Change → Event → Reaction
```

**Example flow:**
- `OrderPlaced` → `PaymentCaptured` → `OrderShipped`

**The problem:** Publishing is unreliable when coupled to the transaction:
- Transaction succeeds, publishing fails
- Publishing succeeds, transaction rolls back
- Consumers process duplicates
- Retries cause reordering

**Takeaway:** Domain events work for _internal_ consistency but fail for _external_ integration.

---

### Layer 2: The Outbox Pattern (Reliable, but Not Ordered)

**Solution:** Store outgoing events in the same transaction as the aggregate update, then publish asynchronously.

```
Aggregate Change + Outbox Entry (same transaction)
    ↓
Background Publisher (reads Outbox → pushes to Queue)
    ↓
Message Queue (unreliable consumers waiting)
```

**Benefits:**
- If transaction commits, event is persisted
- If publisher crashes, it resumes later
- Retries are safe

**Limitation:** Doesn't guarantee _ordered handling_. The Outbox makes publishing reliable, but a queue just sits there waiting for consumers—it doesn't enforce sequence.

---

### Layer 3: Competing Consumers (Throughput, but Order Lost)

**Naive scaling:** Multiple instances consume from the same queue to increase throughput.

**The bug:** Two events for the same `OrderId` can be processed concurrently:
- Consumer A: `PaymentCaptured`
- Consumer B: `OrderPlaced`
- Side effects run backward

Even with perfect publishing order, retries and redelivery scramble processing.

**The realization:** _Queues scale work. They don't preserve your invariants._

---

### Layer 4: Per-Aggregate Ordering (The Real Requirement)

**Key insight:** You don't need one ordered line for everything. You need many independent ordered lines—one per aggregate.

Why this works:
- **Aggregates already define consistency boundaries** (DDD principle)
- **Events are naturally produced in order** (v1, v2, v3…)
- **The "correct" order is the aggregate's own timeline**

**Simplest solution:** Use a single consumer for the whole stream.
- ✅ Enforces ordering
- ❌ Throughput ceiling (artificial bottleneck)
- ❌ Latency spikes under load
- ❌ Scaling becomes vertical, not horizontal

---

### Layer 5: Publish the Next Message From the Handler (Workflow Birth)

**Insight:** Don't let the queue decide what's next—_we_ decide.

**New flow:**
```
Handle Message for Aggregate A
    ↓
When Done, Publish the Next Message for Aggregate A
    ↓
That message becomes the next item to process
```

**What just happened:** You've stopped building "event handlers." You've started building a **workflow**.

And that workflow is a **choreographed saga**.

---

### Layer 6: Choreographed Saga (Distributed, Sequential)

A **choreographed saga** is a workflow where:
- Each step reacts to an event
- Performs work
- Emits the next event to trigger the next step
- No single central coordinator

**Example: Order fulfillment saga**
```
Order Placed Event
    ↓ (Saga Step 1: Reserve Inventory)
Inventory Reserved Event
    ↓ (Saga Step 2: Charge Payment)
Payment Charged Event
    ↓ (Saga Step 3: Ship Order)
Order Shipped Event
```

**Benefits:**
- Per-aggregate ordering is preserved (the chain is sequential)
- Horizontal scaling across aggregates (many chains in flight)
- Each step is isolated and retryable
- Natural expression of distributed workflows

**Limitation:** Control is distributed, making progress tracking and exception handling messy.

---

### Layer 7: State Machine Saga (Centralized Control & Observability)

When workflows become business-critical, choreography shows limits. You want:
- A single place that knows the current state
- Visibility into progress ("where are we stuck?")
- Explicit timeouts and retries
- Compensating actions when something fails

**State machine saga approach:**
- The saga holds the workflow state (e.g., `OrderProcessing` state machine)
- Events drive state transitions
- The saga decides what message to publish next
- You gain observability and control

**Example state diagram:**
```
[Pending] --OrderPlaced--> [AwaitingPayment]
    ↓
[AwaitingPayment] --PaymentFailed--> [Failed]
    ↓
[AwaitingPayment] --PaymentSucceeded--> [ReadyToShip]
    ↓
[ReadyToShip] --OrderShipped--> [Completed]
```

---

## Broker Support & Trade-offs

Modern message brokers offer per-aggregate ordering primitives (not full ordering):

| Broker                | Feature                | How It Works                                     |
| --------------------- | ---------------------- | ------------------------------------------------ |
| **Amazon SQS**        | FIFO Message Groups    | `MessageGroupId` = Aggregate ID → ordered stream |
| **Azure Service Bus** | Sessions               | Session ID = Aggregate ID → ordered processing   |
| **Kafka**             | Partitions             | Key → Partition → ordered log                    |
| **RabbitMQ**          | Single Active Consumer | Only one consumer per queue at a time            |

**Important:** Broker-level ordering is a foundation, not a complete solution.

You **still need**:
- **Outbox** for reliable publishing (ordering is useless if events are lost)
- **Idempotent consumers / Inbox** for retries and duplicates
- **Consistency boundaries** (what's safe inside the transaction vs. outside)
- **Timeouts + compensation** for partial failures

---

## Key Takeaways

Following the problem from first principles:

1. **Aggregates define the boundary** where ordering matters
2. **Outbox makes publishing reliable** (but not ordered)
3. **Competing consumers break per-aggregate order** (accidental complexity)
4. **Single consumer restores order but caps throughput** (artificial bottleneck)
5. **Publishing "the next message" creates sequential progress** (workflow birth)
6. **Sequential progress per aggregate is a saga** (choreographed → state machine)
7. **You didn't reinvent something by accident—you discovered a necessary pattern**

The conclusion: _Ordered handling per aggregate at scale is not a queue feature. It's a workflow. And sagas are how we model workflows in distributed systems._

---

## Integration Roadmap for .NET 10 Progressive Demos

### Proposed Demo Sequence

#### **demo6** – Multi-Tenant SaaS (Current Target)
- Foundation: Finbuckle + Multi-Identity + Data Isolation
- **Extension:** Introduce Outbox pattern for tenant-scoped events
  - `TenantEvent` table with `TenantId`, `AggregateId`, `EventType`, `Payload`
  - Background publisher reads tenant-scoped events, publishes to broker
  - Idempotent consumer tracking (Inbox table)
  - Example: "Account Created" → "Welcome Email Queued" → "Notification Sent"

#### **demo7** – Feature Flags & Hardening (Current Target)
- Foundation: Feature flags + Azure AppConfig + Observability
- **Extension:** Introduce choreographed saga for feature-gated workflows
  - Example: "Premium Report Generation" saga
    - Step 1: User requests premium report (permission check via feature flag)
    - Step 2: Queue report generation job (emit `ReportGenerationRequested`)
    - Step 3: Generate report (emit `ReportGenerated`)
    - Step 4: Notify user (emit `ReportNotificationSent`)
  - Use Azure Service Bus Sessions (per-tenant) or Kafka (per-aggregate)
  - Track saga state in `ReportGenerationSaga` state machine

#### **demo8** (Future) – Choreographed Sagas (New)
- **Goal:** Implement end-to-end choreographed saga with MassTransit
  - Real-world workflow: Multi-step order processing
    - `OrderPlaced` → (reserve inventory) → `InventoryReserved` → (charge card) → `PaymentCaptured` → (ship) → `OrderShipped`
  - No central orchestrator; each step publishes the next event
  - Horizontal scaling per-aggregate (many orders processing concurrently)
  - Compensation logic for failures (e.g., refund if shipping fails)
  - Distributed tracing to visualize saga progress

#### **demo9** (Future) – State Machine Sagas (New)
- **Goal:** Mature choreography into orchestrated workflows with MassTransit Sagas
  - Saga state machine: `OrderProcessingSaga`
    - Defines states: `Submitted` → `PaymentProcessing` → `Shipping` → `Completed`
    - Handles timeouts: "If payment doesn't arrive in 5 minutes, compensate"
    - Visibility dashboard: "Where are stuck orders?"
  - Multi-tenant saga isolation (each tenant's sagas are independent)
  - Correlation ID tracking across all events
  - Observability: OpenTelemetry metrics for saga duration, failure rates

---

## Implementation Patterns (Unified Across Demos)

### Pattern 1: Outbox (Starting Point – demo6+)

```csharp
// Data Model
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; }    // e.g., "Order", "User"
    public Guid AggregateId { get; set; }        // Aggregate ID for ordering
    public Guid? TenantId { get; set; }          // Multi-tenant scoping (demo6+)
    public string EventType { get; set; }        // e.g., "OrderPlaced"
    public string Payload { get; set; }          // JSON-serialized event
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class InboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime ProcessedAt { get; set; }
    // Idempotency key: prevents duplicate processing
}

// Usage in aggregate
var order = new Order(orderId, customerId);
order.PlaceOrder(amount);

// Emit event + persist in same transaction
dbContext.Orders.Add(order);
var @event = new OutboxEvent
{
    AggregateType = "Order",
    AggregateId = orderId,
    TenantId = tenantId,
    EventType = nameof(OrderPlaced),
    Payload = JsonSerializer.Serialize(new { orderId, amount, timestamp = DateTime.UtcNow })
};
dbContext.OutboxEvents.Add(@event);
await dbContext.SaveChangesAsync();

// Background service (polling-based)
public class OutboxPublisher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var events = await dbContext.OutboxEvents
                .Where(e => e.PublishedAt == null)
                .OrderBy(e => e.CreatedAt)  // Preserve order
                .Take(100)
                .ToListAsync(ct);

            foreach (var @event in events)
            {
                await messageBroker.PublishAsync(@event);
                @event.PublishedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
```

### Pattern 2: Choreographed Saga (demo8+)

```csharp
// Events form a chain
public record OrderPlaced(Guid OrderId, decimal Amount);
public record InventoryReserved(Guid OrderId, Guid OrderItemId);
public record PaymentCaptured(Guid OrderId);
public record OrderShipped(Guid OrderId);

// Handler: Publish the next message
public class OrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced @event)
    {
        // Do work
        var reserved = await inventory.ReserveAsync(@event.OrderId);
        
        // Publish the next step (the saga continues)
        await messageBroker.PublishAsync(new InventoryReserved(
            @event.OrderId,
            reserved.ItemId
        ));
    }
}

public class InventoryReservedHandler : IMessageHandler<InventoryReserved>
{
    public async Task HandleAsync(InventoryReserved @event)
    {
        var order = await orders.GetAsync(@event.OrderId);
        var captured = await payments.CaptureAsync(order.Amount);
        
        await messageBroker.PublishAsync(new PaymentCaptured(@event.OrderId));
    }
}

// Broker ensures per-aggregate ordering
// Azure Service Bus: Session ID = OrderId
// Kafka: Partition Key = OrderId
```

### Pattern 3: State Machine Saga (demo9+)

```csharp
// Saga State Machine (MassTransit example)
public class OrderProcessingSaga : MassTransitStateMachine<OrderProcessingState>
{
    public State Submitted { get; private set; }
    public State PaymentProcessing { get; private set; }
    public State Shipping { get; private set; }
    public State Completed { get; private set; }

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; }
    public Event<PaymentCaptured> PaymentCapturedEvent { get; private set; }
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; }

    public OrderProcessingSaga()
    {
        InstanceState(x => x.CurrentState);
        
        // State: Submitted
        During(Submitted,
            When(OrderPlacedEvent)
                .Then(context => {
                    var order = context.Data;
                    context.Instance.OrderId = order.OrderId;
                    context.Instance.Amount = order.Amount;
                })
                .TransitionTo(PaymentProcessing)
                .Publish(context => new CapturePayment(context.Instance.OrderId, context.Instance.Amount))
        );

        // State: PaymentProcessing
        During(PaymentProcessing,
            When(PaymentCapturedEvent)
                .TransitionTo(Shipping)
                .Publish(context => new ShipOrder(context.Instance.OrderId)),
            
            When(PaymentFailedEvent)
                .TransitionTo(Completed)
                .Finalize()
        );

        // State: Shipping
        During(Shipping,
            When(OrderShippedEvent)
                .TransitionTo(Completed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}

// State persistence
public class OrderProcessingState
{
    public Guid CorrelationId { get; set; }  // Links events to saga instance
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

---

## Trade-offs & Design Considerations

### When to Use Each Pattern

| Pattern                             | When                                    | Cost      | Benefit                              |
| ----------------------------------- | --------------------------------------- | --------- | ------------------------------------ |
| **Domain Events only**              | Single service, no integration          | Low       | Simple mental model                  |
| **Outbox**                          | Reliable publishing, but single service | Medium    | Durability, no lost events           |
| **Outbox + Competing Consumers**    | High throughput, weak ordering needs    | Medium    | Throughput (but risky)               |
| **Outbox + Per-Aggregate Ordering** | Multi-step workflows per aggregate      | High      | Reliability + ordered processing     |
| **Choreographed Saga**              | Simple workflows, distributed decision  | High      | Natural expression, scale            |
| **State Machine Saga**              | Complex workflows, business visibility  | Very High | Control, observability, compensation |

### Common Pitfalls

1. **Publishing Directly from Transaction**
   - ❌ Tempting for simplicity, but unreliable
   - ✅ Always use Outbox

2. **Assuming Broker Ordering is Enough**
   - ❌ Per-aggregate ordering alone doesn't handle retries, duplicates, or compensation
   - ✅ Combine with Idempotent Inbox + Saga state tracking

3. **Choreographed Saga Without Monitoring**
   - ❌ Hard to track "where are we in the workflow?"
   - ✅ Use correlation IDs + structured logging + OpenTelemetry

4. **Premature State Machine**
   - ❌ Orchestration has overhead; choreography may suffice
   - ✅ Start choreographed, migrate to state machine when control is needed

---

## Recommended Next Actions

1. **For demo6 (Multi-Tenant SaaS):**
   - Add Outbox + Inbox tables to support tenant-scoped events
   - Implement background `OutboxPublisher` service (Aspire-managed)
   - Document tenant event isolation strategy in `.docs/research/`

2. **For demo7 (Feature Flags & Hardening):**
   - Use feature flags to gate saga enablement per tenant
   - Introduce simple choreographed workflow (e.g., report generation)
   - Integrate OpenTelemetry for saga lifecycle tracking

3. **Plan demo8 & demo9:**
   - Scope: Full choreographed → state machine saga evolution
   - Library choice: MassTransit (mature, .NET-idiomatic)
   - Real example: Order fulfillment with multi-step processing
   - Compensation logic (e.g., refund if any step fails)

---

## References

- **Primary:** [Solving Message Ordering from First Principles](https://www.milanjovanovic.tech/blog/solving-message-ordering-from-first-principles)
- **Saga Patterns:**
  - [Implementing the Saga Pattern with MassTransit](https://www.milanjovanovic.tech/blog/implementing-the-saga-pattern-with-masstransit)
  - [Orchestration vs. Choreography](https://www.milanjovanovic.tech/blog/orchestration-vs-choreography)
- **Domain Events:**
  - [How to Use Domain Events to Build Loosely Coupled Systems](https://www.milanjovanovic.tech/blog/how-to-use-domain-events-to-build-loosely-coupled-systems)
- **Idempotency:**
  - [Idempotent Consumer Handling Duplicate Messages](https://www.milanjovanovic.tech/blog/idempotent-consumer-handling-duplicate-messages)
- **Broker-Level Support:**
  - [Amazon SQS FIFO Message Groups](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/using-messagegroupid-property.html)
  - [Azure Service Bus Sessions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions)
  - [Apache Kafka Partitions](https://developer.confluent.io/courses/apache-kafka/partitions/)
