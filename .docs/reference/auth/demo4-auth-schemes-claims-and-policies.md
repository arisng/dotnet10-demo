# demo4 Auth Schemes, Claims, and Policies (Reference)

## Authentication schemes

demo4 runs with multiple authentication handlers:

- ASP.NET Core Identity application cookie (local users)
- Microsoft Entra ID OpenID Connect (external login)

The app’s effective user session is typically represented by the Identity application cookie.

## Key claims

### Entra markers

- `oid`: Entra Object ID
- `tid`: Entra Tenant ID
- `roles`: Entra app roles (if configured)

### App markers

- `permission`: application permission claims added by `IClaimsTransformation`
- `auth_provider`: optional durable marker of the chosen auth provider (`entra` / `local`)

## Policies

### `entra.user`

Meaning: “Authenticated Entra user”.

Requirements:

- authenticated
- has `oid`
- has `tid`

Used to protect Graph endpoints and Entra-only pages.

## SSR → WASM auth-state persistence

To support `InteractiveAuto`, auth state is persisted into `PersistentComponentState` and rehydrated in WASM.

- Server persists `UserInfo`
- Client reconstructs a `ClaimsPrincipal` from `UserInfo`

## Where to look

- Server DI + pipeline: `demo4/Demo4.EntraIntegration/Program.cs`
- Claims transformation: `demo4/Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs`
- Server persistence: `demo4/Demo4.EntraIntegration/Authorization/PersistingServerAuthenticationStateProvider.cs`
- Client rehydration: `demo4/Demo4.EntraIntegration.Client/Services/PersistentAuthenticationStateProvider.cs`
- Diagnostics UI: `demo4/Demo4.EntraIntegration.Client/Components/Pages/AuthStateProbe.razor`
