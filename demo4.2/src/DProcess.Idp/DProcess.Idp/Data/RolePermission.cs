namespace DProcess.Idp.Data;

public class RolePermission
{
    public string RoleId { get; set; } = default!;
    public virtual ApplicationRole Role { get; set; } = default!;

    public int PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = default!;
}
