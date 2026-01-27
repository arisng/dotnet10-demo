using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DProcess.Idp.Data;

public class ApplicationRole : IdentityRole
{
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
