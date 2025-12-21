# Demo 4.1 (Refined) - Entra + BFF (YARP) + Interactive Auto + Aspire

Source spec: `demo4.1/.docs/251221-demo4-refined-implementation-plan.md`

## Projects

- `demo4.1/SaaS.WeatherApp.sln`
- Orchestrator: `demo4.1/SaaS.AppHost`
- Backend API: `demo4.1/SaaS.Backend` (`GET /weather-forecast`, requires `Weather.Get` scope)
- Frontend BFF: `demo4.1/SaaS.Frontend/SaaS.Frontend` (OIDC + token acquisition + YARP proxy)
- Frontend WASM: `demo4.1/SaaS.Frontend/SaaS.Frontend.Client` (calls `/api/proxy/weather/...`)

## Configure Entra (complete)

This demo uses two app registrations:

- Backend API: `SaaS.BackendApi` (exposes delegated scope `Weather.Get`)
- Frontend BFF: `SaaS.BlazorClient` (web app using auth code + PKCE, acquires user token, proxies via YARP)

If you change the frontend port from `https://localhost:7001`, update the redirect URIs accordingly (see `demo4.1/SaaS.Frontend/SaaS.Frontend/Properties/launchSettings.json`).

### 1) Register the Backend API (`SaaS.BackendApi`)

Microsoft Entra admin center -> Identity -> Applications -> App registrations -> New registration

- Name: `SaaS.BackendApi`
- Supported account types: Single tenant (recommended for this demo)
- Redirect URI: none

After creation, record:
- `Backend_TenantId` = Directory (tenant) ID
- `Backend_ClientId` = Application (client) ID

Expose an API:
- App registration -> Expose an API
- Application ID URI: accept the default (should look like `api://<Backend_ClientId>`)
- Add a scope:
  - Scope name: `Weather.Get`
  - Who can consent: Admins only (recommended for dev consistency)
  - Admin consent display name: `Read Weather Data`
  - Admin consent description: `Read Weather Data`
  - State: Enabled

Record:
- `Backend_Audience` (for `AzureAd:Audience`) = `api://<Backend_ClientId>`
- `Backend_Scope` = `api://<Backend_ClientId>/Weather.Get`

### 2) Register the Frontend BFF (`SaaS.BlazorClient`)

App registrations -> New registration

- Name: `SaaS.BlazorClient`
- Supported account types: Single tenant (recommended for this demo)
- Redirect URI: can be blank initially

After creation, record:
- `Frontend_TenantId` = Directory (tenant) ID
- `Frontend_ClientId` = Application (client) ID

Authentication:
- App registration -> Authentication -> Add a platform -> Web
- Redirect URIs:
  - `https://localhost:7001/signin-oidc`
  - `https://localhost:7001/signout-callback-oidc`
- Front-channel logout URL:
  - `https://localhost:7001/signout-callback-oidc`
- Implicit grant and hybrid flows:
  - Ensure both access tokens and ID tokens are unchecked

Certificates & secrets:
- App registration -> Certificates & secrets -> New client secret
- Record `Frontend_ClientSecret` (you can't view it again after you leave the page)

API permissions:
- App registration -> API permissions -> Add a permission
- My APIs -> select `SaaS.BackendApi` -> Delegated permissions -> select `Weather.Get`
- Add permissions
- Add a permission -> Microsoft Graph -> Delegated permissions -> select `User.Read` (recommended for `/me`)
- Grant admin consent (recommended)

Record:
- `Frontend_Domain` (your tenant domain, e.g. `contoso.onmicrosoft.com`)

## Local configuration (recommended: user-secrets)

Backend (`demo4.1/SaaS.Backend`):

```powershell
cd demo4.1\\SaaS.Backend
dotnet user-secrets set "AzureAd:TenantId" "<Backend_TenantId>"
dotnet user-secrets set "AzureAd:Audience" "api://<Backend_ClientId>"
```

Frontend (`demo4.1/SaaS.Frontend/SaaS.Frontend`):

```powershell
cd demo4.1\\SaaS.Frontend\\SaaS.Frontend
dotnet user-secrets set "AzureAd:TenantId" "<Frontend_TenantId>"
dotnet user-secrets set "AzureAd:ClientId" "<Frontend_ClientId>"
dotnet user-secrets set "AzureAd:ClientSecret" "<Frontend_ClientSecret>"
dotnet user-secrets set "AzureAd:Domain" "<Frontend_Domain>"
dotnet user-secrets set "DownstreamApis:WeatherApi:Scopes:0" "api://<Backend_ClientId>/Weather.Get"
dotnet user-secrets set "DownstreamApis:MicrosoftGraph:Scopes:0" "User.Read"
```

Note: `Microsoft.Identity.Web` downstream API configuration expects `Scopes` to be a collection (array). For user-secrets, set array entries using `:0`, `:1`, ...

## Run

```powershell
cd demo4.1
dotnet run --project .\\SaaS.AppHost
```

Note: `SaaS.AppHost` uses `Aspire.AppHost.Sdk` and `Properties/launchSettings.json` to configure the Dashboard and OTLP endpoints. If you run without launch settings for some reason, add `--launch-profile https`.

Then open the `frontend` endpoint and navigate to `/weather`.
