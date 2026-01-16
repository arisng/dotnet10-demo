# Cross-Cutting Guidance

## Observability Across Patterns

All patterns benefit from:
- **Structured Logging:** Context + correlation IDs
- **OpenTelemetry:** Traces + metrics + logs
- **Health Checks:** Service readiness/liveness
- **Dashboards:** Real-time monitoring

## Security Considerations

Every pattern must address:
- **HTTPS Everywhere:** No HTTP in production
- **Token Rotation:** Refresh before expiration
- **Scope Validation:** Least-privilege principle
- **Audit Trails:** Log who did what when
- **CORS Security:** Explicit origin validation
- **PII Protection:** Scrub sensitive data from logs

## Performance & Scaling

Pattern implications:
- **Stateless > Stateful:** Easier to scale
- **Caching:** Token caching, permission caching, query caching
- **Async/Await:** Non-blocking I/O
- **Batching:** Reduce roundtrips
- **Monitoring:** Identify bottlenecks

Back to [Patterns Index](../index.md)
