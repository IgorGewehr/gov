using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Platform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioTenantIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuariosTenantIndex",
                schema: "plataforma",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosTenantIndex", x => x.Email);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosTenantIndex",
                schema: "plataforma");
        }
    }
}
