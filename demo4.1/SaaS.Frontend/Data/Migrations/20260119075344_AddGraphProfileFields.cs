using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Frontend.Migrations
{
    /// <inheritdoc />
    public partial class AddGraphProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastGraphSync",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastGraphSync",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "AspNetUsers");
        }
    }
}
