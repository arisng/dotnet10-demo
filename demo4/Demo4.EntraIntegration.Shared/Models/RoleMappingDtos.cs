namespace Demo4.EntraIntegration.Shared.Models;

public record CreateRoleMappingDto
{
    public required string EntraAppRoleValue { get; init; }
    public required string LocalRoleName { get; init; }
    public string? Notes { get; init; }
}

public record UpdateRoleMappingDto
{
    public required string EntraAppRoleValue { get; init; }
    public required string LocalRoleName { get; init; }
    public string? Notes { get; init; }
}