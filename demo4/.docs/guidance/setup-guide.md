# Demo4 Setup Guide

## Overview
Register a single client app for authentication and Graph access. BFF APIs use cookies; no custom scopes needed (demo5 adds them).

## Prerequisites
- Azure subscription with rights to register Microsoft Entra apps
- .NET 10 SDK installed
- Demo3 or familiarity with the permission-backed RBAC seed data
- (Recommended) `.NET user secrets` configured for `AzureAd:ClientSecret`

## Step-by-Step: Register Blazor Client App
1. In the Azure Portal go to Microsoft Entra ID → App registrations → New registration.
2. Use a descriptive name (e.g., `Demo4.EntraIntegration`) and choose the appropriate account types (Organizational directory).
3. Under Redirect URIs add `https://localhost:7210/signin-oidc` and `https://localhost:7210/signout-callback-oidc`.
4. Enable ID tokens in the Authentication blade and disable public client flows.
5. Copy the Application (client) ID, Directory (tenant) ID, and create a client secret (save the value immediately). Grant `Microsoft Graph` → `User.Read` under API permissions, then grant admin consent if required.

## Step 2: Wire Up Configuration
- Update `appsettings.json` with the AzureAd + DownstreamApi sections in `reference/quick-reference.md` (move secrets to User Secrets when possible).
- Store `ClientSecret` via:
  ```powershell
  dotnet user-secrets set "AzureAd:ClientSecret" "<secret>"
  ```
- Ensure `DownstreamApi:Scopes` contains `User.Read` (plus `User.ReadBasic.All` if you need basic profile data).
- Optional: Configure Redis or SQL token cache providers and set `MsalDistributedTokenCacheAdapterOptions.Encrypt = true`.

## Step 3: Run & Verify
1. `dotnet ef database update` to apply the migration `AddEntraIntegrationFields`.
2. `dotnet watch` from `demo4/Demo4.EntraIntegration`.
3. Visit https://localhost:7210, register a local account, and confirm the auth-state-probe lists the local provider and permission claims.
4. Sign out and sign in with Microsoft Entra ID. The `auth-state-probe` should show a blue Entra badge, `oid`, `tid`, and the permission list.
5. Use the SQL snippet from `reference/quick-reference.md` to assign roles to the new Entra user so API navigation succeeds.

## Key Validation Checks
- [ ] Local credentials still work (passkey or password)
- [ ] Entra login provisions a user with `EntraObjectId` filled
- [ ] Permissions show up in the auth-state probe under `permission_transformed`
- [ ] Graph profile data appears (DisplayName, JobTitle)
- [ ] Weather/Users/Reports pages honor the required permissions
- [ ] Logs emit `aspnetcore.authentication.*` and `aspnetcore.authorization.*` metrics

## Troubleshooting Jumpstart
Refer to `support/troubleshooting.md` for structured steps on redirect mismatches, Graph API issues, missing permissions, and token cache resets.