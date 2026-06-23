using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FolhaCicloAnual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FolhasDePagamento_TenantId_Competencia",
                schema: "recursoshumanos",
                table: "FolhasDePagamento");

            migrationBuilder.AddColumn<bool>(
                name: "BaseSeparada",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill aditivo: folhas pre-existentes sao MENSAIS (design §1.1). O defaultValue so vale
            // como semente do backfill desta migration; o modelo nao tem default (o dominio sempre informa
            // o Tipo no ctor). nvarchar(20) casa com a conversao enum->string ("Mensal"/"DecimoTerceiro"/...).
            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mensal");

            migrationBuilder.CreateIndex(
                name: "IX_FolhasDePagamento_TenantId_Competencia_Tipo",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                columns: new[] { "TenantId", "Competencia", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FolhasDePagamento_TenantId_Competencia_Tipo",
                schema: "recursoshumanos",
                table: "FolhasDePagamento");

            migrationBuilder.DropColumn(
                name: "BaseSeparada",
                schema: "recursoshumanos",
                table: "FolhasDePagamento");

            migrationBuilder.DropColumn(
                name: "Tipo",
                schema: "recursoshumanos",
                table: "FolhasDePagamento");

            migrationBuilder.CreateIndex(
                name: "IX_FolhasDePagamento_TenantId_Competencia",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                columns: new[] { "TenantId", "Competencia" },
                unique: true);
        }
    }
}
