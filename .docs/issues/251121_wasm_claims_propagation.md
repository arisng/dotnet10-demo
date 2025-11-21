# Issue: Custom Claims Lost in Blazor WASM (InteractiveAuto)

## Problem Description

In a Blazor Web App using `InteractiveAuto` render mode, users may experience the following behavior:

1. **Login works**: The user logs in successfully.
2. **Prerendering works**: The initial server-rendered page shows authorized content because the server has access to all services (database, claims transformation).
3. **WASM Transition fails**: As soon as the WebAssembly runtime takes over (or if the user navigates to a WASM-only page), the user is treated as "Not Authorized" for specific policies, or redirected back to the login page.

## Root Cause

When Blazor transitions from Server (Prerendering) to Client (WASM), it must transfer the authentication state.

- By default, Blazor serializes the principal's basic identity data.
- **Crucially**, it does **not** automatically re-run server-side `IClaimsTransformation` or fetch custom permissions on the client.
- As a result, the client-side `AuthenticationState` has the user's Name and ID, but lacks the custom `permission` claims required by `[Authorize(Policy = "...")]`.

## Solution: Persistent Component State

To fix this, we must manually persist the custom claims from the Server to the Client using `PersistentComponentState`.

### 1. The Data Transfer Object (DTO)

Create a lightweight class to hold the necessary identity information, including the custom permissions.

```csharp
// Demo3.BffRbac.Client/Models/UserInfo.cs
public class UserInfo
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string[] Roles { get; set; }
    public required string[] Permissions { get; set; } // The missing piece
}
```

### 2. Server-Side: Persisting the State

Create a service that listens for the `OnPersisting` event. This service runs during the server-side rendering pass. It fetches the permissions and saves them into the HTML state.

```csharp
// Demo3.BffRbac/Components/Account/PersistingServerAuthenticationStateProvider.cs
public class PersistingServerAuthenticationStateProvider : IDisposable
{
    // ... dependencies (PersistentComponentState, IPermissionService) ...

    private async Task OnPersistingAsync()
    {
        var authState = await _authenticationStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // 1. Fetch permissions explicitly on the server
            var permissions = await _permissionService.GetPermissionsAsync(userId);

            // 2. Create DTO
            var userInfo = new UserInfo
            {
                UserId = userId,
                Email = email,
                Permissions = permissions.ToArray(),
                // ...
            };

            // 3. Persist to state
            _state.PersistAsJson("UserInfo", userInfo);
        }
    }
}
```

### 3. Client-Side: Hydrating the State

Create a custom `AuthenticationStateProvider` for the Client project. Instead of just calling an API, it first checks if `UserInfo` exists in the persisted state.

```csharp
// Demo3.BffRbac.Client/Services/PersistentAuthenticationStateProvider.cs
public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    // ...
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // 1. Try to read from persisted state (injected into HTML)
        if (!userInfoReceived && _state.TryTakeFromJson<UserInfo>("UserInfo", out var userInfo))
        {
            // 2. Reconstruct Principal with ALL claims (including permissions)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
                new Claim(ClaimTypes.Email, userInfo.Email),
                // Add permissions back as claims
            };
            claims.AddRange(userInfo.Permissions.Select(p => new Claim("permission", p)));

            var identity = new ClaimsIdentity(claims, "Bearer");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        // Fallback to default behavior (e.g., unauthenticated or fetch from API)
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
```

### 4. Registration

Finally, register these services in their respective `Program.cs` files.

**Server (`Program.cs`):**

```csharp
// Register the persisting provider as a standalone service
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();
```

**Server (`App.razor`):**

Inject the service to ensure it initializes and subscribes to the persistence event.

```razor
@inject PersistingServerAuthenticationStateProvider PersistingState
```

**Client (`Program.cs`):**

```csharp
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
```

## Outcome

With this pattern, the custom `permission` claims are calculated once on the server, serialized into the page HTML, and immediately available to the WASM client. This prevents the "flicker" of unauthorized state and ensures policy checks pass immediately.
