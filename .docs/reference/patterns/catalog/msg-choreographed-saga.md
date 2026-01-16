# Choreographed Saga


**Introduced:** demo8 (Planned)  
**Category:** Messaging  
**Complexity:** ⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Distributed transaction pattern where workflow steps are coordinated through events. Each step processes an event and publishes the next event in sequence. No central coordinator; workflow logic is distributed.

**Use Case: Order Fulfillment**
```
OrderPlaced Event
    ↓ (Inventory Handler)
InventoryReserved Event
    ↓ (Payment Handler)
PaymentCaptured Event
    ↓ (Shipping Handler)
OrderShipped Event
```

**Per-Aggregate Ordering:**
- All events for same OrderId processed sequentially
- Different orders process in parallel (scalability)
- Achieves "ordered per aggregate" without bottleneck

**Implementation:**
- Message broker with per-aggregate ordering (Sessions, Partitions, FIFO Groups)
- Each handler publishes the next event
- Outbox + Inbox for reliability + idempotency
- Compensation for failure scenarios

**Strengths:**
- ✅ Per-aggregate ordering with horizontal scale
- ✅ Natural workflow expression
- ✅ Decoupled handlers
- ✅ Automatic failure recovery

**Weaknesses:**
- ❌ Workflow implicit (hard to visualize)
- ❌ Complex debugging
- ❌ Eventual consistency
- ❌ Compensation logic needed

**Related Patterns:**
- [Outbox Pattern](data-outbox.md)
- [Inbox Pattern](data-inbox.md)
- [State Machine Saga](msg-state-machine-saga.md)

**Demo References:**
- demo8 (Planned): Full choreographed saga with order processing

