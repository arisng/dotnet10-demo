# Research Summary: Recommended .NET 10 Features for Roadmap Updates

## Master Todo List

- [x] Research OpenAPI & Operation Transformers (Completed)
- [x] Research .NET Aspire Orchestration (Completed)
- [x] Research HybridCache for Permissions (Completed)
- [x] Research Testing with Auth Fixtures (Completed)

## Findings

- **Source:** Microsoft Docs search for "ASP.NET Core 10 OpenAPI operation transformers"
- **Key Insights:** Operation transformers enable fine-grained customization of OpenAPI documents for individual routes. You can add security requirements, descriptions, or modify schemas per endpoint. For Identity endpoints, this allows documenting RBAC permissions in Swagger UI dynamically. Schema enhancements include oneOf for nullable types and improved $ref resolution. Microsoft.OpenApi 2.0.0 GA brings breaking changes like new HTTP method enums.
- **Recommendations:** Add to Demo 3: Use transformers to annotate BFF endpoints with required permissions in the OpenAPI spec, making API docs self-documenting for RBAC.

- **Source:** Microsoft Docs search for "ASP.NET Core 10 .NET Aspire orchestration BFF microservices"
- **Key Insights:** .NET Aspire uses IDistributedApplicationBuilder to orchestrate services, providing service discovery and the Aspire Dashboard for metrics and traces. It's ideal for microservices setups like Demo 5, replacing manual port management. Integrates with auth metrics for monitoring.
- **Recommendations:** Refactor Demo 5 to use Aspire for orchestration, allowing the dashboard to display auth metrics and traces out-of-the-box.

- **Source:** Microsoft Docs search for "ASP.NET Core 10 HybridCache permissions caching"
- **Key Insights:** HybridCache (from .NET 9, refined in 10) provides efficient caching with stampede protection via GetOrCreateAsync. Perfect for caching RBAC permissions or Entra ID claims to reduce DB load. Supports distributed caches for scalability.
- **Recommendations:** Update Demo 4/6: Replace IMemoryCache in PermissionService with HybridCache for better performance in permission lookups.

- **Source:** Microsoft Docs search for "ASP.NET Core 10 WebApplicationFactory authentication testing"
- **Key Insights:** WebApplicationFactory can be customized with test auth handlers to simulate authenticated users. Create a TestAuthHandler that injects claims programmatically, bypassing real login flows. Enables integration testing of auth-protected APIs.
- **Recommendations:** Add Demo 8: Focus on testing auth flows, using WebApplicationFactory with custom handlers to test RBAC and API security without UI interactions.

## Implementation Strategy

- **OpenAPI**: Integrate into Demo 3 for better API documentation.
- **Aspire**: Core for Demo 5 to demonstrate cloud-native orchestration.
- **HybridCache**: Performance enhancement for Demo 7.
- **Testing**: New demo for comprehensive testing strategy.
