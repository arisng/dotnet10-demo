# Demo4 Quick Reference

## Commands
```powershell
cd demo4\Demo4.EntraIntegration
# Build + run
dotnet build
dotnet watch
# Database migration
dotnet ef migrations add <Name>
dotnet ef database update
# Secrets
dotnet user-secrets set "AzureAd:ClientSecret" "..."
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
```

## Configuration Snippet
Add this section to `appsettings.json` (or keep the secrets in User Secrets/Key Vault):
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "your-tenant.onmicrosoft.com",
  "TenantId": "<TenantId>",
  "ClientId": "<ClientId>",
  "ClientCredentials": [
    {
      "SourceType": "ClientSecret",
      "ClientSecret": "<secret>"
    }
  ],
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
},
"DownstreamApi": {
  "BaseUrl": "https://graph.microsoft.com/v1.0",
  "Scopes": [ "User.Read", "User.ReadBasic.All" ]
}
```

## SQL Snippets
- List all Entra users:
```sql
SELECT Id, Email, EntraObjectId, DisplayName, JobTitle
FROM AspNetUsers
WHERE ExternalAuthenticationProvider = 'Entra';
```
- Assign Admin role:
```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'user@tenant.com'
  AND r.Name = 'Admin'
  AND u.ExternalAuthenticationProvider = 'Entra';
```
- View permissions:
```sql
SELECT DISTINCT p.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
JOIN RolePermissions rp ON r.Id = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.Id
WHERE u.Email = 'user@tenant.com';
```

## API & UI URLs
- App: https://localhost:7210
- Auth probe: https://localhost:7210/auth-state-probe
- Weather API: GET /api/weather (`weather.read` required)
- Users API: GET /api/users (`users.read`), POST /api/users (`users.write`)
- Reports API: GET /api/reports (`reports.view`)

## Helpful Links
- [Setup Guide](../guidance/setup-guide.md)
- [Implementation Patterns](../guidance/implementation-patterns.md)
- [Implementation Summary](../guidance/implementation-summary.md)
- [Research folder](../research/README.md)
- [Troubleshooting](../support/troubleshooting.md)

## Troubleshooting Shortcuts
Problems? Start with `support/troubleshooting.md` for reproducible steps and diagnostics commands, then check the AuthState probe or logs for the counters listed in `guidance/implementation-patterns.md`.
