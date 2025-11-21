# demo3 – BFF APIs + Permission-Based RBAC

## Goal

Implement the Backend-for-Frontend (BFF) pattern with fine-grained permission-based authorization, establishing a production-ready security model before introducing external identity providers. This demo transforms the authentication foundation from demo2 into a complete authorization system where API endpoints explicitly declare required permissions, and users' access rights are determined by aggregating roles into granular permission claims.

## Prerequisites

- Completion of **demo2 – Dual-Mode Diagnostics + Passkeys** (reuse the complete passkey implementation and authentication infrastructure)
- .NET 10 SDK (Preview) with EF Core tools
- Understanding of claims-based authorization and policy-based patterns
- Familiarity with ASP.NET Core Minimal APIs
- Basic knowledge of WASM HttpClient patterns for calling server APIs

## Architecture Overview

**Pattern:** Monolithic Blazor Web App (Server + WASM + APIs + RBAC in single project)

**Why Monolithic for demo3?**
- True BFF pattern - APIs physically in same process as server
- Cookie authentication works seamlessly across all components
- No CORS configuration needed
- Simpler for learning authorization concepts
- Prepares for multi-identity scenarios (demo4+) where backend unifies auth handling

**Authorization Flow:**
```
User Signs In (Passkey/Password)
    ↓
IClaimsTransformation runs on each request
    ↓
Load User's Roles from database
    ↓
Aggregate all Permissions for those Roles
    ↓
Add permission claims to ClaimsPrincipal
    ↓
Authorization handlers check permission claims
    ↓
// ...existing code...
API endpoint grants/denies access
```

### Service Abstraction Pattern (New in Demo 3)

To solve the "Prerendering Dependency Injection" challenge (where `HttpClient` is not available during server-side prerendering), we use a Service Abstraction Pattern:

1. **Shared Interfaces**: `IWeatherService`, `IUserService`, etc., defined in the Client project.
2. **Client Implementation**: `ClientWeatherService` uses `HttpClient` to call the BFF APIs.
3. **Server Implementation**: `ServerWeatherService` accesses the Database/UserManager directly.
4. **Registration**:
   - **Client**: Registers `Client*` services.
   - **Server**: Registers `Server*` services.

This allows components to simply inject `IWeatherService` and work correctly in both environments (Server Prerendering and Client WASM) without code changes.

## How to Run
// ...existing code...
```

### Service Abstraction Pattern (New in Demo 3)

To solve the "Prerendering Dependency Injection" challenge (where `HttpClient` is not available during server-side prerendering), we use a Service Abstraction Pattern:

1.  **Shared Interfaces**: `IWeatherService`, `IUserService`, etc., defined in the Client project.
2.  **Client Implementation**: `ClientWeatherService` uses `HttpClient` to call the BFF APIs.
3.  **Server Implementation**: `ServerWeatherService` accesses the Database/UserManager directly.
4.  **Registration**:
    *   **Client**: Registers `Client*` services.
    *   **Server**: Registers `Server*` services.

This allows components to simply inject `IWeatherService` and work correctly in both environments (Server Prerendering and Client WASM) without code changes.

## How to Run

### Standard Development Mode

1. Copy the entire `demo2` folder to create `demo3` baseline:

```powershell
# From the dotnet10-demo directory
Copy-Item -Path demo2 -Destination demo3 -Recurse
cd demo3
```

2. Rename solution and projects (use your IDE's rename/refactor tools or manually):
   - Solution: `Demo2.DualModeHandoff.sln` → `Demo3.BffRbac.sln`
   - Server project: `Demo2.DualModeHandoff` → `Demo3.BffRbac`
   - Client project: `Demo2.DualModeHandoff.Client` → `Demo3.BffRbac.Client`
   - Update namespaces in all `.cs` and `.razor` files accordingly

3. Create and apply the new RBAC migration:

```powershell
cd Demo3.BffRbac/Demo3.BffRbac
dotnet ef migrations add AddRolePermissionSystem
dotnet ef database update
```

4. Launch with hot reload:

```powershell
dotnet watch
```

5. Visit `https://localhost:7210`, sign in with one of the seeded accounts:
   - **admin@local.app** / `Admin123!` (Admin role - all permissions)
   - **manager@local.app** / `Manager123!` (Manager role - subset of permissions)
   - **user@local.app** / `User123!` (User role - read-only permissions)

6. Test the permission system:
   - Navigate to **Auth State Probe** - see roles and permission claims
   - Try **Weather** page - calls `/api/weather` (requires `weather.read`)
   - Try **User Management** page - calls `/api/users` (requires `users.read`, delete requires `users.delete`)
   - Try **Reports** page - calls `/api/reports` (requires `reports.view`, export requires `reports.export`)
   - Observe 403 errors when trying operations your role doesn't permit

### Testing Authorization Behavior

**Test 401 (Unauthenticated):**
```powershell
curl https://localhost:7210/api/weather -k
# Expected: 401 Unauthorized (not login redirect - this is .NET 10's new behavior)
```

**Test 403 (Authenticated but Insufficient Permissions):**
- Sign in as `user@local.app`
- Try to delete a user via User Management UI
- Expected: 403 Forbidden with clear error message

**Test Permission Claim Inspection:**
- Sign in as any user
- Open Auth State Probe
- Verify `permission` claims appear in the timeline
- Confirm they match the user's role assignments

## What's New from demo2

### Database Schema Extensions

**New Tables:**
- `Roles` - Define logical groupings (Admin, Manager, User)
- `Permissions` - Granular actions (`weather.read`, `users.delete`, etc.)
- `RolePermissions` - Many-to-many junction table
- Updated `AspNetUserRoles` - Link users to roles

**Migration:** `AddRolePermissionSystem`

### Authorization Infrastructure (.NET 10 Best Practices)

#### 1. IClaimsTransformation Implementation
**File:** `Authorization/PermissionClaimsTransformation.cs`

Uses the standard .NET `IClaimsTransformation` interface (runs automatically on each request):

```csharp
public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IPermissionService _permissionService;
    
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var permissions = await _permissionService.GetUserPermissionsAsync(userId);
        
        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;
        
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }
        
        return clone;
    }
}
```

**Why IClaimsTransformation?**
- Runs automatically before authorization
- No custom middleware needed
- Standard .NET pattern, well-documented
- Testable and mockable

#### 2. Permission Service
**File:** `Services/PermissionService.cs`

```csharp
public interface IPermissionService
{
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
}

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        // 1. Get user's roles
        // 2. Get all permissions for those roles
        // 3. Return distinct permission names
    }
}
```

**Performance Considerations:**
- Use `.AsNoTracking()` for read-only queries
- Consider caching permissions (keyed by userId) for high-traffic scenarios
- Claims transformation runs once per request, permissions cached in `ClaimsPrincipal`

#### 3. Custom Authorization Requirement & Handler
**Files:** 
- `Authorization/PermissionRequirement.cs`
- `Authorization/PermissionAuthorizationHandler.cs`

```csharp
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) 
        => Permission = permission;
}

public class PermissionAuthorizationHandler 
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

#### 4. Extension Method for Clean API Declaration
**File:** `Authorization/AuthorizationExtensions.cs`

```csharp
public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder, string permission)
    {
        var requirement = new PermissionRequirement(permission);
        return builder.RequireAuthorization(
            new AuthorizationPolicyBuilder()
                .AddRequirements(requirement)
                .Build());
    }
}
```

**Usage in Program.cs:**
```csharp
app.MapGet("/api/weather", GetWeather)
   .RequirePermission("weather.read");
```

#### 5. Program.cs Registration (.NET 10 Fluent API)

```csharp
// Authorization services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

// Use .NET 10's AddAuthorizationBuilder() fluent API
builder.Services.AddAuthorizationBuilder();

// Register custom handlers
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

### Seed Data

**File:** `Data/DbSeeder.cs`

**Roles:**
- **Admin** - Full system access
- **Manager** - Operational management
- **User** - Read-only consumer

**Permissions (dot notation for grouping):**
- `weather.read` - View weather forecasts
- `weather.write` - Create/update forecasts
- `users.read` - List users
- `users.write` - Create/update users
- `users.delete` - Remove users
- `reports.view` - Access reports
- `reports.export` - Download report data

**Role → Permission Mappings:**
```
Admin:
  - weather.read, weather.write
  - users.read, users.write, users.delete
  - reports.view, reports.export

Manager:
  - weather.read, weather.write
  - users.read
  - reports.view, reports.export

User:
  - weather.read
  - reports.view
```

**Seeded Users:**
- `admin@local.app` / `Admin123!` (Admin role, passkey-enabled)
- `manager@local.app` / `Manager123!` (Manager role, passkey-enabled)
- `user@local.app` / `User123!` (User role, passkey-enabled)

### BFF API Endpoints (Minimal APIs)

**File:** `Program.cs` (after `app.MapAdditionalIdentityEndpoints()`)

```csharp
// Weather API
var weatherApi = app.MapGroup("/api/weather");

weatherApi.MapGet("/", GetWeatherForecast)
    .RequirePermission("weather.read");

weatherApi.MapPost("/", CreateWeatherForecast)
    .RequirePermission("weather.write");

// User Management API
var usersApi = app.MapGroup("/api/users");

usersApi.MapGet("/", GetUsers)
    .RequirePermission("users.read");

usersApi.MapPost("/", CreateUser)
    .RequirePermission("users.write");

usersApi.MapDelete("/{id}", DeleteUser)
    .RequirePermission("users.delete");

// Reports API
var reportsApi = app.MapGroup("/api/reports");

reportsApi.MapGet("/", GetReports)
    .RequirePermission("reports.view");

reportsApi.MapGet("/export", ExportReports)
    .RequirePermission("reports.export");
```

**Key Features:**
- Uses route groups (`MapGroup`) for clean organization
- Each endpoint explicitly declares required permission
- Cookie authentication inherited from server context
- No tokens exposed to client (true BFF pattern)

### WASM Components Consuming BFF APIs

**Files (in `.Client` project):**
- `Pages/Weather.razor` - Calls `/api/weather`
- `Pages/UserManagement.razor` - Calls `/api/users`
- `Pages/Reports.razor` - Calls `/api/reports`

**Pattern:**
```csharp
@page "/weather"
@rendermode InteractiveWebAssembly
@inject HttpClient Http

<PageTitle>Weather</PageTitle>

<h3>Weather Forecasts</h3>

@if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else if (error != null)
{
    <div class="alert alert-danger">
        <strong>Error:</strong> @error
    </div>
}
else
{
    <table class="table">
        <!-- render forecasts -->
    </table>
}

@code {
    private WeatherForecast[]? forecasts;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/api/weather");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            error = "You don't have permission to view weather forecasts.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            error = "You must sign in to view weather forecasts.";
        }
        catch (Exception ex)
        {
            error = $"Failed to load weather: {ex.Message}";
        }
    }
}
```

**Error Handling Strategy:**
- 401 Unauthorized - User not signed in (redirect to login)
- 403 Forbidden - User lacks required permission (show friendly error)
- 500 Server Error - System issue (show generic error with logging)

### UI Authorization with AuthorizeView

**Example in NavMenu.razor:**
```razor
@* Only show for users with users.read permission *@
<AuthorizeView Policy="RequirePermission" Resource="users.read">
    <Authorized>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="user-management">
                <span class="bi bi-people" aria-hidden="true"></span> User Management
            </NavLink>
        </div>
    </Authorized>
</AuthorizeView>
```

**Note:** For permission-based `AuthorizeView`, need custom policy provider or use roles/policies. Simpler approach for demo3:

```razor
<AuthorizeView Roles="Admin,Manager">
    <!-- UI for users with Admin or Manager role -->
</AuthorizeView>
```

Permission checks happen at API level (enforced), UI just hides/shows elements (UX).

### Enhanced Auth State Probe

**Updates to `AuthStateProbe.razor`:**
- Display user's assigned roles
- List all aggregated permission claims
- Show timeline of when claims transformation occurred
- Highlight permission claims distinct from Identity claims

**New Sections:**
```razor
<div class="card mt-3">
    <div class="card-header">Authorization Details</div>
    <div class="card-body">
        <h5>Roles</h5>
        <ul>
            @foreach (var role in GetRoles())
            {
                <li><code>@role</code></li>
            }
        </ul>
        
        <h5>Permissions</h5>
        <ul>
            @foreach (var permission in GetPermissions())
            {
                <li><code>@permission</code></li>
            }
        </ul>
    </div>
</div>
```

### .NET 10 Specific Features Demonstrated

#### 1. Automatic 401/403 for API Endpoints (New in .NET 10)
The cookie authentication handler now detects `IApiEndpointMetadata` on Minimal APIs and returns proper HTTP status codes instead of redirecting to login pages.

**What this means:**
- Your `/api/weather` endpoint automatically returns `401 Unauthorized` for unauthenticated requests
- No need for custom cookie event handlers
- More RESTful API behavior

**Verify:**
```powershell
curl https://localhost:7210/api/weather -k
# Response: 401 Unauthorized (not a redirect to /Account/Login)
```

#### 2. AddAuthorizationBuilder() Fluent API (New in .NET 10)
Cleaner policy registration:

```csharp
// Old (.NET 8 and earlier)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtLeast21", policy => 
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
});

// New (.NET 10)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AtLeast21", policy => 
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
```

Benefits: Better IntelliSense, chainable, more discoverable.

#### 3. Built-in Authorization Metrics (New in .NET 10)
Automatically available in `Microsoft.AspNetCore.Authorization` meter:
- `aspnetcore.authorization.requests_requiring_authorization` (counter)
- Tag: `policy_name`, `result` (success/failure)

**View in Aspire Dashboard or Application Insights:**
- Monitor which permissions are checked most frequently
- Identify authorization failures (potential security issues)
- Track performance of authorization handlers

**Enable:**
```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Microsoft.AspNetCore.Authorization");
    });
```

## Outcome

By the end of demo3, you will have:

✅ **Complete permission-based authorization system**
- Fine-grained control at action level (not just roles)
- Explicit permission requirements on every API endpoint
- Standard .NET patterns (`IClaimsTransformation`, `IAuthorizationHandler`)

✅ **Production-ready BFF architecture**
- APIs secured with cookie authentication
- WASM components consume APIs via HttpClient
- No tokens exposed to browser (secure by design)
- Proper error handling for 401/403 scenarios

✅ **.NET 10 best practices**
- Use `AddAuthorizationBuilder()` fluent API
- Leverage automatic 401/403 for API endpoints
- Built-in authorization metrics for observability
- `IClaimsTransformation` for claims augmentation

✅ **Foundation for multi-identity scenarios**
- Authorization logic is identity-source agnostic
- Role → Permission mapping supports any auth provider
- When Entra ID is added (demo4), it will inherit this permission system
- Clear separation: authentication (who you are) vs. authorization (what you can do)

✅ **Developer experience improvements**
- Seeded test users with different permission levels
- Enhanced diagnostics (Auth State Probe shows permissions)
- Clean API endpoint declarations (`RequirePermission("weather.read")`)
- Comprehensive error handling examples in WASM components

## Key Learning Outcomes

1. **Claims-Based Authorization Pattern**
   - Understand how claims represent permissions
   - Learn when to use roles vs. permissions
   - See how claims flow from database → ClaimsPrincipal → authorization handlers

2. **BFF Security Model**
   - Why cookies are more secure than tokens for WASM
   - How server APIs proxy authentication state
   - When to use BFF vs. token-based APIs

3. **Permission System Design**
   - Dot notation for permission naming (`resource.action`)
   - Role → Permission many-to-many relationship
   - Aggregation strategy for performance

4. **Authorization vs. Authentication**
   - Authentication: "Who are you?" (demo2 with passkeys)
   - Authorization: "What can you do?" (demo3 with permissions)
   - How they work together but remain independent

## Next Steps: Preparing for demo4

Demo4 will add **Microsoft Entra ID** as an external identity provider. The permission system you built in demo3 will remain unchanged - Entra users will simply be assigned roles (Admin/Manager/User) which map to the same permissions.

**What won't change:**
- Permission database schema
- `IClaimsTransformation` logic
- API endpoint declarations (`RequirePermission()`)
- Authorization handlers

**What will be added:**
- Entra ID configuration (`AddMicrosoftIdentityWebApp()`)
- "Sign in with Microsoft" button alongside passkey login
- Account linking (same email, different providers)
- Provider differentiation in Auth State Probe

This demonstrates the power of the permission-based approach: **authorization logic stays constant regardless of how users authenticate**.

## Troubleshooting

**Problem:** Permission claims not appearing in Auth State Probe
- **Solution:** Ensure `IClaimsTransformation` is registered as scoped, not singleton
- Verify `PermissionService.GetUserPermissionsAsync()` is returning data
- Check that user has role assignments in database

**Problem:** API returns 403 even though user has permission
- **Solution:** Verify permission name spelling matches exactly (case-sensitive)
- Check that `PermissionAuthorizationHandler` is registered in DI
- Ensure the requirement is added to the endpoint correctly

**Problem:** Seeded users not created after migration
- **Solution:** Run `DbSeeder` logic in `Program.cs` (call `await SeedDataAsync(app.Services)`)
- Check connection string is correct
- Verify migration was applied (`dotnet ef database update`)

**Problem:** WASM component can't call API (CORS error)
- **Solution:** Shouldn't happen in monolithic setup! Verify both projects are in same solution
- Check that `.Client` project is referenced by server project
- Ensure `AddInteractiveWebAssemblyRenderMode()` is called

## References

- [Policy-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
- [Claims-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0)
- [Minimal APIs authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0)
- [IClaimsTransformation interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.iclaimstransformation)
- [Backend for Frontend pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends)
- [What's new in ASP.NET Core 10 - Authorization metrics](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#authentication-and-authorization)
