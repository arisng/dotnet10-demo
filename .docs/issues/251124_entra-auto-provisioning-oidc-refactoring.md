# Refactoring Entra ID Auto-Provisioning from ClaimsTransformation to OIDC Events

**Date:** 2025-11-24
**Issue Type:** Architecture Decision
**Severity:** Medium
**Status:** Resolved

## 📋 Summary

Refactored the Entra ID user auto-provisioning logic from `IClaimsTransformation` to OIDC `OnTokenValidated` event handlers for production-ready implementation. The original approach worked but violated separation of concerns by performing database mutations within a claims transformation pipeline designed for read-only claim enrichment.

**Impact:** Improved code maintainability, error handling, idempotency, and alignment with Microsoft's recommended patterns for external authentication integration.

## 🔍 Analysis / Context

### Original Implementation Issues

- **Architectural Anti-Pattern**: Used `IClaimsTransformation.TransformAsync()` for user provisioning (database writes) instead of claim enrichment (read-only)
- **Side Effects**: Mixed database mutations with claims processing, violating single responsibility principle
- **Limited Error Handling**: Exception in transformation could leave partial state (user created but no external login)
- **Race Condition Vulnerability**: Multiple concurrent logins could attempt to create duplicate users
- **No Rollback Mechanism**: Failed `AddLoginAsync` left orphaned user records

### Why ClaimsTransformation Was Used Initially

- Provided seamless auto-provisioning without showing registration form
- Ran automatically for all authenticated requests
- Allowed unified permission loading for both Entra and local users
- Simple to implement for MVP/demo purposes

### Provider Key Validation

Research initially suggested potential mismatch between:
- `principal.GetNameIdentifierId()` (returns `sub` claim)
- `principal.GetObjectId()` (returns `oid` claim)

**Testing confirmed:** For single-tenant Entra apps, `SignInManager.GetExternalLoginInfoAsync()` returns `ProviderKey` matching `GetNameIdentifierId()`, so using either works correctly.

## ✅ Resolution / Decision

**Decision:** Move user provisioning to OIDC `OnTokenValidated` event handler with dedicated service.

### Implementation Structure

1. **New Service**: `EntraUserProvisioningService`
   - Handles all user creation, role sync, and profile updates
   - Implements proper idempotency and race condition protection
   - Provides rollback on failure

2. **OIDC Event Configuration** (`Program.cs`):
   ```csharp
   builder.Services.Configure<OpenIdConnectOptions>("MicrosoftEntra", options =>
   {
       options.Events = new OpenIdConnectEvents
       {
           OnTokenValidated = async context =>
           {
               var oid = context.Principal?.GetObjectId();
               if (!string.IsNullOrEmpty(oid))
               {
                   var provisioningService = context.HttpContext.RequestServices
                       .GetRequiredService<IEntraUserProvisioningService>();
                   await provisioningService.ProvisionUserAsync(context.Principal);
               }
           }
       };
   });
   ```

3. **Simplified ClaimsTransformation**:
   - Now only loads permission claims (original purpose)
   - No longer creates users or syncs roles
   - Clean separation of concerns

### Why OIDC Events are Better

- **Timing**: Runs once during authentication, not on every request
- **Appropriate Context**: Authentication pipeline is correct place for provisioning
- **Error Handling**: Can fail authentication if provisioning fails
- **Microsoft Pattern**: Recommended approach in official documentation
- **Performance**: Reduces overhead on subsequent requests

## 📚 Lessons Learned

### Key Takeaways

1. **ClaimsTransformation is for claim enrichment only** - Database mutations belong in authentication events or middleware
2. **OnTokenValidated is the right place for auto-provisioning** - Part of authentication flow, runs once per login
3. **Idempotency must be database-backed** - Claim-based checks aren't sufficient for concurrent scenarios
4. **Rollback is critical** - Partial user creation must be cleaned up to maintain consistency
5. **Separation of concerns matters** - Dedicated services are easier to test, maintain, and reason about
6. **Provider key matching needs validation** - Test that `GetNameIdentifierId()` matches `externalLoginInfo.ProviderKey` for your tenant configuration

### Security Insights

- Auto-creation of sensitive roles (Admin, Administrator) must be explicitly blocked
- Email verification from Entra can be trusted (`EmailConfirmed = true`)
- Graph API failures should be non-fatal (graceful degradation)
- Role whitelisting should be implemented for production

## 🛠️ Implementation

### Changes Made

**Created:**
- `Services/EntraUserProvisioningService.cs` - Dedicated provisioning logic with:
  - Idempotent user creation
  - Race condition protection
  - Automatic rollback on failure
  - External login recovery for existing users

**Modified:**
- `Program.cs` - Added OIDC event configuration and service registration
- `Authorization/PermissionClaimsTransformation.cs` - Removed provisioning logic, now only handles permission claims

**Removed:**
- `CreateEntraUserAsync()` from ClaimsTransformation
- `UpdateEntraUserProfileAsync()` from ClaimsTransformation  
- `IsSensitiveRole()` from ClaimsTransformation (moved to service)

### Key Code Patterns

**Idempotency Check:**
```csharp
var existingUser = await _userManager.Users
    .FirstOrDefaultAsync(u => u.EntraObjectId == oid, cancellationToken);

if (existingUser != null)
{
    await EnsureExternalLoginExistsAsync(existingUser, principal);
    return existingUser;
}
```

**Rollback on Failure:**
```csharp
var createResult = await _userManager.CreateAsync(user);
if (createResult.Succeeded)
{
    try
    {
        var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
        if (!addLoginResult.Succeeded)
        {
            await _userManager.DeleteAsync(user); // Rollback
            throw new InvalidOperationException("Failed to link external login");
        }
    }
    catch
    {
        await _userManager.DeleteAsync(user); // Rollback on any error
        throw;
    }
}
```

## 🔗 Related Files

- [`Services/EntraUserProvisioningService.cs`](../Demo4.EntraIntegration/Services/EntraUserProvisioningService.cs) - New provisioning service (lines 1-285)
- [`Program.cs`](../Demo4.EntraIntegration/Program.cs) - OIDC event configuration (lines 93-130)
- [`Authorization/PermissionClaimsTransformation.cs`](../Demo4.EntraIntegration/Authorization/PermissionClaimsTransformation.cs) - Simplified transformation (lines 28-95)
- [`AUTO_PROVISIONING_RESEARCH.md`](../AUTO_PROVISIONING_RESEARCH.md) - Detailed research findings

## 📖 Additional Resources

### Official Documentation
- [Microsoft.Identity.Web - Authentication Events](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/additional-claims)
- [ASP.NET Core Identity External Login](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)
- [IClaimsTransformation Best Practices](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims)

### Related Patterns
- OIDC Event Handlers: `OnTokenValidated`, `OnUserInformationReceived`
- External Login Flow: `GetExternalLoginInfoAsync()`, `ExternalLoginSignInAsync()`
- Account Linking: `AddLoginAsync()`, `UserLoginInfo`

## 🏷️ Tags

`dotnet` `blazor` `entra-id` `authentication` `architecture-decision` `oidc` `claims-transformation` `auto-provisioning` `aspnet-identity` `external-login` `security` `best-practices` `production-ready` `separation-of-concerns` `refactoring` `medium-priority`
