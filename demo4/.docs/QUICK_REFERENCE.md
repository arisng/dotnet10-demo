# Demo4 Quick Reference

## Common Commands

### Build & Run
```powershell
cd demo4\Demo4.EntraIntegration
dotnet build                    # Build the solution
dotnet watch                    # Run with hot reload
dotnet run                      # Run without hot reload
```

### Database Operations
```powershell
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback one migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### User Secrets (Recommended for ClientSecret)
```powershell
cd demo4\Demo4.EntraIntegration

# Set client secret
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-value"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "AzureAd:ClientSecret"

# Clear all secrets
dotnet user-secrets clear
```

## Configuration Quick Copy

### Minimum Required Configuration

Add to `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-guid",
    "ClientId": "your-app-client-guid",
    "ClientSecret": "your-client-secret-or-use-user-secrets",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  }
}
```

## Azure Portal Quick Links

- **App Registrations:** [https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps)
- **Enterprise Applications:** [https://portal.azure.com/#blade/Microsoft_AAD_IAM/StartboardApplicationsMenuBlade/AllApps](https://portal.azure.com/#blade/Microsoft_AAD_IAM/StartboardApplicationsMenuBlade/AllApps)
- **Microsoft Graph Explorer:** [https://developer.microsoft.com/graph/graph-explorer](https://developer.microsoft.com/graph/graph-explorer)

## Azure App Registration Checklist

- [ ] App registered in Entra ID
- [ ] Redirect URI: `https://localhost:7210/signin-oidc`
- [ ] Logout URI: `https://localhost:7210/signout-callback-oidc`
- [ ] ID tokens enabled in Authentication
- [ ] User.Read permission granted
- [ ] Client secret created and saved
- [ ] Client ID and Tenant ID copied

## Common SQL Queries

### View All Users
```sql
SELECT Id, UserName, Email, EmailConfirmed, 
       ExternalAuthenticationProvider, EntraObjectId, 
       DisplayName, JobTitle
FROM AspNetUsers;
```

### Find Entra Users
```sql
SELECT Id, Email, DisplayName, JobTitle, EntraObjectId
FROM AspNetUsers
WHERE ExternalAuthenticationProvider = 'Entra';
```

### Find Local Users
```sql
SELECT Id, UserName, Email
FROM AspNetUsers
WHERE ExternalAuthenticationProvider IS NULL;
```

### View User Roles
```sql
SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.Email;
```

### Assign Admin Role to Entra User
```sql
-- Replace 'user@tenant.com' with actual email
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'user@tenant.com'
  AND r.Name = 'Admin'
  AND u.ExternalAuthenticationProvider = 'Entra';
```

### View All Permissions for a User
```sql
SELECT DISTINCT p.Name AS Permission
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
JOIN RolePermissions rp ON r.Id = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.Id
WHERE u.Email = 'user@tenant.com'
ORDER BY p.Name;
```

## Test URLs

After starting the app at `https://localhost:7210`:

- **Home:** [https://localhost:7210](https://localhost:7210)
- **Login:** [https://localhost:7210/Account/Login](https://localhost:7210/Account/Login)
- **Register:** [https://localhost:7210/Account/Register](https://localhost:7210/Account/Register)
- **Auth Probe:** [https://localhost:7210/auth-state-probe](https://localhost:7210/auth-state-probe)
- **Weather:** [https://localhost:7210/weather](https://localhost:7210/weather)
- **Users:** [https://localhost:7210/users](https://localhost:7210/users)
- **Reports:** [https://localhost:7210/reports](https://localhost:7210/reports)

## API Endpoints (BFF)

All require authentication and specific permissions:

### Weather API
- `GET /api/weather` - Requires `weather.read` permission
- `POST /api/weather` - Requires `weather.write` permission

### Users API
- `GET /api/users` - Requires `users.read` permission
- `POST /api/users` - Requires `users.write` permission
- `DELETE /api/users/{id}` - Requires `users.delete` permission

### Reports API
- `GET /api/reports` - Requires `reports.view` permission
- `GET /api/reports/export` - Requires `reports.export` permission

## Troubleshooting Quick Checks

### "Sign in with Microsoft" button not showing
```powershell
# Check if configuration is loaded
dotnet user-secrets list

# Verify AzureAd section in appsettings
# Restart the app
```

### AADSTS50011: Reply URL mismatch
```
1. Check Azure Portal redirect URIs (must be exact)
2. Verify using https://localhost:7210 (not http://localhost:5210)
3. Ensure CallbackPath is "/signin-oidc"
```

### User signed in but has no permissions
```sql
-- Check if user has roles
SELECT * FROM AspNetUserRoles WHERE UserId = 'user-id';

-- Assign Admin role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 'user-id', Id FROM AspNetRoles WHERE Name = 'Admin';
```

### Graph API 401 errors
```
1. Check User.Read permission granted in Azure Portal
2. Verify Scopes in appsettings: "User.Read"
3. Check logs for MSAL token acquisition errors
4. Try signing out and back in to refresh tokens
```

## Environment Variables (Alternative to User Secrets)

```powershell
# Set for current PowerShell session
$env:AzureAd__ClientSecret = "your-secret"

# Run app with environment variable
dotnet run
```

Or create `.env` file (requires additional configuration).

## Logs to Monitor

### Important log patterns:
- `[PermissionClaimsTransformation] Processing Entra ID user` - Entra user detected
- `[GraphService] Successfully fetched user profile` - Graph API success
- `[PermissionClaimsTransformation] Added X permissions` - Permission loading
- `MSAL` - Token acquisition logs from Microsoft.Identity.Web

### Increase logging verbosity:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.Identity.Web": "Debug"
    }
  }
}
```

## Performance Notes

- **First Entra login:** Slower (creates user + calls Graph API)
- **Subsequent logins:** Faster (only updates profile from Graph)
- **Token caching:** Currently in-memory (fast, but lost on restart)
- **Claims transformation:** Cached per request via `permissions_loaded` claim

## Security Checklist

### Development ✅
- [x] HTTPS redirect enabled
- [x] Antiforgery tokens in forms
- [x] Authentication required for APIs
- [x] Permission-based authorization

### Production ⚠️
- [ ] Client secret in Key Vault (not appsettings)
- [ ] Distributed token cache (Redis/SQL)
- [ ] Token encryption enabled
- [ ] Data Protection key ring configured
- [ ] HSTS enabled (already in code)
- [ ] Logging and monitoring configured

## Useful Documentation Links

- **Demo4 README:** [./README.md](./README.md) - Architecture & features
- **Setup Guide:** [./SETUP_GUIDE.md](./SETUP_GUIDE.md) - Step-by-step configuration
- **Implementation Summary:** [./IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) - Technical details
- **Research Findings:** [./RESEARCH_FINDINGS.md](./RESEARCH_FINDINGS.md) - .NET 10 best practices

## Contact & Support

For issues with:
- **Demo setup:** Check SETUP_GUIDE.md troubleshooting section
- **Entra configuration:** Review Azure Portal checklist above
- **Permissions:** Check SQL queries section for role assignment
- **Graph API:** Test with Graph Explorer first to isolate issues

---

**Quick Start:** See [SETUP_GUIDE.md](./SETUP_GUIDE.md) Section "Step 1: Register Application"
