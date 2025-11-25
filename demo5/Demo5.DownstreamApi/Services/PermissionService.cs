using Demo5.DownstreamApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Demo5.DownstreamApi.Services;

public interface IPermissionService
{
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
}

public class PermissionService(ApplicationDbContext context) : IPermissionService
{
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        // 1. Get user's roles
        var userRoles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (userRoles.Count == 0) return [];

        // 2. Get permissions for those roles
        var permissions = await context.RolePermissions
            .Where(rp => userRoles.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }
}
