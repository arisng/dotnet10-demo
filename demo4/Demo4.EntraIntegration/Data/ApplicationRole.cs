using Microsoft.AspNetCore.Identity;

namespace Demo4.EntraIntegration.Data;

public class ApplicationRole : IdentityRole
{
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
