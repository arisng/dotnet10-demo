using Microsoft.AspNetCore.Identity;

namespace Demo4.EntraIntegration.Data;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
