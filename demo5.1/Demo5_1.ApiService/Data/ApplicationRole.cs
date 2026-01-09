using Microsoft.AspNetCore.Identity;

namespace Demo5_1.ApiService.Data;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
