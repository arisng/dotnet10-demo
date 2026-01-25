# Request Flows — demo4.2 (IdP + BFF + API + Graph)

This document illustrates all request flows in the demo4.2 architecture using:
- An overview **data‑flow diagram** (DFD)
- **Flow cards** (compact bullets)
- **Sequence diagrams** (Mermaid)
- A single **mega‑diagram** (Mermaid)

---

## 1) Overview DFD (token map)

```mermaid
flowchart LR
  Browser[Browser]
  BFF[DProcess.Bff]
  IdP[DProcess.Idp OpenIddict + Identity]
  API[DProcess.Api]
  Entra[Microsoft Entra ID]
  Graph[Microsoft Graph]

  Browser -->|OIDC login| IdP
  IdP -->|id_token + access_token IdP| Browser
  Browser -->|cookie| BFF

  BFF -->|Bearer access_token IdP| API

  BFF -->|OIDC login Graph path| Entra
  Entra -->|access_token Entra| BFF
  BFF -->|Bearer access_token Entra| API

  API -->|OBO: access_token Entra -> Graph token| Entra
  Entra -->|access_token Graph| API
  API -->|Graph call| Graph
```

Legend:
- **IdP tokens** = OpenIddict‑issued, used for local API authorization + permission claims.
- **Entra tokens** = Entra‑issued, required for Graph OBO.
- **API auth** = two JWT bearer schemes with **per‑route policies** (`/api/*` → OpenIddict, `/api/graph/*` → Entra).
- **RBAC** = local `permission` claims apply to OpenIddict endpoints; Entra endpoints require either scope-only auth or permission enrichment.

---

## 2) Flow cards (compact descriptions)

**Flow A — Local login (OpenIddict)**
- Trigger: user signs in with local username/password or passkey
- Sequence: Browser → IdP → Browser (IdP tokens) → BFF (cookie)
- Tokens: `id_token` + `access_token` issued by **IdP**
- Authority: **OpenIddict**

**Flow B — External login via Entra (through IdP)**
- Trigger: user chooses “Microsoft Entra ID” on IdP login page
- Sequence: Browser → IdP → Entra → IdP → Browser → BFF
- Tokens: `id_token` + `access_token` issued by **IdP** (after external login)
- Authority: **OpenIddict**

**Flow C — BFF → API (local permissions)**
- Trigger: app calls `/api/*` for local features
- Sequence: Browser → BFF → API
- Tokens: `access_token` issued by **IdP**
- Authority: **OpenIddict**
- Auth policy: **OpenIddict scheme** + local `permission` claims

**Flow D — Graph path: BFF → API (Entra token)**
- Trigger: app calls `/api/graph/*` (Graph‑enabled path)
- Sequence: Browser → BFF (gets Entra token) → API
- Tokens: `access_token` issued by **Entra**
- Authority: **Entra**
- Auth policy: **Entra scheme**; RBAC is **scopes only** unless you enrich permissions

**Flow E — API → Graph (OBO)**
- Trigger: API receives Entra access token for Graph path
- Sequence: API → Entra (OBO exchange) → API → Graph
- Tokens: incoming **Entra** access token; outgoing **Graph** access token
- Authority: **Entra**

---

## 3) Sequence diagrams (Mermaid)

### Flow A — Local login (OpenIddict)
```mermaid
sequenceDiagram
  participant U as User/Browser
  participant IdP as DProcess.Idp
  participant BFF as DProcess.Bff

  U->>IdP: GET /Account/Login
  IdP-->>U: Login UI (local/passkey)
  U->>IdP: POST credentials / passkey
  IdP-->>U: id_token + access_token (IdP)
  U->>BFF: Subsequent requests (cookie)
```

### Flow B — External login via Entra (through IdP)
```mermaid
sequenceDiagram
  participant U as User/Browser
  participant IdP as DProcess.Idp
  participant Entra as Microsoft Entra ID
  participant BFF as DProcess.Bff

  U->>IdP: GET /Account/Login
  U->>IdP: POST ExternalLogin (Entra)
  IdP->>Entra: OIDC auth redirect
  Entra-->>U: Sign-in + consent (if needed)
  Entra-->>IdP: OIDC code callback
  IdP-->>U: id_token + access_token (IdP)
  U->>BFF: Subsequent requests (cookie)
```

### Flow C — BFF → API (local permissions)
```mermaid
sequenceDiagram
  participant U as User/Browser
  participant BFF as DProcess.Bff
  participant API as DProcess.Api

  U->>BFF: UI action
  BFF->>API: /api/* with Bearer (IdP access_token)
  API-->>BFF: response
  BFF-->>U: UI update
```

### Flow D — Graph path: BFF → API (Entra token)
```mermaid
sequenceDiagram
  participant U as User/Browser
  participant BFF as DProcess.Bff
  participant Entra as Microsoft Entra ID
  participant API as DProcess.Api

  U->>BFF: UI action (Graph feature)
  BFF->>Entra: OIDC auth (if no Entra token cached)
  Entra-->>BFF: access_token (Entra)
  BFF->>API: /api/graph/* with Bearer (Entra access_token)
  API-->>BFF: response
  BFF-->>U: UI update
```

### Flow E — API → Graph (OBO)
```mermaid
sequenceDiagram
  participant API as DProcess.Api
  participant Entra as Microsoft Entra ID
  participant Graph as Microsoft Graph

  API->>Entra: OBO (AcquireTokenOnBehalfOf)
  Entra-->>API: access_token (Graph)
  API->>Graph: Call Graph API
  Graph-->>API: Graph response
```

---

## 4) ClaimsPrincipal construction in BFF

### Summary
The BFF’s `ClaimsPrincipal` is created by the **OIDC handler** (from ID token + UserInfo), then persisted by the **cookie auth handler**. Blazor uses that cookie principal for SSR/InteractiveAuto.  
During prerendering, the **server auth state is serialized** and passed to the WASM client via `PersistingServerAuthenticationStateProvider`, so the client initializes with the same authenticated user **without re-authenticating**.

### Sequence diagram — BFF ClaimsPrincipal lifecycle
```mermaid
sequenceDiagram
  participant U as User/Browser
  participant BFF as DProcess.Bff
  participant IdP as DProcess.Idp
  participant Cookie as CookieAuthHandler
  participant OIDC as OpenIdConnectHandler

  U->>BFF: GET /login
  BFF->>IdP: OIDC challenge
  IdP-->>BFF: id_token (+ access_token) userinfo optional
  BFF->>OIDC: Validate tokens
  OIDC-->>BFF: ClaimsPrincipal (from id_token + userinfo)
  BFF->>Cookie: Sign-in principal
  Cookie-->>BFF: Auth cookie issued
  U->>BFF: Subsequent request
  Cookie-->>BFF: Rehydrate ClaimsPrincipal -> HttpContext.User
```

### Note (Option A)
Permission claims must be present in **ID token** or **UserInfo**, and the BFF must map them into the auth principal (so UI policies can evaluate `permission` claims).

### SSR → WASM auth state transfer (InteractiveAuto)
```mermaid
sequenceDiagram
  participant SSR as Server (SSR)
  participant PSP as PersistingServerAuthenticationStateProvider
  participant WASM as Client (WASM)

  SSR-->>PSP: Capture AuthenticationState
  PSP-->>WASM: Serialize auth state into response
  WASM-->>WASM: Restore AuthenticationState on startup
```

---

## 5) Mega‑diagram (all flows in one)

```mermaid
sequenceDiagram
  participant U as User/Browser
  participant BFF as DProcess.Bff
  participant IdP as DProcess.Idp
  participant API as DProcess.Api
  participant Entra as Microsoft Entra ID
  participant Graph as Microsoft Graph

  %% Flow A/B: login via IdP (local or Entra external)
  U->>IdP: /Account/Login (local or Entra)
  IdP->>Entra: OIDC (external login) optional
  Entra-->>IdP: code callback (external)
  IdP-->>U: id_token + access_token (IdP)
  U->>BFF: requests with cookie

  %% Flow C: local API path
  BFF->>API: /api/* with Bearer (IdP access_token)
  API-->>BFF: response

  %% Flow D: Graph path
  BFF->>Entra: OIDC (Graph scopes)
  Entra-->>BFF: access_token (Entra)
  BFF->>API: /api/graph/* with Bearer (Entra access_token)

  %% Flow E: OBO to Graph
  API->>Entra: OBO exchange
  Entra-->>API: access_token (Graph)
  API->>Graph: Graph call
  Graph-->>API: response
  API-->>BFF: response
```

Note: `/api/*` routes enforce the **OpenIddict** scheme + local `permission` policies; `/api/graph/*` routes enforce the **Entra** scheme and **do not** see local permissions unless you enrich the principal.
