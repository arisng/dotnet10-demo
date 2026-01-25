namespace SaaS.Frontend.Data;

public class RoleMappingConfiguration
{
    public int Id { get; set; }
    public required string EntraAppRoleValue { get; set; }  // "GlobalAdmin"
    public required string LocalRoleName { get; set; }      // "Admin"
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}