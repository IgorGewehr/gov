using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InteressadoDocumento",
                schema: "protocolo",
                table: "Processos",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "protocolo",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "protocolo",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "protocolo",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Processos_TenantId_InteressadoDocumento",
                schema: "protocolo",
                table: "Processos",
                columns: new[] { "TenantId", "InteressadoDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "protocolo",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Processos_TenantId_InteressadoDocumento",
                schema: "protocolo",
                table: "Processos");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "protocolo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "InteressadoDocumento",
                schema: "protocolo",
                table: "Processos");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "protocolo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "protocolo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "protocolo",
                table: "AuditTrail");
        }
    }
}
