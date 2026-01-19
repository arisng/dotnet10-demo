# Troubleshooting Demo4

## "AADSTS50011: The reply URL does not match"
- Verify redirect URIs in Entra app registration match exactly: `https://localhost:7210/signin-oidc`
- Check for typos in `appsettings.json` → `AzureAd:CallbackPath`

## "AADSTS65001: The user or administrator has not consented"
- Navigate to **API permissions** in Entra portal
- Click **Grant admin consent for [Tenant]**
- Refresh browser and retry sign-in

## "Failed to provision user" Error During Login
- Check application logs for detailed error from `EntraUserProvisioningService`
- Common causes:
  - Database connection failure
  - Missing required claims (`oid`, `preferred_username`)
  - Duplicate email conflict with existing local user
- Authentication fails if provisioning fails (by design) to prevent incomplete state

## Entra User Has No Permissions
- Entra users start with **default "User" role** (no elevated permissions)
- Manually assign roles via SQL (see "How to Run" section)
- If Entra app roles are configured, they sync automatically (with security whitelist)
- In demo6, we'll automate this via Entra App Roles mapping

## Graph API Returns 401 Unauthorized
- Verify `User.Read` scope is granted in Entra portal
- Check `DownstreamApi:Scopes` in `appsettings.json`
- Ensure `EnableTokenAcquisitionToCallDownstreamApi()` is called in `Program.cs`

## External Login Missing for Existing User
- Service automatically repairs this on next login via `EnsureExternalLoginExistsAsync()`
- Check logs for "External login missing for existing user" warning
- This can occur if user was created manually or provisioning partially failed previously