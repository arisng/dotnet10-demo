# Demo4 Architecture – Entra ID Integration

This document visualizes the architecture of Demo4 using the **C4 Model** (System Context, Container, and Component levels).

## 1. System Context Diagram (Level 1)

The System Context diagram provides a high-level view of how users interact with the application and how the application interacts with external services.

```mermaid
C4Context
    title System Context Diagram for Demo4

    Person(customer, "B2C Customer", "External user who uses passkeys for local authentication.")
    Person(employee, "Employee", "Internal user who authenticates via Microsoft Entra ID.")

    Enterprise_Boundary(local_env, "Local Application Environment") {
        System(demo4_system, "Demo4 Blazor Web App", "Provides weather, user management, and profile data via BFF.")
        SystemDb_Ext(database, "Local Database (SQLite)", "Stores user records, passkeys, and permissions.")
    }

    System_Boundary(external_cloud, "Microsoft Cloud Services") {
        System_Ext(entra_id, "Microsoft Entra ID", "External IdP handling OIDC and roles.")
        System_Ext(graph_api, "Microsoft Graph", "Downstream API for user profile data.")
    }

    Rel(customer, demo4_system, "Authenticates (Passkey)", "HTTPS")
    Rel(employee, demo4_system, "Authenticates (Entra ID)", "HTTPS/OIDC")
    
    Rel(demo4_system, database, "Reads/Writes", "EF Core")
    Rel(demo4_system, entra_id, "Validates", "HTTPS/OIDC")
    Rel(demo4_system, graph_api, "Fetches Data", "HTTPS/OBO")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="1")
```

## 2. Container Diagram (Level 2)

The Container diagram shows the high-level technology choices and how the Blazor Web App is split between Server-side and Client-side (WASM).

```mermaid
C4Container
    title Container Diagram for Demo4

    Person(user, "Authenticated User", "Customer or Employee.")

    System_Boundary(demo4_boundary, "Demo4 System") {
        Container(client_wasm, "Blazor WASM Client", "C#, .NET 10", "Interactive UI.")
        Container(server_app, "Blazor Server (BFF)", "ASP.NET Core 10", "Handles Auth, OBO, and APIs.")
        ContainerDb(sqlite_db, "SQLite Database", "SQLite", "Identity and App data.")
    }

    System_Boundary(ext_services, "External Services") {
        System_Ext(entra_id, "Microsoft Entra ID", "Identity Provider")
        System_Ext(graph_api, "Microsoft Graph", "Downstream API")
    }

    Rel(user, server_app, "Auth Request", "HTTPS")
    Rel(user, client_wasm, "Interacts", "WASM")
    
    Rel(client_wasm, server_app, "BFF Calls", "HTTPS/Cookie")
    Rel(server_app, sqlite_db, "Accesses", "EF Core")
    
    Rel(server_app, entra_id, "OIDC Flow", "HTTPS")
    Rel(server_app, graph_api, "OBO Calls", "HTTPS/Bearer")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="1")
```

## 3. Component Diagram (Level 3 - Server-side)

This diagram focuses on the internal components of the **Server-side (BFF)** container, specifically the authentication and authorization pipeline.

```mermaid
C4Component
    title Component Diagram (Server-side BFF)

    %% Layer 1: Entry Point
    Component(am, "Auth Middleware", "ASP.NET Core", "Session & OIDC Management")

    %% Layer 2: Core Processors (wide spacing)
    Component(up, "User Provisioning", "Service", "Syncs Entra users to DB")
    Component(ct, "Claims Transformation", "IClaimsTransformation", "Loads permissions")

    %% Layer 3: Request Handlers
    Component(ph, "Policy Handler", "Authorization", "Evaluates RBAC")
    Component(apis, "BFF Endpoints", "Minimal APIs", "Weather & Graph Proxies")
    Component(gs, "Graph Service", "IGraphService", "OBO Token Exchange")

    %% Layer 4: Persistence
    ContainerDb(db, "SQLite Database", "EF Core", "Stores Identity & App Data")

    %% Relationships - organized by flow
    Rel(am, up, "Triggers", "user provisioning")
    Rel(up, db, "Writes user", "EF Core")
    
    Rel(am, ct, "Enriches claims", "post-auth")
    Rel(ct, db, "Loads permissions", "query")

    Rel(apis, ph, "Enforces policy", "before execute")
    Rel(apis, gs, "Calls OBO", "token exchange")
    Rel(gs, db, "Reads config", "EF Core")
```

## Architecture Summary

- **Hybrid Authentication:** Supports both Local Identity (for Customers using Passkeys) and Microsoft Entra ID (for Employees).
- **Auto-Provisioning:** User records are automatically created in the local database during the OIDC `OnTokenValidated` event.
- **Unified Permission Model:** Whether a user is local or from Entra, they are mapped to local roles, and `IClaimsTransformation` loads their `permission` claims for authorization.
- **BFF Pattern:** The client WASM never holds access tokens. It authenticates with the server using secure, HttpOnly cookies. The server performs token exchange (OBO) to call downstream APIs like Microsoft Graph.
