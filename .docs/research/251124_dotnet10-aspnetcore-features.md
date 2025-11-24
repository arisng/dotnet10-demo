# Research Summary: ASP.NET Core 10 New Features for Identity, Authentication, Blazor, Authorization, OpenAPI, and Cloud Native Patterns

## Master Todo List
- [x] Research Identity & Authentication new features in .NET 10 (Completed)
- [x] Research Blazor Authentication improvements in .NET 10 (Completed)
- [x] Research Authorization new middleware/policies in .NET 10 (Completed)
- [x] Research OpenAPI/Swagger interactions with Identity endpoints in .NET 10 (Completed)
- [x] Research Cloud Native/Microservices patterns for token handling or BFF in .NET 10 (Completed)

## Findings
- **Source:** Microsoft Docs search for "What's new in ASP.NET Core 10 Identity and Authentication"
- **Key Insights:** 
  - Added authentication and authorization metrics (e.g., authenticated request duration, challenge/sign in/sign out counts).
  - ASP.NET Core Identity metrics for user management, login/session handling, and two-factor authentication.
  - No cookie login redirects for known API endpoints (returns 401/403 instead of redirects for API controllers, minimal APIs with JSON, TypedResults, SignalR).
  - Passkeys support in ASP.NET Core Identity using WebAuthn/FIDO2 for passwordless authentication.
- **Recommendations:** Include demos on metrics integration with Aspire dashboard, API endpoint behavior changes, and passkey setup in Blazor Web Apps.

- **Source:** Microsoft Docs search for "Blazor authentication improvements in ASP.NET Core 10"
- **Key Insights:** 
  - Passkeys integrated into Blazor Web App template with out-of-the-box management and login.
  - Circuit state persistence for resuming sessions without losing work (browser throttling, mobile app switching, network interruptions).
  - Updated Blazor Web App security samples for OIDC, Entra, Windows Auth, including separate web API projects, token handlers, named HTTP clients, distributed token cache for web farms, and Azure Key Vault with Managed Identities.
  - Declarative [PersistentState] attribute for persisting component/service state during prerendering.
  - Authentication state serialization/deserialization for dual-mode (Auto) rendering.
- **Recommendations:** Workshop on dual-mode auth handoff, circuit persistence for auth state, and BFF patterns with updated samples.

- **Source:** Microsoft Docs search for "Authorization new features middleware policies ASP.NET Core 10"
- **Key Insights:** 
  - Authorization metrics added (count of requests requiring authorization).
  - No new middleware or policy providers; existing UseAuthorization middleware unchanged.
  - API endpoint changes affect authorization behavior (401/403 for API endpoints).
- **Recommendations:** Focus on metrics for authorization monitoring; note behavioral changes for API security.

- **Source:** Microsoft Docs search for "OpenAPI Swagger Identity endpoints ASP.NET Core 10"
- **Key Insights:** 
  - Endpoint-specific operation transformers for fine-grained OpenAPI customization of individual routes.
  - Microsoft.OpenApi upgraded to 2.0.0 (GA) with breaking changes (e.g., Metadata for ephemeral properties, HTTP method enums).
  - Schema generation enhancements: oneOf for nullable types, improved $ref resolution, property descriptions as siblings of $ref.
  - Applies to all endpoints, including Identity endpoints for better documentation.
- **Recommendations:** Demo OpenAPI transformers for customizing Identity endpoint docs; handle schema upgrades in workshops.

- **Source:** Microsoft Docs search for "Cloud Native Microservices token handling BFF ASP.NET Core 10"
- **Key Insights:** 
  - Updated BFF samples with OIDC and Entra using YARP or Duende Access Token Management for automatic token refresh.
  - Distributed token cache with encryption for web farms, Azure Key Vault integration.
  - Duende Access Token Management for transparent token lifetime management in Blazor apps.
  - JWT bearer authentication, IdentityServer patterns for microservices.
- **Recommendations:** Workshop on BFF with YARP/Duende for token handling in microservices; distributed cache setup.

## Workshop Roadmap Features
- **Identity & Authentication**: Passkeys in Blazor Web Apps; API endpoint 401/403 behavior; Identity metrics.
- **Blazor Authentication**: Dual-mode auth state serialization; circuit persistence; updated security samples with web APIs and token handlers.
- **Authorization**: Authorization metrics; API security changes.
- **OpenAPI/Swagger**: Operation transformers for Identity endpoints; schema enhancements.
- **Cloud Native/Microservices**: BFF patterns with YARP/Duende; distributed encrypted token cache; Azure Key Vault integration.