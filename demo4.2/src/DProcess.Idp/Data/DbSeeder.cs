using DProcess.Shared.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DProcess.Idp.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Apply pending migrations before seeding
        await context.Database.MigrateAsync();

        string[] roles = ["Admin", "Manager", "User"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        var permissions = new Dictionary<string, string[]>
        {
            {
                "Admin",
                [
                    PermissionNames.WeatherRead,
                    PermissionNames.WeatherWrite,
                    PermissionNames.UsersRead,
                    PermissionNames.UsersWrite,
                    PermissionNames.UsersDelete,
                    PermissionNames.ReportsView,
                    PermissionNames.ReportsExport
                ]
            },
            {
                "Manager",
                [
                    PermissionNames.WeatherRead,
                    PermissionNames.WeatherWrite,
                    PermissionNames.UsersRead,
                    PermissionNames.ReportsView,
                    PermissionNames.ReportsExport
                ]
            },
            {
                "User",
                [
                    PermissionNames.WeatherRead,
                    PermissionNames.ReportsView
                ]
            }
        };

        var allPermissions = permissions.Values.SelectMany(x => x).Distinct(StringComparer.Ordinal);
        foreach (var permName in allPermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Name == permName))
            {
                context.Permissions.Add(new Permission { Name = permName });
            }
        }
        await context.SaveChangesAsync();

        foreach (var roleName in permissions.Keys)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var rolePerms = permissions[roleName];
            foreach (var permName in rolePerms)
            {
                var perm = await context.Permissions.FirstAsync(p => p.Name == permName);
                if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == perm.Id))
                {
                    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                }
            }
        }
        await context.SaveChangesAsync();

        var users = new[]
        {
            (Email: "admin@local.app", Pwd: "Admin123!", Role: "Admin"),
            (Email: "manager@local.app", Pwd: "Manager123!", Role: "Manager"),
            (Email: "user@local.app", Pwd: "User123!", Role: "User")
        };

        foreach (var (email, pwd, role) in users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, pwd);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
            else if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
