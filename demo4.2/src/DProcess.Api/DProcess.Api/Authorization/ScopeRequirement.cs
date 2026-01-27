using Microsoft.AspNetCore.Authorization;

namespace DProcess.Api.Authorization;

public class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}
