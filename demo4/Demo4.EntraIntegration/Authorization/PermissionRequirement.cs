using Microsoft.AspNetCore.Authorization;

namespace Demo4.EntraIntegration.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
