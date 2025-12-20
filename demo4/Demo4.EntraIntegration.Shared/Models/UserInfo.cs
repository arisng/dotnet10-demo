namespace Demo4.EntraIntegration.Shared.Models;

public class UserInfo
{
    public required string UserId { get; set; }
    public required string Email { get; set; }

    // "local" (ASP.NET Core Identity) or "entra" (Microsoft Entra ID)
    public string? AuthProvider { get; set; }

    // Present for Entra users (copied from oid/tid claims)
    public string? EntraObjectId { get; set; }
    public string? EntraTenantId { get; set; }

    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
