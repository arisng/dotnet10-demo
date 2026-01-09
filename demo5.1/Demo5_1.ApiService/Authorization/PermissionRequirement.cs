using Microsoft.AspNetCore.Authorization;

namespace Demo5_1.ApiService.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
