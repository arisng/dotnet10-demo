# State Machine Saga


**Introduced:** demo9 (Planned)  
**Category:** Messaging  
**Complexity:** ⭐⭐⭐⭐⭐ (Very Advanced)

**Definition:**
Orchestration pattern for long-running workflows. A saga state machine holds the workflow state and decides what happens next. More explicit than choreography but adds complexity.

**Use Case: Order Processing State Machine**
```
[Submitted]
    ↓ OrderPlaced
[AwaitingPayment]
    ├─ PaymentSucceeded → [ReadyToShip]
    └─ PaymentFailed → [Compensating] → [Failed]

[ReadyToShip]
    ↓ OrderShipped
[Completed]
```

**Implementation (MassTransit):**
- Define state machine with states, events, transitions
- Saga instance holds workflow data
- Events drive state transitions
- Saga decides what message to publish
- Saga handles timeouts and compensation

**Strengths:**
- ✅ Workflow is explicit and visible
- ✅ Centralized state tracking
- ✅ Easier debugging ("where are we stuck?")
- ✅ Timeouts + compensation explicit
- ✅ Observability dashboards possible

**Weaknesses:**
- ❌ More complex implementation
- ❌ State machine library required
- ❌ Latency from state persistence
- ❌ More operational overhead

**Related Patterns:**
- [Choreographed Saga](msg-choreographed-saga.md)
- [Outbox Pattern](data-outbox.md)

**Demo References:**
- demo9 (Planned): Order processing state machine

---

## Multi-Tenancy Patterns
