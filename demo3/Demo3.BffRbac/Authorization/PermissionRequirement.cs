using Microsoft.AspNetCore.Authorization;

namespace Demo3.BffRbac.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
