# Azure Entra ID Setup for Demo4

## Overview
Register a single client app for authentication and Graph access. BFF APIs use cookies; no custom scopes needed (demo5 adds them).

## Step-by-Step: Register Blazor Client App
1. Go to Azure Portal → Microsoft Entra ID → App registrations → New registration.
2. Name: `Demo4.EntraIntegration`, Account types: Organizational directory.
3. Redirect URIs: `https://localhost:7210/signin-oidc` and `/signout-callback-oidc`.
4. Enable ID tokens.
5. API Permissions: Add `User.Read` (Delegated).
6. Grant admin consent.
7. Create client secret, copy value.
8. Note: Application ID, Tenant ID.

## Configuration
Update `appsettings.json` with AzureAd section (Instance, Domain, TenantId, ClientId, ClientSecret, etc.) and DownstreamApi (BaseUrl: https://graph.microsoft.com/v1.0, Scopes: ["User.Read"]).

For dev, use `dotnet user-secrets set "AzureAd:ClientSecret" "YOUR-SECRET"`.