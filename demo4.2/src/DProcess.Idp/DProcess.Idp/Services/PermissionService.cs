using DProcess.Idp.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DProcess.Idp.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(string userId);
}

public sealed class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext context;

    public PermissionService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(string userId)
    {
        var roleIds = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var permissions = await context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }
}
