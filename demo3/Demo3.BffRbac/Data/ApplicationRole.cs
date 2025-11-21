using Microsoft.AspNetCore.Identity;

namespace Demo3.BffRbac.Data;

public class ApplicationRole : IdentityRole
{
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
