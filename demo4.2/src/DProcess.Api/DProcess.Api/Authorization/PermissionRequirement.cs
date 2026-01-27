using Microsoft.AspNetCore.Authorization;

namespace DProcess.Api.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
