# Demo6 – From BFF to Modular Monolith with Legacy Integration

## Goal

Transform the **Backend-for-Frontend (BFF)** architecture from demo5 into a **modular monolithic structure** using vertical slices. Demonstrate how to organize a growing application with **three complete service flows** that showcase different integration patterns (local database, legacy API, and modern API). This introductory approach to modular architecture prepares senior developers for maintaining large-scale enterprise applications where multiple domains and integration patterns coexist in a single deployment unit.

## Prerequisites

- **Completed:** demo4 (Entra ID + claims mapping) and demo5 (downstream API patterns)
- **.NET 10 SDK** (Preview) with EF Core tools installed
- **Visual Studio Code** or JetBrains Rider
- **Target Audience:** Senior full-stack developers familiar with monolithic and distributed architectures
- Understanding of vertical slicing and domain-driven design concepts (helpful but not required)

## Architecture Overview

### Three Complete Service Flows

Each service implements a full vertical slice from data source to UI:

**User Service Flow (Greenfield)**
```
SQL Database
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
LegacyOrderAdapter (wraps legacy API calls)
   ↓
OrderService (IOrderService) - maps legacy DTO → internal DTO
   ↓
/api/orders (Minimal API endpoints)
   ↓
Blazor Components (OrderList.razor, OrderDetail.razor)
   ↓
SSR + WASM Client
```

**Graph Service Flow (Modern API Integration)**
```
Microsoft Graph API
   ↓
IDownstreamApi (OBO token exchange)
   ↓
GraphService (IGraphService) - wraps Graph SDK calls
   ↓
/api/graph (Minimal API endpoints)
   ↓
Blazor Components (UserCalendar.razor, UserEmails.razor)
   ↓
SSR + WASM Client
```

**Key Difference from Demo5:**
- Demo5: Single-purpose BFF with one downstream API pattern
- Demo6: Multi-domain modular monolith with three integration patterns (local, legacy, modern)

### Architecture Diagram

```
┌────────────────────────────────────────────────────────────────┐
│  Blazor UI Layer (SSR + WASM)                                  │
│  ┌─────────────────────┬──────────────────┐                    │
│  │ UserProfile.razor   │ OrderList.razor  │                    │
│  │ UserList.razor      │ OrderDetail.razor│                    │
│  └─────────────────────┴──────────────────┘                    │
├────────────────────────────────────────────────────────────────┤
│  BFF API Layer (/api/users, /api/orders)                       │
│  (Minimal APIs with permission-based authorization)            │
├────────────────────────────────────────────────────────────────┤
│  Service Layer                                                 │
│  ┌──────────────────────────┬──────────────────────────┐       │
│  │ UserService              │ OrderService             │       │
│  │ (IUserService)           │ (IOrderService)          │       │
│  └──────────────────────────┴──────────────────────────┘       │
├────────────────────────────────────────────────────────────────┤
│  Adapter Layer (external services only)                        │
│  ┌──────────────────────────────────────────────────┐          │
│  │ LegacyOrderAdapter (wraps legacy HTTP API)       │          │
│  └──────────────────────────────────────────────────┘          │
├────────────────────────────────────────────────────────────────┤
│  Authentication & Authorization                                │
│  • PermissionClaimsTransformation (adds permission claims)     │
│  • PermissionAuthorizationHandler (enforces policies)          │
│  • Simple token forwarding to legacy service                   │
├────────────────────────────────────────────────────────────────┤
│  Data Access Layer                                             │
│  ┌──────────────────────┐      ┌──────────────────────────┐    │
│  │ EF Core DbContext    │      │ HttpClient to            │    │
│  │ (local database)     │      │ legacy service           │    │
│  └──────────────────────┘      └──────────────────────────┘    │
└────────────────────────────────────────────────────────────────┘
         ↓                            ↓
   ┌──────────────┐            ┌────────────────┐
   │ SQL Database │            │ Legacy Service │
   │              │            │ (port 7230)    │
   └──────────────┘            └────────────────┘
```

### Modular Monolithic Structure (Introductory Pattern)

Each service module is a **vertical slice** organized by business capability:

```
Demo6.LegacyIntegration/
├── Modules/
│   ├── Users/                   (Greenfield - Local Database)
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
│   ├── Orders/                  (Legacy Integration)
│   │   ├── Adapters/
│   │   │   └── LegacyOrderAdapter.cs
│   │   │       └── Wraps legacy HTTP API calls
│   │   ├── Services/
│   │   │   └── OrderService.cs (IOrderService)
│   │   │       └── Maps legacy DTO → internal DTO
│   │   ├── Api/
│   │   │   └── OrdersEndpoints.cs (/api/orders)
│   │   └── Components/
│   │       ├── OrderList.razor
│   │       └── OrderDetail.razor
│   │
│   └── Graph/                   (Modern API Integration - OBO Flow)
│       ├── Services/
│       │   └── GraphService.cs (IGraphService)
│       │       └── Calls IDownstreamApi with OBO tokens
│       ├── Api/
│       │   └── GraphEndpoints.cs (/api/graph/calendar, /api/graph/emails)
│       └── Components/
│           ├── UserCalendar.razor
│           └── UserEmails.razor
│
├── Shared/                      (Cross-Module Contracts)
│   ├── Models/
│   │   ├── Order.cs
│   │   ├── CalendarEvent.cs
│   │   ├── Email.cs
│   │   └── ApplicationUser.cs
│   └── Interfaces/
│       ├── IUserService.cs
│       ├── IOrderService.cs
│       └── IGraphService.cs
│
├── Authorization/               (Shared Infrastructure)
│   ├── PermissionClaimsTransformation.cs
│   └── PermissionAuthorizationHandler.cs
│
├── Data/                        (Shared Infrastructure)
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs
│
├── Program.cs                   (Module Registration & DI)
└── appsettings.json
```

**Key Principles (Simple Introduction):**
- **Vertical Slicing:** Each module owns its full stack (data → UI)
- **Shared Infrastructure:** Common concerns (auth, data) live outside modules
- **Module Independence:** Modules communicate via interfaces, not direct coupling
- **DI Registration:** Each module registers its services in `Program.cs`

**Not Covered in Demo6 (Advanced Topics for Future):**
- ❌ Module-level event buses
- ❌ Complex inter-module communication patterns
- ❌ Module-specific databases (separate schemas)
- ❌ Feature toggles per module

## What's New in Demo6

### Architectural Progression: From Monolith to Modular Monolith

**Demo4:** Introduced Entra ID integration + centralized claims transformation (identity patterns)

**Demo5:** Introduced downstream API pattern with OBO flow (API security patterns)

**Demo6:** Combines both patterns into a **modular monolithic structure** that shows how to organize a growing application:
```
Demo4: Authentication + Authorization
  ↓
Demo5: BFF + Downstream APIs
  ↓
Demo6: Modular Organization of Multiple Integration Patterns
```

### From Single-Pattern BFF to Multi-Pattern Modular Monolith

**Demo5 (Single Pattern):**
```
Blazor UI → BFF → Downstream Weather API (OBO flow)
└─ One domain (Weather), one integration pattern
```

**Demo6 (Multiple Patterns, Same App):**
```
Blazor UI → Modular BFF:
            ├── Users Module → Local SQL Database
            ├── Orders Module → Legacy HTTP API (new adapter pattern)
            └── Graph Module → Microsoft Graph (OBO from demo5)
└─ Three domains, three integration patterns, single deployment
```

**Key Achievement:** Demo6 shows that demo4's centralized claims (identity) and demo5's OBO flow (API pattern) scale naturally as the application grows. Auth and API patterns from earlier demos remain intact and functional.

### Three Service Implementations

#### 1. **User Service** (Greenfield/Local)

- **Data Source:** SQL Server database via EF Core
- **Service Layer:** `IUserService` with simple CRUD operations
- **API Layer:** `/api/users`, `/api/me` (Minimal APIs)
- **UI Components:** `UserProfile.razor`, `UserList.razor` (SSR + WASM)
- **Pattern:** Traditional local database access (baseline for comparison)

#### 2. **Order Service** (Legacy Integration) ← PRIMARY FOCUS

- **Data Source:** External legacy HTTP API (port 7230, simulated)
- **Adapter Layer:** `LegacyOrderAdapter` wraps legacy API calls
- **Service Layer:** `IOrderService` maps legacy DTOs to internal models
- **API Layer:** `/api/orders` (Minimal APIs)
- **UI Components:** `OrderList.razor`, `OrderDetail.razor` (SSR + WASM)
- **Pattern:** Adapter pattern for external service integration

#### 3. **Graph Service** (Modern API Integration)

- **Data Source:** Microsoft Graph API (Calendar, Mail, etc.)
- **Integration:** `IDownstreamApi` with OBO token exchange (from demo5)
- **Service Layer:** `IGraphService` wraps Graph SDK calls
- **API Layer:** `/api/graph/calendar`, `/api/graph/emails` (Minimal APIs)
- **UI Components:** `UserCalendar.razor`, `UserEmails.razor` (SSR + WASM)
- **Pattern:** OBO flow for delegated permissions (carries over from demo5)

### Key Features

#### Adapter Pattern for External Services

**Legacy Order Adapter**
```csharp
public interface ILegacyOrderAdapter
{
    Task<LegacyOrderResponse> GetOrderAsync(int orderId);
    Task<List<LegacyOrderResponse>> GetOrdersByCustomerAsync(string customerId);
}

public class LegacyOrderAdapter : ILegacyOrderAdapter
{
    private readonly HttpClient _httpClient;
    
    // Wraps legacy HTTP API calls
    // Handles legacy response formats and error codes
    // Returns raw legacy DTOs (transformation happens in service layer)
}
```

**Order Service (Maps Legacy to Internal Models)**
```csharp
public class OrderService : IOrderService
{
    private readonly ILegacyOrderAdapter _adapter;
    
    public async Task<Order> GetOrderAsync(int orderId)
    {
        // Call adapter
        var legacyOrder = await _adapter.GetOrderAsync(orderId);
        
        // Map legacy DTO → internal domain model
        return new Order
        {
            Id = legacyOrder.OrderId,
            CustomerName = legacyOrder.CustName, // Legacy uses abbreviated fields
            Status = MapLegacyStatus(legacyOrder.Stat),
            Total = legacyOrder.TotalAmt / 100m // Legacy stores cents
        };
    }
}
```

#### Simple Authentication Forwarding

For demo purposes, we use **simple token forwarding** to legacy services:

```csharp
// In LegacyOrderAdapter
public class LegacyOrderAdapter : ILegacyOrderAdapter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private async Task<HttpRequestMessage> CreateRequestAsync(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        
        // Forward user identity as simple header (demo only)
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            request.Headers.Add("X-User-Id", userId);
        }
        
        return request;
    }
}
```

**Production Note:** In production systems, use proper auth translation (OAuth2 token exchange, service accounts, or API gateway-managed auth). Complex `IAuthBridgeService` abstractions will be covered in advanced demos.

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

#### Modular Monolithic Architecture (Introduction)

Each service module follows vertical slicing principles:
- **Vertical Slicing:** Module owns its complete stack (data → service → API → UI)
- **Module Registration:** Extensions methods in `Program.cs` for clean DI setup
- **Cross-Module Communication:** Via shared interfaces in `Shared/` folder
- **Shared Infrastructure:** Common concerns (auth, database context) remain shared

**Module Registration Pattern**
```csharp
// Program.cs
builder.Services.AddUsersModule();   // Registers IUserService, endpoints, etc.
builder.Services.AddOrdersModule();  // Registers IOrderService, adapter, endpoints, etc.
builder.Services.AddGraphModule();   // Registers IGraphService, IDownstreamApi, endpoints
```

**Extension Method Example**
```csharp
// Modules/Orders/OrdersModule.cs
public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILegacyOrderAdapter, LegacyOrderAdapter>();
        
        // Register HttpClient for legacy API
        services.AddHttpClient<ILegacyOrderAdapter, LegacyOrderAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7230");
        });
        
        return services;
    }
    
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders").RequireAuthorization();
        
        group.MapGet("/", async (IOrderService orderService) =>
            await orderService.GetOrdersAsync());
        
        return endpoints;
    }
}
```

**Why This Pattern:**
- Clear module boundaries without complex infrastructure
- Easy to test (mock `ILegacyOrderAdapter` for `OrderService` tests)
- Prepares for potential microservices migration (each module is extraction-ready)
- Keeps it simple: no event buses, no separate databases, no feature flags (yet)

#### Simulated Legacy Service

A standalone project simulates the legacy order system:
- **Simulated.LegacyOrderService** (port 7230): Mimics 15-year-old HTTP API patterns
  - Non-RESTful endpoints (e.g., `/GetOrder?id=123`)
  - Quirky field names (e.g., `CustName`, `Stat`, `TotalAmt` in cents)
  - Inconsistent error handling
- Can be replaced with real legacy API by updating `appsettings.json`

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

3. **Terminal 2 – Start Main App:**
   ```bash
   cd demo6/Demo6.LegacyIntegration
   dotnet watch
   # Runs on https://localhost:7210
   ```

4. **Open Browser:**
   ```
   https://localhost:7210
   ```

5. **Test the Three Service Flows:**
   
   **User Service (Greenfield/Local):**
   - Log in with test credentials (e.g., `admin@local.app` / `Admin123!`)
   - Navigate to `/users` → View user list
   - Navigate to `/profile` → View/edit your profile
   - Verify data is stored locally in SQL database
   - **Key Observation:** Direct database access, fast response times
   
   **Order Service (Legacy Integration) ← PRIMARY FOCUS:**
   - Navigate to `/orders` → Load orders from legacy service
   - Click on an order → View order details
   - Observe the adapter translating legacy quirks:
     - Field name mapping (`CustName` → `CustomerName`)
     - Data transformation (cents → decimal)
     - Status code mapping
   - **Resilience Test:** Stop the legacy service (Terminal 1) → observe error handling
   - **Key Observation:** Adapter isolates legacy quirks from your domain model
   
   **Graph Service (Modern API Integration):**
   - Log in with Entra ID account (requires work/school account)
   - Navigate to `/calendar` → View your calendar events from Microsoft Graph
   - Navigate to `/emails` → View recent emails from your mailbox
   - Observe OBO flow in action (BFF exchanges user token for Graph token)
   - **Key Observation:** Same OBO pattern from demo5, now in modular context
   
   **Module Boundaries:**
   - Review `Modules/Users/`, `Modules/Orders/`, `Modules/Graph/` in code
   - Notice how each module has its own service, API, and components
   - Observe module registration in `Program.cs` (3 separate `.AddXxxModule()` calls)

## Key Learning Points

### Architecture Evolution

- ✅ **BFF → Modular Monolith:** Understand when and how to evolve a BFF into a modular structure
- ✅ **Vertical Slicing (Introductory):** Organize domains into independent modules with complete stacks
- ✅ **Module Boundaries:** Define clear boundaries without over-engineering
- ✅ **Three Integration Patterns:** Local DB + Legacy API + Modern API in one app

### Legacy Integration (PRIMARY FOCUS)

- ✅ **Adapter Pattern:** Isolate legacy API quirks from your domain model
- ✅ **DTO Mapping:** Transform incompatible schemas at service boundaries
- ✅ **Simple Auth Forwarding:** Basic authentication patterns for demo purposes

### Modern API Integration (Continuity from Demo5)

- ✅ **OBO Flow in Modular Context:** Microsoft Graph integration with delegated permissions
- ✅ **IDownstreamApi Pattern:** Reuse downstream API service from demo5
- ✅ **Enterprise Realism:** Calendar and email integration scenarios

### Enterprise Patterns

- ✅ **Multiple Data Sources:** Combine local databases with external service calls
- ✅ **Resilience (Basic):** Handle external service failures gracefully
- ✅ **Module Registration:** Clean DI setup with extension methods
- ✅ **Testability:** Design for unit testing with interface-based dependencies

### What's Deferred to Advanced Demos

- ⏭️ Complex auth translation (OAuth2 token exchange, service accounts)
- ⏭️ Inter-module event buses and messaging
- ⏭️ Module-specific databases (separate schemas/connections)
- ⏭️ Feature flags and gradual rollouts
- ⏭️ Full circuit breaker implementations (Polly patterns)
- ⏭️ Microservices decomposition strategies

## Files & Folders

| File/Folder                           | Purpose                                  |
| ------------------------------------- | ---------------------------------------- |
| `Demo6.LegacyIntegration/`            | Main app with modular structure          |
| `Demo6.LegacyIntegration.Client/`     | Blazor WASM client                       |
| `Demo6.LegacyIntegration.Shared/`     | Shared DTOs and interfaces               |
| `Simulated.LegacyOrderService/`       | Simulated legacy HTTP API (port 7230)    |

## Design Decisions for Demo6

**Kept Simple (For Introduction):**
- ✅ No MediatR/CQRS (keep service layer direct)
- ✅ Shared infrastructure (auth, DbContext) outside modules
- ✅ Basic error handling (no full Polly circuit breakers yet)
- ✅ Simple token forwarding (no complex auth bridge)

**Advanced Topics for Future Demos:**
- ⏭️ **Demo7+:** Microservices decomposition from this modular monolith
- ⏭️ **Demo8+:** Event-driven architecture with message buses
- ⏭️ **Demo9+:** Full observability stack (distributed tracing, metrics)
- ⏭️ **Demo10+:** Multi-tenancy and feature flags per module

## Related Demos

- **Demo3**: Permission-based RBAC (foundation)
- **Demo4**: Entra ID integration (modern auth)
- **Demo5**: Downstream APIs & OBO flow (inter-service calls)
- **Demo6**: Legacy integration & modular architecture ← **You are here**
