using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Frontend.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleMappingConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleMappingConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntraAppRoleValue = table.Column<string>(type: "TEXT", nullable: false),
                    LocalRoleName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMappingConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleMappingConfigurations_EntraAppRoleValue",
                table: "RoleMappingConfigurations",
                column: "EntraAppRoleValue",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMappingConfigurations");
        }
    }
}
