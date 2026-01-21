# Demo 4.1 - A Refined Implementation Plan for Demo 4

This is a comprehensive Implementation Specification for building a **Blazor Web App (Interactive Auto)** secured with **Microsoft Entra ID**, utilizing the **BFF (Backend for Frontend)** pattern with **YARP** and **.NET Aspire**.

This spec assumes a greenfield solution and dictates the exact steps required to implement the Dual-Flow (SSR + WASM) architecture.

---

## Phase 1: Azure / Entra ID Infrastructure

*Goal: Obtain the necessary Client IDs, Tenant IDs, and Scopes.*

### 1.1 Register Backend API

1. **Create App Registration:** Name: `SaaS.BackendApi`.
2. **Expose an API:**

* Set Application ID URI (accept default, e.g., `api://<backend-client-id>`).
* Add a Scope: Name `Weather.Get`, Admin consent display name "Read Weather Data".

1. **Record Values:**

* `Backend_ClientId`: (From Overview)
* `Backend_TenantId`: (From Overview)
* `Backend_Scope`: `api://<backend-client-id>/Weather.Get`

### 1.2 Register Frontend Client (BFF)

1. **Create App Registration:** Name: `SaaS.BlazorClient`.
2. **Authentication:**

* Platform: **Web**.
* Redirect URIs:
* `https://localhost:7001/signin-oidc` (Port will be defined in Aspire later).
* `https://localhost:7001/signout-callback-oidc`.

* Front-channel logout URL: `https://localhost:7001/signout-callback-oidc`.
* **Implicit grant:** UNCHECK everything (We use PKCE/Code flow).

1. **API Permissions:**

* Add Permission -> My APIs -> `SaaS.BackendApi` -> Select `Weather.Get`.
* Click "Grant admin consent" (Optional but recommended for dev).

1. **Certificates & secrets:**

* Create New Client Secret.

1. **Record Values:**

* `Frontend_ClientId`
* `Frontend_ClientSecret`
* `Frontend_TenantId`

---

## Phase 2: Solution & Project Scaffolding

*Goal: Create the structure managed by Aspire.*

1. **Create Solution:** `SaaS.WeatherApp`
2. **Add Projects:**

* `SaaS.AppHost` (Aspire Orchestrator)
* `SaaS.ServiceDefaults` (Aspire Defaults)
* `SaaS.Backend` (ASP.NET Core Web API)
* `SaaS.Frontend` (Blazor Web App) -> **Select "Interactive Auto"**, **"Per Page/Component"**, **"Include Sample Pages"**.
* *Note:* This creates `SaaS.Frontend` (Server) and `SaaS.Frontend.Client` (WASM).

1. **Add NuGets:**

* **SaaS.Backend:** `Microsoft.AspNetCore.Authentication.JwtBearer`
* **SaaS.Frontend (Server):** `Microsoft.Identity.Web`, `Microsoft.Identity.Web.DownstreamApi`, `Yarp.ReverseProxy`
* **SaaS.Frontend.Client (WASM):** `Microsoft.AspNetCore.Components.WebAssembly.Authentication`

---

## Phase 3: The Backend Implementation (`SaaS.Backend`)

*Goal: Secure the API endpoint.*

### 3.1 `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Aspire

// 1. Configure JWT Bearer
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.Audience = builder.Configuration["AzureAd:ClientId"]; 
        options.MapInboundClaims = false; 
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

// 2. Secure Endpoint
app.MapGet("/weather-forecast", () => 
{
    // ... return data ...
})
.RequireAuthorization(); // Enforce Token

app.Run();

```

### 3.2 `appsettings.json`

```json
"AzureAd": {
  "TenantId": "<Backend_TenantId>",
  "ClientId": "api://<backend-client-id>" 
}

```

---

## Phase 4: Shared Logic

*Goal: Define the contract.*

1. Create a folder/namespace accessible to both Frontend projects (or a shared Class Library).
2. **Interface:** `IWeatherForecaster.cs`

```csharp
public interface IWeatherForecaster
{
    Task<IEnumerable<WeatherForecast>> GetWeatherAsync();
}

```

1. **Model:** `WeatherForecast.cs`

---

## Phase 5: The Frontend Server / BFF Implementation (`SaaS.Frontend`)

*Goal: Handle Auth, SSR Data Fetching, and YARP Proxying.*

### 5.1 `appsettings.json`

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "contoso.onmicrosoft.com", // Your Directory Domain
  "TenantId": "<Frontend_TenantId>",
  "ClientId": "<Frontend_ClientId>",
  "ClientSecret": "<Frontend_ClientSecret>",
  "CallbackPath": "/signin-oidc"
},
"DownstreamApis": {
  "WeatherApi": {
    "BaseUrl": "https+http://weatherapi", 
    "Scopes": "api://<backend-client-id>/Weather.Get" 
  }
}

```

*Note: `BaseUrl` value `weatherapi` refers to the Aspire resource name.*

### 5.2 Implement SSR Service (`ServerWeatherForecaster.cs`)

```csharp
using Microsoft.Identity.Web;

public class ServerWeatherForecaster(IDownstreamApi downstreamApi) : IWeatherForecaster
{
    public async Task<IEnumerable<WeatherForecast>> GetWeatherAsync()
    {
        // DIRECT CALL: Server -> Backend API
        // "WeatherApi" matches appsettings config
        return await downstreamApi.CallApiForUserAsync<IEnumerable<WeatherForecast>>("WeatherApi");
    }
}

```

### 5.3 Implement State Persistence (`PersistingAuthenticationStateProvider.cs`)

*This prevents the "Flicker" when switching to WASM.*

```csharp
// Inherit from AuthenticationStateProvider (or hook into it)
// See Microsoft Docs for "PersistingAuthenticationStateProvider" full class boilerplate.
// Key logic inside OnPersistingAsync:

private async Task OnPersistingAsync()
{
    var authState = await _authenticationStateTask;
    var user = authState.User;

    if (user.Identity?.IsAuthenticated == true)
    {
        // Extract basic claims
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(ClaimTypes.Name)?.Value;
        
        // Persist to the page
        _state.PersistAsJson("UserInfo", new UserInfo(userId, email));
    }
}

```

### 5.4 `Program.cs` (Server) Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Aspire

// 1. Add Entra ID Auth & Downstream API support
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("WeatherApi", builder.Configuration.GetSection("DownstreamApis:WeatherApi"))
    .AddInMemoryTokenCaches(); // Use Redis in Prod

// 2. Add YARP
builder.Services.AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters()) // Define helper methods below
    .AddTransforms(context =>
    {
        // TRANSFORM: Inject Access Token for WASM requests
        if (context.Route.RouteId == "weather-proxy")
        {
            context.AddRequestTransform(async transformContext =>
            {
                var tokenAcquisition = transformContext.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
                // Get token for the defined scope
                var token = await tokenAcquisition.GetAccessTokenForUserAsync(["api://<backend-client-id>/Weather.Get"]);
                transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });
        }
    });

// 3. Register SSR Service Implementation
builder.Services.AddScoped<IWeatherForecaster, ServerWeatherForecaster>();

// 4. Register Persisting State Provider (Boilerplate)
// builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>(); 

var app = builder.Build();

// ... Middleware ...
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy(); // Activate YARP

// ... Blazor Hub ...
app.Run();

// 5. YARP Helpers
IReadOnlyList<RouteConfig> GetRoutes() => [
    new RouteConfig {
        RouteId = "weather-proxy",
        ClusterId = "weather-cluster",
        Match = new RouteMatch { Path = "/api/proxy/weather/{**catch-all}" } // Distinct path for proxy
    }
];

IReadOnlyList<ClusterConfig> GetClusters() => [
    new ClusterConfig {
        ClusterId = "weather-cluster",
        Destinations = new Dictionary<string, DestinationConfig> {
            { "backend", new DestinationConfig { Address = "https+http://weatherapi" } }
        }
    }
];

```

---

## Phase 6: The Frontend Client / WASM Implementation (`SaaS.Frontend.Client`)

*Goal: Handle Interactive Auto logic and Local Proxy calls.*

### 6.1 Implement WASM Service (`ClientWeatherForecaster.cs`)

```csharp
public class ClientWeatherForecaster(HttpClient httpClient) : IWeatherForecaster
{
    public async Task<IEnumerable<WeatherForecast>> GetWeatherAsync()
    {
        // PROXY CALL: Browser -> Blazor Server (YARP) -> Backend API
        // Note: The path matches the YARP Route Match in Server Program.cs
        return await httpClient.GetFromJsonAsync<IEnumerable<WeatherForecast>>("/api/proxy/weather/weather-forecast");
    }
}

```

### 6.2 `Program.cs` (Client)

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. Register Auth State Deserialization (Receives the "Backpack")
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// 2. Register Client Service Implementation
builder.Services.AddScoped<IWeatherForecaster, ClientWeatherForecaster>();

// 3. Configure HttpClient to talk to the HOST (The Blazor Server)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

```

---

## Phase 7: Orchestration & Component Integration

*Goal: Tie it together in the UI.*

### 7.1 `Weather.razor` (In `SaaS.Frontend.Client` or Shared)

```razor
@page "/weather"
@attribute [Authorize]
@inject IWeatherForecaster WeatherForecaster
@rendermode InteractiveAuto

<h3>Weather</h3>

@if (forecasts == null)
{
    <p>Loading...</p>
}
else
{
    <!-- Render Grid -->
}

@code {
    private IEnumerable<WeatherForecast>? forecasts;

    protected override async Task OnInitializedAsync()
    {
        // This line runs on Server first (SSR), then Client (WASM)
        // It magically swaps implementations based on where it runs.
        forecasts = await WeatherForecaster.GetWeatherAsync();
    }
}

```

### 7.2 `SaaS.AppHost` (Program.cs)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 1. Define Backend
var weatherApi = builder.AddProject<Projects.SaaS_Backend>("weatherapi");

// 2. Define Frontend and Inject Backend URL
builder.AddProject<Projects.SaaS_Frontend>("frontend")
    .WithReference(weatherApi) // Allows service discovery
    .WithExternalHttpEndpoints(); // Public access

builder.Build().Run();

```

---

## Phase 8: Execution Flow Verification

1. **Launch via Aspire:** Run `SaaS.AppHost`.
2. **Open Frontend:** Navigate to `/weather`.
3. **Authentication:** Redirects to Microsoft Login. Login success. Redirect back.
4. **SSR Check:** The page loads immediately with weather data. (Evidence: Network tab shows HTML document contains table rows).

* *System Action:* `ServerWeatherForecaster` used `IDownstreamApi` to fetch data.

1. **WASM Transition:** Browser downloads .NET runtime background.
2. **Interactive Check:** Click a "Refresh" button (add one to test).

* *System Action:* `ClientWeatherForecaster` fires `GET /api/proxy/weather/...` -> YARP intercepts -> Adds Token -> Forwards to Backend.

This specification covers the complete lifecycle. You can now copy the file names and logic directly into your IDE.
