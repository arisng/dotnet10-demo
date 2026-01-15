# Demo4: Entra ID Implementation Gaps

**Date:** 2025-12-15  
**Status:** ⚠️ Partial Implementation  
**Assignee:** TBD  
**Priority:** High  

---

## Problem Statement

Demo4 README documents comprehensive Entra ID integration features including claims mapping and Graph API integration. However, the actual codebase has significant implementation gaps. Core authentication scaffolding is complete, but advanced features are either missing or incomplete, preventing full end-to-end testing.

---

## Current State Assessment

### ✅ Implemented & Working
- Entra ID Authentication (OIDC) in Program.cs
- Graph API Integration (OBO flow)
- Entra User Provisioning Service
- ApplicationUser extended with Entra fields

### ⚠️ Partially Implemented (Gaps)
- Claims Mapping (Entra Roles) - Service exists, schema missing
- EntraRoleClaimsTransformation Logic - Needs verification
- AuthStateProbe Enhancements - May not show Entra-specific claims

### ❌ Not Implemented
- RoleMappingConfiguration Table (schema + migration)
- RoleMappingManager.razor Admin UI
- "Sign in with Microsoft" Button verification
- Graph API Data Components (Profile.razor)
- Demo3 Pages/Components (weather, users, reports)

---

## Detailed Gaps & Resolutions

### Gap 1: RoleMappingConfiguration Table (CRITICAL)

**Issue:**  
Entra App Role values ("GlobalAdmin", "ContentManager") cannot be mapped to local roles ("Admin", "Manager") without a database table and admin UI.

**Current State:**
- ❌ Entity model `RoleMappingConfiguration` missing
- ❌ DbSet not added to ApplicationDbContext
- ❌ No database migration

**Required Implementation:**

```csharp
// Data/RoleMappingConfiguration.cs
public class RoleMappingConfiguration
{
    public int Id { get; set; }
    public string EntraAppRoleValue { get; set; }  // "GlobalAdmin"
    public string LocalRoleName { get; set; }      // "Admin"
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

// In Data/ApplicationDbContext.cs
public DbSet<RoleMappingConfiguration> RoleMappingConfigurations { get; set; }
```

**Resolution Steps:**
1. Create entity model
2. Add DbSet to ApplicationDbContext
3. Create EF Core migration
4. Seed demo data: GlobalAdmin→Admin, ContentManager→Manager
5. Update DbSeeder.cs

**Impact:** Without this, Entra roles cannot be dynamically mapped. Manual SQL required.

---

### Gap 2: RoleMappingManager.razor Admin UI (MEDIUM)

**Issue:**  
No UI for non-developers to manage Entra → Local role mappings.

**Current State:**
- ❌ Component does not exist in `Components/Pages/`
- ❌ No CRUD API endpoints for role mappings
- ❌ Documentation marks as "optional future work" but blocks self-service admin

**Required Implementation:**
1. Create `Components/Pages/RoleMappingManager.razor`
   - Display table of current mappings
   - Add/Edit/Delete UI forms
   - Show available Entra App Roles

2. Create API endpoints:
   - `GET /api/admin/role-mappings`
   - `POST /api/admin/role-mappings`
   - `PUT /api/admin/role-mappings/{id}`
   - `DELETE /api/admin/role-mappings/{id}`

3. Add authorization:
   - Require `roles.manage` permission
   - Restrict to Admin role users

**Impact:** Enables self-service admin configuration without code changes.

---

### Gap 3: EntraRoleClaimTransformation Verification (HIGH)

**Issue:**  
Unclear if `PermissionClaimsTransformation.cs` properly handles Entra role mapping.

**Current State:**
- ✅ Service file exists
- ⚠️ Implementation unclear:
  - Does it read `roles` claim from Entra token?
  - Does it query `RoleMappingConfiguration` table?
  - Does it handle missing mappings gracefully?
  - Does it block sensitive roles (Admin, Administrator)?

**Verification Checklist:**
- [ ] Read `roles` claim from ClaimsPrincipal
- [ ] Query RoleMappingConfiguration for each Entra role
- [ ] Apply mapped local role + permissions
- [ ] Log when mapping not found
- [ ] Implement security whitelist

**Example Logic:**
```csharp
var entraRoles = principal.FindAll("roles");
foreach (var role in entraRoles) 
{
    var mapping = await dbContext.RoleMappingConfigurations
        .FirstOrDefaultAsync(m => m.EntraAppRoleValue == role);
    
    if (mapping != null) 
    {
        // Add local role + load permissions
        claims.Add(new Claim(ClaimTypes.Role, mapping.LocalRoleName));
    }
    else 
    {
        _logger.LogWarning($"No mapping found for Entra role: {role}");
    }
}
```

**Impact:** Without this, Entra users cannot authenticate with permission-based authorization.

---

### Gap 4: AuthStateProbe Enhancements (MEDIUM)

**Issue:**  
AuthStateProbe may not display Entra-specific claims and authentication provider information.

**Current State:**
- ❓ Unknown if component displays:
  - Auth provider detection ("Local (Passkey)" vs "Microsoft Entra ID")
  - Entra claims (`oid`, `tid`, `preferred_username`)
  - Graph API user data (displayName, jobTitle, profile photo)

**Verification Checklist:**
- [ ] Component exists and is inherited from demo3
- [ ] Displays auth provider source
- [ ] Shows Entra-specific claims
- [ ] Renders Graph API profile data

**Enhancement Example:**
```razor
@if (authProvider == "EntraId")
{
    <p>Auth Provider: <strong>Microsoft Entra ID</strong></p>
    <p>Object ID: @claims["oid"]</p>
    <p>Tenant ID: @claims["tid"]</p>
    @if (graphProfile != null)
    {
        <img src="@profilePhotoUrl" />
        <p>Display Name: @graphProfile.DisplayName</p>
    }
}
```

**Impact:** Better diagnostics for troubleshooting Entra authentication issues.

---

### Gap 5: Demo3 Pages Not Carried Forward (LOW)

**Issue:**  
Demo4 should inherit working BFF API demo pages from demo3 for testing authorization.

**Missing Pages:**
- ❌ `/weather` - Weather API demo
- ❌ `/users` - User management demo
- ❌ `/reports` - Reports demo

**Current State:**
- `Components/Pages/` only contains Error.razor
- Cannot test permission-based authorization without consuming APIs

**Resolution:**
1. Copy demo3's `/weather`, `/users`, `/reports` Razor components
2. Update for InteractiveAuto mode (SSR + WASM)
3. Verify authorization works with both local and Entra users

**Impact:** Enables end-to-end testing of unified authorization system.

---

### Gap 6: Graph API Data Components (LOW)

**Issue:**  
IGraphService implements `GetUserProfileAsync()` and `GetUserPhotoAsync()` but no components consume this data.

**Current State:**
- ✅ GraphService methods implemented
- ❌ No Razor components display profile data
- ❌ No demo page showing Graph API integration

**Recommended Implementation:**
Create `Components/Pages/Profile.razor`:
```razor
@page "/profile"
@rendermode InteractiveAuto
@inject IGraphService GraphService

<h2>My Profile</h2>
@if (profile != null)
{
    <img src="@profilePhotoUrl" alt="Profile" />
    <p>Display Name: @profile.DisplayName</p>
    <p>Job Title: @profile.JobTitle</p>
    <p>Email: @profile.Mail</p>
}

@code {
    private UserProfile? profile;
    private string? profilePhotoUrl;

    protected override async Task OnInitializedAsync()
    {
        profile = await GraphService.GetUserProfileAsync();
        var photo = await GraphService.GetUserPhotoAsync();
        if (photo != null)
        {
            profilePhotoUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photo)}";
        }
    }
}
```

**Impact:** Demonstrates Graph API integration in action.

---

## Implementation Priority & Timeline

### Phase 1: Critical (1-2 hours)
1. Add `RoleMappingConfiguration` entity + migration
2. Seed demo data mappings
3. Audit EntraRoleClaimTransformation logic

### Phase 2: Important (2-3 hours)
4. Copy demo3 BFF API pages (weather, users, reports)
5. Enhance AuthStateProbe with Entra claims
6. Test dual authentication (local vs. Entra)

### Phase 3: Nice-to-Have (3-4 hours)
7. Create RoleMappingManager.razor admin UI
8. Create Profile.razor component (Graph API demo)
9. Add comprehensive integration tests

---

## Testing Requirements

**Cannot Currently Test:**
- ❌ Entra role → local role mapping flow
- ❌ Graph API profile/photo display
- ❌ Entra users with elevated permissions
- ❌ Permission claims from Entra roles
- ❌ Dual authentication in action

**Test Plan (Post-Implementation):**
1. Create test Entra roles in tenant
2. Configure mappings in RoleMappingConfiguration
3. Authenticate as Entra user with mapped role
4. Verify permissions are applied correctly
5. Verify Graph API profile displays on Profile.razor
6. Verify AuthStateProbe shows all Entra claims

---

## Open Questions

1. Should RoleMappingConfiguration be seeded in `DbSeeder.cs` or manually configured post-deployment?
2. Should "Sign in with Microsoft" be available on public login or restricted to authorized users?
3. Should Graph API failures gracefully degrade the profile display?
4. Should we block auto-provisioning of sensitive roles (Admin, SuperAdmin)?
5. Should RoleMappingManager.razor be accessible to Admin role or a separate "RoleManager" role?

---

## Success Criteria

- [x] Core Entra OIDC authentication works
- [ ] Entra users are provisioned with correct local roles
- [ ] Permission claims are loaded from both local and Entra roles
- [ ] AuthStateProbe displays all Entra-specific claims
- [ ] BFF API pages (weather, users, reports) work with Entra users
- [ ] Graph API profile data displays on Profile.razor
- [ ] RoleMappingManager.razor allows runtime configuration
- [ ] Integration tests cover all authentication flows (local + Entra)

---

## Related Files

- [Program.cs](c:\Workplace\Demo\dotnet10-demo\demo4\Demo4.EntraIntegration\Program.cs)
- [ApplicationDbContext.cs](c:\Workplace\Demo\dotnet10-demo\demo4\Demo4.EntraIntegration\Data\ApplicationDbContext.cs)
- [PermissionClaimsTransformation.cs](c:\Workplace\Demo\dotnet10-demo\demo4\Demo4.EntraIntegration\Authorization\PermissionClaimsTransformation.cs)
- [EntraUserProvisioningService.cs](c:\Workplace\Demo\dotnet10-demo\demo4\Demo4.EntraIntegration\Services\EntraUserProvisioningService.cs)
- [GraphService.cs](c:\Workplace\Demo\dotnet10-demo\demo4\Demo4.EntraIntegration\Services\GraphService.cs)

---

## Resolution Notes

_To be updated as implementation progresses._

**Phase 1 Status:** Not started  
**Phase 2 Status:** Not started  
**Phase 3 Status:** Not started  

