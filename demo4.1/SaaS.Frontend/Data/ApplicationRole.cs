using Microsoft.AspNetCore.Identity;

namespace SaaS.Frontend.Data;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
