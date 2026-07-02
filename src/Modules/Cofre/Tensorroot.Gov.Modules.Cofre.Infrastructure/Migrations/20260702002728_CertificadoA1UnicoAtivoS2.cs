using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CertificadoA1UnicoAtivoS2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificadosA1_TenantId_Status",
                schema: "cofre",
                table: "CertificadosA1");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosA1_TenantId_Status",
                schema: "cofre",
                table: "CertificadosA1",
                columns: new[] { "TenantId", "Status" },
                unique: true,
                filter: "[Status] = 'Ativo'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificadosA1_TenantId_Status",
                schema: "cofre",
                table: "CertificadosA1");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosA1_TenantId_Status",
                schema: "cofre",
                table: "CertificadosA1",
                columns: new[] { "TenantId", "Status" });
        }
    }
}
