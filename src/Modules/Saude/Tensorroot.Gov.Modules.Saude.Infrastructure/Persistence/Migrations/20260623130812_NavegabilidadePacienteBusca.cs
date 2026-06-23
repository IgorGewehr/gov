using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NavegabilidadePacienteBusca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpfBusca",
                schema: "saude",
                table: "Pacientes",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeBusca",
                schema: "saude",
                table: "Pacientes",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "saude",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "saude",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "saude",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TenantId_CpfBusca",
                schema: "saude",
                table: "Pacientes",
                columns: new[] { "TenantId", "CpfBusca" });

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TenantId_NomeBusca",
                schema: "saude",
                table: "Pacientes",
                columns: new[] { "TenantId", "NomeBusca" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pacientes_TenantId_CpfBusca",
                schema: "saude",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_TenantId_NomeBusca",
                schema: "saude",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "CpfBusca",
                schema: "saude",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NomeBusca",
                schema: "saude",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "saude",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "saude",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "saude",
                table: "AuditTrail");
        }
    }
}
