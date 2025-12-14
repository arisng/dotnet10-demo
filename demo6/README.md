# Demo6 – Legacy Integration with Modular Monolithic Architecture

## Goal

Build a **greenfield ASP.NET Core 10 application** showcasing **three complete service flows** from UI to data access, each implemented as an independent modular vertical slice:

1. **User Service** (Greenfield/Local) - Managed entirely in the modern app
2. **Order Service** (Legacy Integration) - Consumes a simulated 15+ year-old legacy HTTP API
3. **Report Service** (Existing Integration) - Consumes a simulated 3-year-old REST API

Each service includes a complete data-to-UI flow: Data Source → Adapter/Wrapper → Service Layer → BFF API → Blazor UI (SSR + WASM). Demonstrates modular architecture, adapter patterns, multi-service orchestration, and unified authentication across diverse backend systems.

## Prerequisites

- **Completed:** demo5 (Downstream API & OBO flow fundamentals)
- **.NET 10 SDK** (Preview) with EF Core tools installed
- **Visual Studio Code** or JetBrains Rider
- Basic understanding of SOAP, REST APIs, and authentication schemes
- (Optional) SoapUI or Postman for testing external service calls

## Architecture Overview

### Three Complete Service Flows

Each service implements the full vertical slice: Data Source → Adapter → Service → API → UI

**User Service Flow (Greenfield)**
```
Database
   ↓
EF Core DbContext
   ↓
UserService (IUserService)
   ↓
/api/users (Minimal API endpoints)
   ↓
Blazor Components (UserProfile.razor, UserList.razor)
   ↓
SSR + WASM Client
```

**Order Service Flow (Legacy Integration)**
```
Legacy HTTP API (port 7230)
   ↓
LegacyOrderServiceAdapter (IOrderServiceAdapter)
   ↓
OrderService (IOrderService) - maps legacy DTO → internal DTO
   ↓
/api/orders (Minimal API endpoints)
   ↓
Blazor Components (OrderList.razor, OrderDetail.razor)
   ↓
SSR + WASM Client
```

**Report Service Flow (Existing Integration)**
```
Existing REST API (port 7240)
   ↓
ExistingAnalyticsServiceAdapter (IReportServiceAdapter)
   ↓
ReportService (IReportService) - maps v2 schema → v3 schema
   ↓
/api/reports (Minimal API endpoints)
   ↓
Blazor Components (ReportDashboard.razor, ReportsList.razor)
   ↓
SSR + WASM Client
```

### Architecture Diagram

```
┌────────────────────────────────────────────────────────────────┐
│  Blazor UI Layer (SSR + WASM)                                  │
│  ┌─────────────────────┬──────────────────┬──────────────────┐ │
│  │ UserProfile.razor   │ OrderList.razor  │ ReportDash.razor │ │
│  └─────────────────────┴──────────────────┴──────────────────┘ │
├────────────────────────────────────────────────────────────────┤
│  BFF API Layer (/api/users, /api/orders, /api/reports)         │
│  (Minimal APIs with permission-based authorization)            │
├────────────────────────────────────────────────────────────────┤
│  Service Layer                                                 │
│  ┌──────────────────┬──────────────────┬──────────────────┐    │
│  │ UserService      │ OrderService     │ ReportService    │    │
│  │ (IUserService)   │ (IOrderService)  │ (IReportService) │    │
│  └──────────────────┴──────────────────┴──────────────────┘    │
├────────────────────────────────────────────────────────────────┤
│  Adapter/Gateway Layer (only for external services)            │
│  ┌──────────────────────────────┬──────────────────────────┐   │
│  │ OrderServiceAdapter          │ ReportServiceAdapter     │   │
│  │ (wraps legacy API)           │ (wraps existing API)     │   │
│  └──────────────────────────────┴──────────────────────────┘   │
├────────────────────────────────────────────────────────────────┤
│  Authentication & Authorization                                │
│  • PermissionClaimsTransformation (adds permission claims)     │
│  • PermissionAuthorizationHandler (enforces policies)          │
│  • AuthBridgeService (maps auth to legacy/existing schemes)    │
├────────────────────────────────────────────────────────────────┤
│  Data Access Layer                                             │
│  ┌──────────────────────┐      ┌──────────────────────────┐    │
│  │ EF Core DbContext    │      │ HTTP Clients to          │    │
│  │ (local database)     │      │ external services        │    │
│  └──────────────────────┘      └──────────────────────────┘    │
└────────────────────────────────────────────────────────────────┘
         ↓                  ↓                    ↓
   ┌──────────────┐    ┌────────────────┐  ┌──────────────────┐
   │ SQL Database │    │ Legacy Service │  │ Existing Service │
   │              │    │ (port 7230)    │  │ (port 7240)      │
   └──────────────┘    └────────────────┘  └──────────────────┘
```

### Modular Monolithic Structure

Each service module is a **complete vertical slice** with data, service, API, and UI layers:

```
Demo6.LegacyIntegration/
├── Modules/
│   ├── Users/ (Greenfield Service - fully local)
│   │   ├── Data/
│   │   │   ├── ApplicationUser.cs
│   │   │   └── UserRole.cs
│   │   ├── Services/
│   │   │   └── UserService.cs (IUserService)
│   │   ├── Api/
│   │   │   └── UsersEndpoints.cs (/api/users, /api/me)
│   │   └── Components/
│   │       ├── UserProfile.razor
│   │       └── UserList.razor
│   │
│   ├── Orders/ (Legacy Integration Service)
│   │   ├── Adapters/
│   │   │   └── LegacyOrderServiceAdapter.cs (IOrderServiceAdapter)
│   │   ├── Services/
│   │   │   └── OrderService.cs (IOrderService)
│   │   │       └── calls adapter → transforms legacy DTO → internal DTO
│   │   ├── Api/
│   │   │   └── OrdersEndpoints.cs (/api/orders)
│   │   └── Components/
│   │       ├── OrderList.razor
│   │       └── OrderDetail.razor
│   │
│   └── Reports/ (Existing Integration Service)
│       ├── Adapters/
│       │   └── ExistingAnalyticsServiceAdapter.cs (IReportServiceAdapter)
│       ├── Services/
│       │   └── ReportService.cs (IReportService)
│       │       └── calls adapter → maps v2 schema → v3 schema
│       ├── Api/
│       │   └── ReportsEndpoints.cs (/api/reports)
│       └── Components/
│           ├── ReportDashboard.razor
│           └── ReportsList.razor
│
├── Shared/
│   ├── Models/
│   │   ├── Order.cs
│   │   ├── Report.cs
│   │   └── ApplicationUser.cs
│   └── Interfaces/
│       ├── IUserService.cs
│       ├── IOrderService.cs
│       └── IReportService.cs
│
├── Authorization/
│   ├── PermissionClaimsTransformation.cs
│   └── PermissionAuthorizationHandler.cs
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs
│
├── Program.cs
└── appsettings.json
```

## What's New in Demo6

### Three Complete Service Implementations

Unlike previous demos which focus on single architectures, demo6 implements **three end-to-end services** with different data sources:

#### 1. **User Service** (Greenfield/Local)

- **Data Source:** SQL Server database (EF Core)
- **Service Layer:** `IUserService` with business logic
- **API Layer:** `/api/users`, `/api/me` (Minimal APIs)
- **UI Components:** `UserProfile.razor`, `UserList.razor` (SSR + WASM)
- **No Adapter Needed:** Direct database access
- **Auth:** Local passkeys + Entra ID sync

#### 2. **Order Service** (Legacy Integration)

- **Data Source:** External legacy HTTP API (port 7230)
- **Adapter Layer:** `LegacyOrderServiceAdapter` (wraps quirky legacy API)
- **Service Layer:** `IOrderService` (translates legacy DTO → internal DTO)
- **API Layer:** `/api/orders` (Minimal APIs)
- **UI Components:** `OrderList.razor`, `OrderDetail.razor` (SSR + WASM)
- **Resilience:** Circuit breakers, retries, error handling

#### 3. **Report Service** (Existing Integration)

- **Data Source:** External 3-year-old REST API (port 7240)
- **Adapter Layer:** `ExistingAnalyticsServiceAdapter` (API translator)
- **Service Layer:** `IReportService` (maps v2 schema → v3 schema)
- **API Layer:** `/api/reports` (Minimal APIs)
- **UI Components:** `ReportDashboard.razor`, `ReportsList.razor` (SSR + WASM)
- **Resilience:** Circuit breakers, API key management, graceful degradation

### Key Features

#### Adapter Pattern for External Services

**Legacy Order Service Adapter**
```csharp
public interface ILegacyOrderServiceAdapter
{
    Task<OrderDto> GetOrderAsync(int orderId);
    Task<List<OrderDto>> GetOrdersByCustomerAsync(string customerId);
    Task<bool> CreateOrderAsync(CreateOrderRequest request);
}

public class LegacyOrderServiceAdapter : ILegacyOrderServiceAdapter
{
    // Wraps legacy HTTP API calls
    // Translates legacy responses to modern DTOs
    // Handles legacy error codes and quirky response formats
}
```

**Existing Analytics Service Adapter**
```csharp
public interface IExistingAnalyticsServiceAdapter
{
    Task<ReportDto> GetReportAsync(string reportId);
    Task<List<ReportDto>> QueryReportsAsync(ReportFilterDto filter);
}

public class ExistingAnalyticsServiceAdapter : IExistingAnalyticsServiceAdapter
{
    // Calls 3-year-old REST API
    // Maps v2 API schema to v3 internal schema
    // Handles API key authentication
}
```

#### Authentication Bridge Service

Converts between modern authentication (Entra ID + passkeys) and legacy auth schemes:

```csharp
public interface IAuthBridgeService
{
    Task<string> GetLegacyServiceTokenAsync(ClaimsPrincipal user);
    Task<string> GetExistingServiceApiKeyAsync(ClaimsPrincipal user);
    Task<BasicAuthCredentials> TranslateToBasicAuthAsync(ClaimsPrincipal user);
}
```

**Scenarios:**
- Modern app uses Entra ID token → bridge converts to Legacy Service's basic auth
- Modern app uses passkey cookie → bridge generates API key for Existing Service
- Cross-service impersonation / service-to-service calls with bearer tokens

#### Full Blazor UI Support (SSR + WASM)

All three services have complete UI coverage:
- **Server-Side Rendering (SSR):** Pre-rendered HTML for fast initial load and SEO
- **WebAssembly (WASM):** Interactive components after initial load
- **Interactivity:** Seamless handoff between SSR and WASM
- **Routable Pages:** Each service has dedicated pages with data loading

```csharp
// Example: OrderList component works in both SSR and WASM
@page "/orders"
@rendermode InteractiveAuto
@inject IOrderService OrderService
@inject AuthenticationStateProvider AuthenticationStateProvider

<h2>Orders</h2>
@if (orders != null)
{
    @foreach (var order in orders)
    {
        <OrderCard Order="order" />
    }
}

@code {
    private List<OrderDto> orders;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        orders = await OrderService.GetOrdersForUserAsync(authState.User);
    }
}
```

#### Modular Monolithic Architecture

Each service module is independent:
- **Vertical Slicing:** Module owns data, service, API, and UI layers
- **Dependency Injection per Module:** Modules register in `Program.cs`
- **Cross-Module Communication:** Via interfaces, not direct coupling
- **Testability:** Each module can be unit tested with mocked external services

**Module Registration**
```csharp
// Program.cs
builder.Services.AddUsersModule();
builder.Services.AddOrdersModule();
builder.Services.AddReportsModule();
```

#### Simulated External Services

Both legacy and existing services are simulated (built-in for demo):
- **Simulated.LegacyOrderService** (port 7230): Fake legacy HTTP API with quirky patterns
- **Simulated.ExistingAnalyticsService** (port 7240): Fake 3-year-old REST API
- Can be replaced with real services by updating `appsettings.json`

#### Error Handling & Resilience

- **Polly Circuit Breakers:** Graceful degradation when external services fail
- **Retry Policies:** Exponential backoff for transient errors
- **Fallback Strategies:** Return cached data or degraded responses
- **Logging & Observability:** Track calls to external systems

```csharp
services.AddHttpClient<ILegacyOrderServiceAdapter, LegacyOrderServiceAdapter>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(orderServiceUrl))
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)))
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(100) }));
```

#### Data Consistency & Mapping

Demonstrates strategies for data consistency across system boundaries:
- **DTO Mapping:** Convert between legacy/existing schemas and internal domain models
- **Data Validation:** Validate external data before persisting locally
- **Audit Trail:** Log all calls to external systems
- **Eventual Consistency:** Accept that external systems may be eventually consistent

## Seeded Test Users

Same as demo5:
| Email             | Password    | Role    |
| ----------------- | ----------- | ------- |
| admin@local.app   | Admin123!   | Admin   |
| manager@local.app | Manager123! | Manager |
| user@local.app    | User123!    | User    |

## How to Run

1. **Prerequisites Setup:**
   ```bash
   cd demo6
   dotnet ef database update
   ```

2. **Terminal 1 – Start Simulated Legacy Service:**
   ```bash
   cd demo6/Simulated.LegacyOrderService
   dotnet watch
   # Runs on https://localhost:7230
   ```

3. **Terminal 2 – Start Simulated Existing Service:**
   ```bash
   cd demo6/Simulated.ExistingAnalyticsService
   dotnet watch
   # Runs on https://localhost:7240
   ```

4. **Terminal 3 – Start Main App:**
   ```bash
   cd demo6/Demo6.LegacyIntegration
   dotnet watch
   # Runs on https://localhost:7210
   ```

5. **Open Browser:**
   ```
   https://localhost:7210
   ```

6. **Test the Three Service Flows:**
   - **User Service (Greenfield):**
     - Log in with passkey or Entra ID
     - Navigate to Users page → View user profile
     - Verify data is stored locally in SQL database
   
   - **Order Service (Legacy Integration):**
     - Navigate to Orders page → Load orders from legacy service (port 7230)
     - Adapter translates legacy HTTP responses to internal DTOs
     - Verify error handling if legacy service is down
   
   - **Report Service (Existing Integration):**
     - Navigate to Reports page → Load reports from existing service (port 7240)
     - Adapter maps v2 API schema to v3 internal schema
     - Verify error handling and fallback behavior
   
   - **Full UI Flow:**
     - Both SSR (server-side rendering) and WASM work for all services
     - Prerendering works (components can be SSR'd)
     - Interactivity switches to WASM seamlessly

## Key Learning Points

- ✅ **Three Complete Service Flows:** Understand how to implement greenfield, legacy, and existing service integrations end-to-end
- ✅ **Vertical Slice Modules:** Organize monolithic apps into independent feature modules with data-to-UI layers
- ✅ **Adapter Pattern:** Wrap and translate incompatible legacy/existing APIs into clean internal contracts
- ✅ **Auth Bridge:** Converting between different authentication schemes (passkeys → basic auth, cookies → API keys, etc.)
- ✅ **Blazor SSR + WASM:** Full UI support for both server-side rendering and interactive WebAssembly
- ✅ **Data Mapping & Transformation:** Converting between incompatible schemas across service boundaries
- ✅ **Resilience Patterns:** Circuit breakers, retries, fallbacks for unreliable external systems
- ✅ **Testing & Mocking:** Unit testing modules with mocked external services
- ✅ **Real-World Enterprise Scenarios:** Bridging legacy, existing, and greenfield systems in production
- ✅ **Modular Monolithic Best Practices:** Balancing maintainability with simplicity

## Files & Folders

| File/Folder                           | Purpose                                  |
| ------------------------------------- | ---------------------------------------- |
| `Demo6.LegacyIntegration/`            | Main greenfield app (modular monolithic) |
| `Demo6.LegacyIntegration.Client/`     | Blazor WASM client                       |
| `Demo6.LegacyIntegration.Shared/`     | Shared DTOs and interfaces               |
| `Simulated.LegacyOrderService/`       | Fake 15-year-old SOAP service            |
| `Simulated.ExistingAnalyticsService/` | Fake 3-year-old REST service             |

## Unresolved Questions & Future Work

- [ ] Should we use MediatR for command/query handling across modules?
- [ ] How deep should vertical slicing go? Complete isolation or shared infrastructure?
- [ ] What level of circuit breaker / resilience is practical for demo?
- [ ] Should we demonstrate service mesh concepts or keep it simple?
- [ ] How to handle cross-cutting concerns (logging, tracing) across module boundaries?
- [ ] Should demo7 extend this with actual microservices decomposition?

## Related Demos

- **Demo3**: Permission-based RBAC (foundation)
- **Demo4**: Entra ID integration (modern auth)
- **Demo5**: Downstream APIs & OBO flow (inter-service calls)
- **Demo6**: Legacy integration & modular architecture ← **You are here**
