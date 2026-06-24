using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "identidade",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "identidade",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "identidade",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "identidade",
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
                schema: "identidade",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "identidade",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "identidade",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "identidade",
                table: "AuditTrail");
        }
    }
}
