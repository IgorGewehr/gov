using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "painelgestor",
                table: "AuditTrail");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "painelgestor",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "painelgestor",
                table: "AuditTrail");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "painelgestor",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });
        }
    }
}
