# Demo 5.1 Authentication & Request Flows

This document details the sequence of events for authentication and API communication in the distributed modular monolith.

## 1. Local Authentication Flow

Used when a user signs in with a local account (email/password).

```mermaid
sequenceDiagram
    actor User
    participant Web as "Demo5_1.Web (BFF)"
    participant Api as "Demo5_1.ApiService"
    participant DB as "SQL Database"

    User->>Web: POST /account/login (Email, Pwd)
    Web->>Api: POST /api/identity/token (creds)
    Api->>DB: Validate credentials
    DB-->>Api: User found & valid
    Api-->>Web: 200 OK { AccessToken: "Local JWT" }
    
    Web->>Web: SignInAsync (Cookies)
    Note over Web: Stores Local JWT in "api_access_token" claim
    
    Web->>Api: POST /api/identity/provision (Bearer Local JWT)
    Api->>Api: Skip provisioning (already local)
    Api-->>Web: 200 OK
    
    Web-->>User: Redirect to Home
```

## 2. Microsoft Entra ID Flow (OBO)

Used when a user signs in via Entra ID. The BFF uses the On-Behalf-Of (OBO) flow to call the API.

```mermaid
sequenceDiagram
    actor User
    participant Web as "Demo5_1.Web (BFF)"
    participant Entra as "Microsoft Entra ID"
    participant Api as "Demo5_1.ApiService"

    User->>Web: GET /MicrosoftIdentity/Account/SignIn
    Web->>Entra: OIDC Challenge (Auth Code Flow)
    Entra->>User: Login & Consent
    User-->>Entra: Authenticate
    Entra-->>Web: Auth Code
    
    Web->>Entra: Token Exchange (via MS Identity Web)
    Entra-->>Web: Access Token + Refresh Token
    
    Web->>Api: POST /api/identity/provision (Bearer OBO Token)
    Api->>Api: Auto-provision user (Link OID)
    Api-->>Web: 200 OK
    
    Web-->>User: Redirect to Home
```

## 3. Protected API Request (Unified YARP Proxy)

All browser-to-API requests pass through YARP, which attaches the appropriate token regardless of the identity provider.

```mermaid
sequenceDiagram
    actor User
    participant Web as "Demo5_1.Web (BFF)"
    participant Provider as "HybridApiTokenProvider"
    participant Api as "Demo5_1.ApiService"

    User->>Web: GET /api/weather
    Web->>Web: YARP Request Transform
    
    Web->>Provider: GetTokenAsync()
    alt Entra User
        Provider->>Provider: Get OBO token (TokenAcquisition)
    else Local User
        Provider->>Provider: Get token from Cookie claim
    end
    Provider-->>Web: Access Token (JWT)
    
    Web->>Api: Forward: GET /api/weather (Auth: Bearer)
    
    Api->>Api: BearerSelector Middleware
    Note right of Api: Routes to "Bearer" (Entra) or "LocalBearer" based on 'iss'
    
    Api->>Api: PermissionClaimsTransformation
    Note right of Api: Resolves user & loads local RBAC permissions
    
    Api-->>Web: JSON Response
    Web-->>User: Weather Data
```

## 4. Authentication State Diagram

Shows the different authentication states a user can transition between in the hybrid system.

```mermaid
stateDiagram-v2
    [*] --> Anonymous
    
    Anonymous --> LocalAuthenticating : Login with email/password
    Anonymous --> EntraAuthenticating : Click "Sign in with Microsoft"
    
    LocalAuthenticating --> LocallyAuthenticated : JWT stored in claims
    EntraAuthenticating --> EntraAuthenticated : OBO token available
    
    LocallyAuthenticated --> TokenExpired : JWT expires
    EntraAuthenticated --> TokenExpired : Access token expires
    
    TokenExpired --> Anonymous : Session ends
    LocallyAuthenticated --> Anonymous : Logout
    EntraAuthenticated --> Anonymous : Logout
    
    LocallyAuthenticated --> MakingApiCall : API request via YARP
    EntraAuthenticated --> MakingApiCall : API request via YARP
    
    MakingApiCall --> LocallyAuthenticated : Success (local JWT reused)
    MakingApiCall --> EntraAuthenticated : Success (fresh OBO token)
    MakingApiCall --> TokenExpired : Token invalid/expired
```

## 5. Token Provider Decision Flow

Illustrates the logic in `HybridApiTokenProvider.GetTokenAsync()` for determining which token to use.

```mermaid
flowchart TD
    A[GetTokenAsync called] --> B{User authenticated?}
    B -->|No| C[Return null]
    B -->|Yes| D{Has oid claim?}
    D -->|Yes| E[Entra ID User]
    D -->|No| F[Local User]
    E --> G[Get OBO token]
    G --> H{Token acquired?}
    H -->|Yes| I[Return OBO token]
    H -->|No| J[Throw exception]
    F --> K[Get api_access_token claim]
    K --> L{Claim exists?}
    L -->|Yes| M[Return stored JWT]
    L -->|No| N[Return null]
```

## 6. Authentication Component Diagram

Shows the key classes, interfaces, and their relationships in the authentication system.

```mermaid
classDiagram
    class IApiTokenProvider {
        +GetTokenAsync() Task<string>
    }
    
    class HybridApiTokenProvider {
        -IHttpContextAccessor _httpContextAccessor
        -ITokenAcquisition _tokenAcquisition
        -IConfiguration _configuration
        +GetTokenAsync() Task<string>
    }
    
    class YarpTransform {
        +Transform() void
    }
    
    class BearerSelectorMiddleware {
        +InvokeAsync() Task
    }
    
    class PermissionClaimsTransformation {
        +TransformAsync() Task
    }
    
    IApiTokenProvider <|.. HybridApiTokenProvider
    HybridApiTokenProvider --> YarpTransform : provides tokens to
    BearerSelectorMiddleware --> PermissionClaimsTransformation : delegates to
```

## 7. Multi-Scheme Authentication Flow

Detailed sequence showing how the API service handles both Bearer schemes and token routing.

```mermaid
sequenceDiagram
    participant Client as "Browser/YARP"
    participant Api as "Demo5_1.ApiService"
    participant BearerSelector as "BearerSelectorMiddleware"
    participant EntraAuth as "Bearer Authentication"
    participant LocalAuth as "LocalBearer Authentication"
    participant ClaimsTransform as "PermissionClaimsTransformation"

    Client->>Api: GET /api/weather (Authorization: Bearer <token>)
    Api->>BearerSelector: Invoke middleware
    
    BearerSelector->>BearerSelector: Inspect token 'iss' claim
    alt Entra ID token (iss: login.microsoftonline.com)
        BearerSelector->>EntraAuth: Route to "Bearer" scheme
        EntraAuth->>EntraAuth: Validate Microsoft token
        EntraAuth-->>BearerSelector: Success + ClaimsPrincipal
    else Local JWT token (iss: apiservice)
        BearerSelector->>LocalAuth: Route to "LocalBearer" scheme  
        LocalAuth->>LocalAuth: Validate local JWT
        LocalAuth-->>BearerSelector: Success + ClaimsPrincipal
    end
    
    BearerSelector->>ClaimsTransform: Transform claims
    ClaimsTransform->>ClaimsTransform: Load user + RBAC permissions
    ClaimsTransform-->>BearerSelector: Enhanced ClaimsPrincipal
    
    BearerSelector-->>Api: Authenticated request
    Api->>Api: Execute business logic
    Api-->>Client: JSON Response
```

## 8. Token Lifecycle Diagram

Shows the complete lifecycle of tokens in the hybrid authentication system.

```mermaid
flowchart TD
    subgraph "Local Authentication"
        A1[User Login] --> B1[Call /api/identity/token]
        B1 --> C1[JWT Created by ApiService]
        C1 --> D1[Stored in 'api_access_token' claim]
        D1 --> E1[Persisted in Cookie Session]
        E1 --> F1[Reused for all API calls]
        F1 --> G1{JWT Expires?}
        G1 -->|No| F1
        G1 -->|Yes| H1[User must re-login]
    end
    
    subgraph "Entra ID Authentication"
        A2[User Login] --> B2[OIDC Code Flow]
        B2 --> C2[Access + Refresh tokens stored]
        C2 --> D2[API Call requested]
        D2 --> E2[OBO Token acquired fresh]
        E2 --> F2[Token cached briefly]
        F2 --> G2[Used for API call]
        G2 --> H2{Token expires soon?}
        H2 -->|No| I2[Reuse cached token]
        H2 -->|Yes| J2[Acquire new OBO token]
        J2 --> F2
    end
    
    subgraph "Token Validation"
        V1[API receives Bearer token] --> V2{Which issuer?}
        V2 -->|Microsoft| V3[Validate via Microsoft scheme]
        V2 -->|ApiService| V4[Validate via LocalBearer scheme]
        V3 --> V5[Load permissions from DB]
        V4 --> V5
        V5 --> V6[Authorize request]
    end
```
