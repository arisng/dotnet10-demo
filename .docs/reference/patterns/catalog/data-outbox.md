# Outbox Pattern


**Introduced:** demo6 (Planned)  
**Category:** Data Persistence / Messaging  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Reliable event publishing pattern where outgoing events are stored in the same database transaction as aggregate state changes. A background process then publishes events to a message broker, ensuring atomicity.

**Problem Solved:**
- Publishing directly to broker can fail (network issue)
- Transaction commits but publishing fails → lost events
- Events lost = data inconsistency

**Implementation:**
```
Aggregate State Change
    + Outbox Entry
    ↓ (same transaction)
Database Commit
    ↓
Background Publisher
    ↓
Message Broker
```

**Use Cases:**
- Reliable domain event publishing
- Event-driven architectures
- Eventually consistent systems
- Multi-service coordination

**Data Model:**
```csharp
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

**Strengths:**
- ✅ Guaranteed event persistence
- ✅ Atomic with business state
- ✅ Natural ordering per aggregate
- ✅ No external dependencies during write

**Weaknesses:**
- ❌ Adds database table + polling overhead
- ❌ Eventually consistent (not immediate)
- ❌ Requires idempotent consumers

**Related Patterns:**
- [Inbox Pattern](data-inbox.md)
- Choreographed Saga

**Demo References:**
- demo6: Outbox for tenant-scoped events
- demo7+: Foundation for saga patterns

