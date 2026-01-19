using Demo4.EntraIntegration.Data;
using Demo4.EntraIntegration.Client.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Demo4.EntraIntegration.Services;

/// <summary>
/// Service responsible for auto-provisioning Entra ID users during OIDC authentication.
/// This runs in OnTokenValidated event to create local user records for first-time Entra users.
/// </summary>
public interface IEntraUserProvisioningService
{
    Task<ApplicationUser> ProvisionUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public class EntraUserProvisioningService : IEntraUserProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IGraphService _graphService;
    private readonly ILogger<EntraUserProvisioningService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EntraUserProvisioningService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        IGraphService graphService,
        ILogger<EntraUserProvisioningService> logger,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _graphService = graphService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<ApplicationUser> ProvisionUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var oid = principal.GetObjectId();
        if (string.IsNullOrEmpty(oid))
        {
            throw new InvalidOperationException("Cannot provision user: Entra Object ID (oid) claim is missing");
        }

        _logger.LogInformation("Starting Entra user provisioning for OID: {Oid}", oid);

        // Check if user already exists (idempotency + race condition protection)
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == oid, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogInformation("Entra user already exists: {Email} (ID: {UserId})", existingUser.Email, existingUser.Id);
            
            // Ensure external login exists (handles partial failure recovery)
            await EnsureExternalLoginExistsAsync(existingUser, principal, cancellationToken);
            
            // Update profile on each login
            await UpdateUserProfileAsync(existingUser, principal, cancellationToken);

            // Ensure key Entra claims are persisted as Identity user claims (durable across requests)
            await EnsureDurableEntraClaimsAsync(existingUser, principal);
            
            return existingUser;
        }

        // Create new user
        var user = await CreateUserAsync(principal, oid, cancellationToken);

        // Ensure key Entra claims are persisted as Identity user claims (durable across requests)
        await EnsureDurableEntraClaimsAsync(user, principal);

        _logger.LogInformation("Successfully provisioned Entra user: {Email} (ID: {UserId})", user.Email, user.Id);

        return user;
    }

    private async Task EnsureDurableEntraClaimsAsync(ApplicationUser user, ClaimsPrincipal principal)
    {
        var oid = principal.GetObjectId() ?? user.EntraObjectId;
        var tid = principal.FindFirstValue("tid")
                  ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

        // Microsoft.Identity.Web relies on an account identifier (msal_account_id) and/or login hint
        // to locate the user's MSAL account in the token cache.
        var preferredUsername = principal.FindFirstValue("preferred_username")
                                ?? principal.FindFirstValue(ClaimTypes.Upn)
                                ?? principal.FindFirstValue(ClaimTypes.Email)
                                ?? user.Email;

        var msalAccountId = principal.GetMsalAccountId();
        if (string.IsNullOrWhiteSpace(msalAccountId) && !string.IsNullOrWhiteSpace(oid) && !string.IsNullOrWhiteSpace(tid))
        {
            msalAccountId = $"{oid}.{tid}";
        }

        var desired = new List<Claim>
        {
            new("auth_provider", "entra")
        };

        if (!string.IsNullOrWhiteSpace(oid)) desired.Add(new Claim("oid", oid));
        if (!string.IsNullOrWhiteSpace(tid)) desired.Add(new Claim("tid", tid));
        if (!string.IsNullOrWhiteSpace(preferredUsername)) desired.Add(new Claim("preferred_username", preferredUsername));
        if (!string.IsNullOrWhiteSpace(msalAccountId)) desired.Add(new Claim("msal_account_id", msalAccountId));

        var existing = await _userManager.GetClaimsAsync(user);

        foreach (var claim in desired)
        {
            if (existing.Any(c => c.Type == claim.Type && c.Value == claim.Value))
            {
                continue;
            }

            // If the claim type exists with a different value, replace it.
            var toRemove = existing.Where(c => c.Type == claim.Type).ToList();
            foreach (var old in toRemove)
            {
                await _userManager.RemoveClaimAsync(user, old);
            }

            await _userManager.AddClaimAsync(user, claim);
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(ClaimsPrincipal principal, string oid, CancellationToken cancellationToken)
    {
        var email = principal.FindFirstValue("preferred_username")
                    ?? principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name");
        var nameIdentifier = principal.GetNameIdentifierId();

        if (string.IsNullOrEmpty(nameIdentifier))
        {
            throw new InvalidOperationException("Cannot create user: NameIdentifier claim is missing");
        }

        var user = new ApplicationUser
        {
            UserName = email ?? $"entra_{oid}",
            Email = email,
            EmailConfirmed = true, // Trust Entra email verification
            ExternalAuthenticationProvider = "MicrosoftEntra",
            EntraObjectId = oid,
            DisplayName = name
        };

        // Step 1: Create user
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create Entra user: {Errors}", errors);
            throw new InvalidOperationException($"Failed to create Entra user: {errors}");
        }

        _logger.LogInformation("Created user record for Entra user: {Email}", user.Email);

        try
        {
            // Step 2: Add external login (critical for ExternalLoginSignInAsync to work)
            var addLoginResult = await _userManager.AddLoginAsync(user,
                new UserLoginInfo("MicrosoftEntra", nameIdentifier, "Microsoft Entra ID"));

            if (!addLoginResult.Succeeded)
            {
                var loginErrors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to add external login for {Email}: {Errors}", user.Email, loginErrors);

                // Rollback: delete the user to maintain consistency
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException($"Failed to link external login: {loginErrors}");
            }

            _logger.LogInformation("Added external login for Entra user: {Email}", user.Email);

            // Step 3: Sync roles from Entra
            await SyncUserRolesAsync(user, principal, cancellationToken);

            // Step 4: Update profile from Graph API
            await UpdateUserProfileAsync(user, principal, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Unexpected error - rollback user creation
            _logger.LogError(ex, "Unexpected error during user provisioning for {Email}, rolling back", user.Email);
            await _userManager.DeleteAsync(user);
            throw;
        }

        return user;
    }

    private async Task EnsureExternalLoginExistsAsync(ApplicationUser user, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider == "MicrosoftEntra"))
        {
            return; // Login already exists
        }

        _logger.LogWarning("External login missing for existing user {Email}, adding now", user.Email);

        var nameIdentifier = principal.GetNameIdentifierId();
        if (string.IsNullOrEmpty(nameIdentifier))
        {
            _logger.LogError("Cannot add external login: NameIdentifier claim is missing");
            return;
        }

        var result = await _userManager.AddLoginAsync(user,
            new UserLoginInfo("MicrosoftEntra", nameIdentifier, "Microsoft Entra ID"));

        if (result.Succeeded)
        {
            _logger.LogInformation("Added missing external login for user {Email}", user.Email);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to add missing external login for {Email}: {Errors}", user.Email, errors);
        }
    }

    private async Task SyncUserRolesAsync(ApplicationUser user, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var entraRoles = principal.FindAll("roles").Select(c => c.Value).ToList();

        if (!entraRoles.Any())
        {
            // Assign default "User" role
            await EnsureUserHasDefaultRoleAsync(user, cancellationToken);
            return;
        }

        _logger.LogInformation("Syncing {Count} Entra roles for user {Email}: {Roles}", 
            entraRoles.Count, user.Email, string.Join(", ", entraRoles));

        foreach (var entraRole in entraRoles)
        {
            // Look up role mapping from database
            var mapping = await _context.RoleMappingConfigurations
                .FirstOrDefaultAsync(rmc => rmc.EntraAppRoleValue == entraRole, cancellationToken);

            string localRoleName;
            if (mapping != null)
            {
                localRoleName = mapping.LocalRoleName;
                _logger.LogDebug("Mapped Entra role '{EntraRole}' to local role '{LocalRole}' for user {Email}", 
                    entraRole, localRoleName, user.Email);
            }
            else
            {
                // No mapping found - log warning and skip
                _logger.LogWarning("No role mapping found for Entra role '{EntraRole}' for user {Email}. Skipping role assignment.", 
                    entraRole, user.Email);
                continue;
            }

            // Security check: skip sensitive roles that shouldn't be auto-assigned
            if (IsSensitiveRole(localRoleName))
            {
                _logger.LogWarning("Skipping sensitive local role '{LocalRole}' (mapped from '{EntraRole}') for user {Email}", 
                    localRoleName, entraRole, user.Email);
                continue;
            }

            // Ensure local role exists
            if (!await _roleManager.RoleExistsAsync(localRoleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new ApplicationRole(localRoleName));
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError("Failed to create local role '{LocalRole}' (mapped from '{EntraRole}'): {Errors}",
                        localRoleName, entraRole, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                    continue;
                }

                _logger.LogInformation("Created new local role: {LocalRole} (mapped from Entra role '{EntraRole}')", 
                    localRoleName, entraRole);
            }

            // Add user to local role
            if (!await _userManager.IsInRoleAsync(user, localRoleName))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, localRoleName);
                if (!addRoleResult.Succeeded)
                {
                    _logger.LogError("Failed to add user {Email} to local role '{LocalRole}' (mapped from '{EntraRole}'): {Errors}",
                        user.Email, localRoleName, entraRole, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    _logger.LogInformation("Added user {Email} to local role: {LocalRole} (mapped from Entra role '{EntraRole}')", 
                        user.Email, localRoleName, entraRole);
                }
            }
            else
            {
                _logger.LogDebug("User {Email} already has local role: {LocalRole}", user.Email, localRoleName);
            }
        }
    }

    private async Task EnsureUserHasDefaultRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string defaultRole = "User";

        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            var createResult = await _roleManager.CreateAsync(new ApplicationRole(defaultRole));
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create default role '{Role}': {Errors}",
                    defaultRole, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, defaultRole);
        if (addResult.Succeeded)
        {
            _logger.LogInformation("Assigned default role '{Role}' to user {Email}", defaultRole, user.Email);
        }
        else
        {
            _logger.LogError("Failed to assign default role to {Email}: {Errors}",
                user.Email, string.Join(", ", addResult.Errors.Select(e => e.Description)));
        }
    }

    private async Task UpdateUserProfileAsync(ApplicationUser user, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        try
        {
            // Sync extended profile from Microsoft Graph API
            // This method internally updates DisplayName, JobTitle, Dept, etc. from Graph data.
            await _graphService.SyncUserProfileToLocalAsync(user.Id);
        }
        catch (Exception ex)
        {
            // Fallback: if Graph sync fails, at least try to update basic DisplayName from the token
            var name = principal.FindFirstValue("name");
            if (!string.IsNullOrEmpty(name) && user.DisplayName != name)
            {
                user.DisplayName = name;
                await _userManager.UpdateAsync(user);
            }

            // Non-fatal: continue without Graph data
            _logger.LogWarning(ex, "Failed to update Graph profile for user {Email}, continuing without extended profile", user.Email);
        }
    }

    private static bool IsSensitiveRole(string roleName)
    {
        // Prevent auto-creation of privileged roles
        var sensitiveRoles = new[] { "Admin", "Administrator", "SuperAdmin", "SystemAdmin" };
        return sensitiveRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
}
