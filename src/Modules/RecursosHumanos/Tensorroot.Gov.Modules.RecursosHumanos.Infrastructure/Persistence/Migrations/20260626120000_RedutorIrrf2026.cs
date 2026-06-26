using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// RH-M10: adiciona o redutor mensal do IRRF (Lei 15.270/2025, vigencia 2026) como value object owned
    /// opcional da TabelaIrrf. Quatro colunas NULAS (coeficientes legais parametrizados por exercicio); as
    /// competencias anteriores ficam com redutor nulo, preservando o calculo mensal existente.
    /// </summary>
    /// <inheritdoc />
    public partial class RedutorIrrf2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RedutorCoeficienteBase",
                schema: "recursoshumanos",
                table: "TabelasIrrf",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedutorCoeficienteRendimento",
                schema: "recursoshumanos",
                table: "TabelasIrrf",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedutorLimiteRendimento",
                schema: "recursoshumanos",
                table: "TabelasIrrf",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedutorTeto",
                schema: "recursoshumanos",
                table: "TabelasIrrf",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedutorCoeficienteBase",
                schema: "recursoshumanos",
                table: "TabelasIrrf");

            migrationBuilder.DropColumn(
                name: "RedutorCoeficienteRendimento",
                schema: "recursoshumanos",
                table: "TabelasIrrf");

            migrationBuilder.DropColumn(
                name: "RedutorLimiteRendimento",
                schema: "recursoshumanos",
                table: "TabelasIrrf");

            migrationBuilder.DropColumn(
                name: "RedutorTeto",
                schema: "recursoshumanos",
                table: "TabelasIrrf");
        }
    }
}
