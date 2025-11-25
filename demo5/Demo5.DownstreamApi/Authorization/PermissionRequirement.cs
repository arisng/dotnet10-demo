using Microsoft.AspNetCore.Authorization;

namespace Demo5.DownstreamApi.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
