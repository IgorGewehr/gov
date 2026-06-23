using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VinculoServidorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VinculosServidorUsuario",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosServidorUsuario", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VinculosServidorUsuario_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "VinculosServidorUsuario",
                columns: new[] { "TenantId", "ServidorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinculosServidorUsuario_TenantId_UsuarioId",
                schema: "recursoshumanos",
                table: "VinculosServidorUsuario",
                columns: new[] { "TenantId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VinculosServidorUsuario",
                schema: "recursoshumanos");
        }
    }
}
