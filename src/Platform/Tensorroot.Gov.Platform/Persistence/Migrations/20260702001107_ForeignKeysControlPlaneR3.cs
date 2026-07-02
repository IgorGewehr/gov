using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Platform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ForeignKeysControlPlaneR3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UsuariosTenantIndex_TenantId",
                schema: "plataforma",
                table: "UsuariosTenantIndex",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantModules_Tenants_TenantId",
                schema: "plataforma",
                table: "TenantModules",
                column: "TenantId",
                principalSchema: "plataforma",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosTenantIndex_Tenants_TenantId",
                schema: "plataforma",
                table: "UsuariosTenantIndex",
                column: "TenantId",
                principalSchema: "plataforma",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantModules_Tenants_TenantId",
                schema: "plataforma",
                table: "TenantModules");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosTenantIndex_Tenants_TenantId",
                schema: "plataforma",
                table: "UsuariosTenantIndex");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosTenantIndex_TenantId",
                schema: "plataforma",
                table: "UsuariosTenantIndex");
        }
    }
}
