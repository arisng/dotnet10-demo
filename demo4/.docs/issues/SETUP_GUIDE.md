# Demo4 Setup Guide - Microsoft Entra ID Integration

## Quick Start

This guide walks you through setting up Microsoft Entra ID authentication for Demo4.

## Prerequisites

- Azure subscription with permissions to register applications in Entra ID
- .NET 10 SDK installed
- Completed demo3 (or understand the permission-based RBAC system)

## Step 1: Register Application in Azure Portal

### 1.1 Create App Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Microsoft Entra ID** → **App registrations** → **New registration**
3. Fill in the details:
   - **Name:** `Demo4.EntraIntegration` (or your preferred name)
   - **Supported account types:** 
     - For testing: "Accounts in this organizational directory only"
     - For multi-tenant: Choose appropriate option
   - **Redirect URI:** 
     - Platform: **Web**
     - URI: `https://localhost:7210/signin-oidc`
4. Click **Register**

### 1.2 Note Your Configuration Values

After registration, copy these values from the **Overview** page:

- **Application (client) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Directory (tenant) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Domain:** `your-tenant.onmicrosoft.com` (or custom domain)

### 1.3 Configure Authentication

1. Go to **Authentication** in the left menu
2. Under **Redirect URIs**, add:
   - `https://localhost:7210/signout-callback-oidc`
3. Under **Implicit grant and hybrid flows**, check:
   - ✅ **ID tokens** (for user sign-in)
4. Under **Advanced settings**, verify:
   - Allow public client flows: **No**
   - Supported account types: As configured in 1.1
5. Click **Save**

### 1.4 Configure API Permissions

1. Go to **API permissions** in the left menu
2. Verify `Microsoft Graph → User.Read` is present (added by default)
3. If not present, click **Add a permission**:
   - Select **Microsoft Graph**
   - Select **Delegated permissions**
   - Search for and check: `User.Read`
   - Click **Add permissions**
4. **(Optional)** If your tenant requires admin consent:
   - Click **Grant admin consent for [Your Tenant]**
   - Confirm the action

### 1.5 Create Client Secret

1. Go to **Certificates & secrets** in the left menu
2. Click **New client secret**
3. Fill in:
   - **Description:** `Demo4 Development Secret`
   - **Expires:** Choose based on your policy (e.g., 6 months, 12 months)
4. Click **Add**
5. **⚠️ IMPORTANT:** Copy the **Value** immediately (it's only shown once!)
   - Example: `abc123def456~xyz789...`

## Step 2: Configure Application Settings

### 2.1 Update appsettings.Development.json

Open `demo4/Demo4.EntraIntegration/appsettings.Development.json` and update with your values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "ClientSecret": "YOUR-CLIENT-SECRET",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  }
}
```

### 2.2 (Recommended) Use User Secrets for ClientSecret

For better security, store the client secret in User Secrets instead of appsettings:

```powershell
cd demo4\Demo4.EntraIntegration
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR-CLIENT-SECRET"
```

Then remove the `ClientSecret` line from `appsettings.Development.json`.

## Step 3: Run the Application

### 3.1 Ensure Database is Up to Date

The migration should already be applied, but verify:

```powershell
cd demo4\Demo4.EntraIntegration
dotnet ef database update
```

### 3.2 Start the Application

```powershell
dotnet watch
```

The app launches at:
- HTTPS: `https://localhost:7210`
- HTTP: `http://localhost:5210`

## Step 4: Test Authentication

### 4.1 Test Local Authentication (Baseline)

1. Navigate to `https://localhost:7210`
2. Click **Register** → Create account with email/password
3. Sign in with passkey (if supported) or password
4. Navigate to `/auth-state-probe`
5. Verify:
   - Provider: **Local (Passkey/Password)**
   - Claims include your email
   - Permissions appear (if assigned via demo3 seeding)

### 4.2 Test Entra ID Authentication

1. Sign out from local account
2. Click **Log in**
3. On the login page, under "Use another service to log in", you should see:
   - Button: **Microsoft Entra ID** (or similar)
4. Click the Microsoft button
5. Authenticate with your Entra ID account (work/school account)
6. **First-time login:** App creates a new `ApplicationUser` record automatically
7. You'll be redirected to the home page

### 4.3 Verify Entra Authentication State

1. Navigate to `/auth-state-probe`
2. Expand the **WebAssembly component** panel
3. Verify:
   - Provider: **Microsoft Entra ID** (badge should be blue)
   - Entra ID User Detected box shows:
     - Object ID: Your Entra `oid`
     - Tenant ID: Your Entra `tid`
     - UPN: Your email/UPN
   - All Claims section shows Entra-specific claims:
     - `oid` (Object ID)
     - `tid` (Tenant ID)
     - `preferred_username` (your email)
     - `name` (display name)

### 4.4 Assign Permissions to Entra User

**Why needed:** New Entra users have no roles/permissions by default.

**Option A: Quick Test via SQL**

```sql
-- Find your Entra user
SELECT Id, UserName, Email, ExternalAuthenticationProvider, EntraObjectId 
FROM AspNetUsers 
WHERE ExternalAuthenticationProvider = 'Entra';

-- Assign Admin role (replace YOUR_ENTRA_USER_ID)
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 'YOUR_ENTRA_USER_ID', Id FROM AspNetRoles WHERE Name = 'Admin';
```

**Option B: Use demo3 Seeding Logic**

Modify `DbSeeder.cs` to assign roles to specific Entra Object IDs.

### 4.5 Test BFF APIs with Entra User

1. Sign in with Entra ID user (with Admin role assigned)
2. Navigate to `/weather`
3. Try fetching weather data → Should succeed (requires `weather.read`)
4. Navigate to `/users`
5. Try viewing users → Should succeed (requires `users.read`)
6. Navigate to `/reports`
7. Try viewing reports → Should succeed (requires `reports.view`)

## Step 5: Verify Microsoft Graph Integration

### 5.1 Check Profile Sync

After Entra login, your profile should auto-sync from Microsoft Graph:

```sql
SELECT DisplayName, JobTitle, Email 
FROM AspNetUsers 
WHERE ExternalAuthenticationProvider = 'Entra';
```

- `DisplayName` should match your Entra display name
- `JobTitle` should show your job title (if set in Entra)

### 5.2 Monitor Logs

Check console output for Graph API calls:

```
[PermissionClaimsTransformation] Processing Entra ID user with oid: abc123...
[GraphService] Successfully fetched user profile from Microsoft Graph
[PermissionClaimsTransformation] Updated Graph profile for user: user@tenant.com
```

## Troubleshooting

### Error: "AADSTS50011: The reply URL does not match"

**Cause:** Redirect URI mismatch

**Fix:**
1. Verify in Azure Portal: Redirect URIs exactly match
2. Check `appsettings.Development.json`: `CallbackPath` is `/signin-oidc`
3. Ensure you're using `https://localhost:7210` (not 5210)

### Error: "AADSTS65001: The user or administrator has not consented"

**Cause:** API permissions not granted

**Fix:**
1. Go to Azure Portal → App registration → API permissions
2. Click **Grant admin consent for [Tenant]**
3. Refresh browser and retry sign-in

### Entra User Signed In But Has No Permissions

**Expected behavior!** Entra users start with no roles/permissions.

**Fix:**
- Manually assign roles via SQL (see Step 4.4)
- In production: Map Entra App Roles → local roles (demo6 will cover this)

### Graph API Returns 401 Unauthorized

**Cause:** Token acquisition failed or scope missing

**Fix:**
1. Verify `User.Read` scope granted in Azure Portal
2. Check `DownstreamApi:Scopes` in `appsettings.Development.json`
3. Ensure `EnableTokenAcquisitionToCallDownstreamApi()` called in `Program.cs`
4. Check logs for MSAL token errors

### Button "Microsoft Entra ID" Not Showing

**Cause:** External authentication scheme not registered or config invalid

**Fix:**
1. Verify `AddMicrosoftIdentityWebApp()` is called in `Program.cs`
2. Check `AzureAd` section in `appsettings.Development.json` is complete
3. Ensure TenantId, ClientId are valid GUIDs
4. Restart the app

## Configuration Security Best Practices

### Development
- ✅ Use User Secrets for `ClientSecret`
- ✅ Use `appsettings.Development.json` for non-sensitive config

### Production
- ❌ Never commit `ClientSecret` to source control
- ✅ Use Azure Key Vault for secrets
- ✅ Use Managed Identity to access Key Vault
- ✅ Use distributed token cache (Redis, SQL Server)
- ✅ Enable token encryption (`Encrypt = true`)

## Next Steps

- **Test hybrid scenarios:** Switch between local and Entra login
- **Explore claims transformation:** See how permissions are unified
- **Check metrics:** View auth metrics in logs/telemetry
- **Proceed to demo5:** Implement downstream API pattern with OBO flow

## Additional Resources

- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web/wiki)
- [Microsoft Graph API Reference](https://learn.microsoft.com/graph/api/overview)
- [ASP.NET Core Blazor Authentication](https://learn.microsoft.com/aspnet/core/blazor/security/)
- [Demo4 README](./README.md) - Detailed architecture explanation
- [Demo4 RESEARCH_FINDINGS](./RESEARCH_FINDINGS.md) - .NET 10 best practices

---

**Setup Complete!** You now have a production-grade hybrid authentication system with local passkeys and Microsoft Entra ID integration.
