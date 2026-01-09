using Demo5_1.ApiService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Demo5_1.ApiService.Services;

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
    private readonly ILogger<EntraUserProvisioningService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EntraUserProvisioningService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<EntraUserProvisioningService> logger,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        _roleManager = roleManager;
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
            
            return existingUser;
        }

        // Create new user
        var user = await CreateUserAsync(principal, oid, cancellationToken);

        _logger.LogInformation("Successfully provisioned Entra user: {Email} (ID: {UserId})", user.Email, user.Id);

        return user;
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

        _logger.LogInformation("Syncing {Count} Entra roles for user {Email}", entraRoles.Count, user.Email);

        foreach (var roleName in entraRoles)
        {
            // Security check: skip sensitive roles that shouldn't be auto-created
            if (IsSensitiveRole(roleName))
            {
                _logger.LogWarning("Skipping sensitive role '{Role}' for user {Email}", roleName, user.Email);
                continue;
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new ApplicationRole(roleName));
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError("Failed to create role '{Role}': {Errors}",
                        roleName, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                    continue;
                }

                _logger.LogInformation("Created new role: {Role}", roleName);
            }

            // Add user to role
            var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addRoleResult.Succeeded)
            {
                _logger.LogError("Failed to add user {Email} to role '{Role}': {Errors}",
                    user.Email, roleName, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                _logger.LogInformation("Added user {Email} to role: {Role}", user.Email, roleName);
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
            // Update basic claims from token
            var name = principal.FindFirstValue("name");
            if (!string.IsNullOrEmpty(name) && user.DisplayName != name)
            {
                user.DisplayName = name;
            }

            // Fetch extended profile from Graph API
            using var scope = _serviceProvider.CreateScope();
            var graphService = scope.ServiceProvider.GetRequiredService<IGraphService>();

            var profile = await graphService.GetUserProfileAsync();
            if (profile != null)
            {
                user.DisplayName = profile.DisplayName ?? user.DisplayName;
                user.JobTitle = profile.JobTitle;

                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Updated Graph profile for user {Email}: {DisplayName}, {JobTitle}",
                    user.Email, profile.DisplayName, profile.JobTitle);
            }
        }
        catch (Exception ex)
        {
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
