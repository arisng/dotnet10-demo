# Inbox Pattern


**Introduced:** demo6 (Planned)  
**Category:** Data Persistence / Messaging  
**Complexity:** ⭐⭐⭐ (Advanced)

**Definition:**
Idempotency pattern where message consumers record consumed messages in a database table. On retry, consumers check if message was already processed, preventing duplicate processing.

**Problem Solved:**
- Message broker retries can cause duplicate processing
- Idempotent consumers prevent duplicate side effects

**Implementation:**
```
Message Received
    ↓
Check Inbox: message ID already processed?
    ├─ Yes → Return cached result
    └─ No → Process + Record in Inbox
```

**Data Model:**
```csharp
public class InboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

**Strengths:**
- ✅ Handles retries gracefully
- ✅ Prevents duplicate processing
- ✅ Guaranteed exactly-once semantics
- ✅ Audit trail of processed events

**Weaknesses:**
- ❌ Requires database per consumer
- ❌ Adds processing latency
- ❌ Cleanup of old records needed

**Related Patterns:**
- [Outbox Pattern](data-outbox.md)
- Choreographed Saga

**Demo References:**
- demo6: Inbox for idempotent event handlers
- demo7+: Saga event processing

