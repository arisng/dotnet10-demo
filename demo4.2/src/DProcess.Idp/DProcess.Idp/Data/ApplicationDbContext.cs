using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DProcess.Idp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
	public DbSet<Permission> Permissions { get; set; } = default!;
	public DbSet<RolePermission> RolePermissions { get; set; } = default!;

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<Permission>()
			.HasIndex(p => p.Name)
			.IsUnique();

		builder.Entity<RolePermission>()
			.HasKey(rp => new { rp.RoleId, rp.PermissionId });

		builder.Entity<RolePermission>()
			.HasOne(rp => rp.Role)
			.WithMany(r => r.RolePermissions)
			.HasForeignKey(rp => rp.RoleId);

		builder.Entity<RolePermission>()
			.HasOne(rp => rp.Permission)
			.WithMany()
			.HasForeignKey(rp => rp.PermissionId);
	}
}
