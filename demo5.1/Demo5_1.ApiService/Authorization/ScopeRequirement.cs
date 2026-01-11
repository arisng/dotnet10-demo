using Microsoft.AspNetCore.Authorization;

namespace Demo5_1.ApiService.Authorization;

public class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}
