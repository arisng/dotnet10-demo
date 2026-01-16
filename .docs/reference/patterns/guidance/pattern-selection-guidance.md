# Pattern Selection Guidance

## Choosing Authentication

| Scenario | Pattern | Why |
|----------|---------|-----|
| Simple web app, single frontend | Cookie | Low overhead, simple model |
| High security requirement | Passkey | Phishing-resistant, modern |
| Enterprise/federated | OIDC | Centralized identity, compliance |
| Multiple auth sources | Multi-Identity | Flexibility, gradual migration |
| API for external clients | Bearer Token | Stateless, reusable |

## Choosing Authorization

| Scenario | Pattern | Why |
|----------|---------|-----|
| 3-5 roles, simple hierarchy | RBAC | Simple to understand and implement |
| 20-500 permissions, business rules | Permission-Based RBAC | Scalable, auditable, flexible |
| Complex conditional logic | Authorization Handlers | Custom rules, testable |
| API access control | OAuth Scopes | Clear contract, user consent |
| Multi-tenant, per-tenant rules | Multi-Tenant + RBAC | Tenant isolation + fine-grained control |

## Choosing API Architecture

| Scenario | Pattern | Why |
|----------|---------|-----|
| Monolithic, single frontend | BFF | Tight coupling OK, simpler security |
| Microservices, multiple clients | Downstream API | Loose coupling, reusable APIs |
| Distributed monolith, one BFF | BFF + YARP | Clean separation, transparent routing |
| Legacy systems, gradual migration | BFF + Downstream | Hybrid approach, flexible integration |

## Choosing Data Patterns

| Scenario | Pattern | Why |
|----------|---------|-----|
| Events must be published reliably | Outbox | Atomic with state, guaranteed delivery |
| Consumers might retry messages | Inbox | Idempotent processing, no duplicates |
| Simple eventual consistency | Choreographed Saga | Distributed workflow, no coordinator |
| Complex workflows, visibility needed | State Machine Saga | Central state, observability, control |

Back to [Patterns Index](../index.md)
