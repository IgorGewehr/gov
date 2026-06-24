using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PG1DespesaPessoalMensalJanela12Meses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndicadoresMunicipioDespesaPessoalMensal",
                schema: "painelgestor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IndicadorMunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicadoresMunicipioDespesaPessoalMensal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicadoresMunicipioDespesaPessoalMensal_IndicadoresMunicipio_IndicadorMunicipioId",
                        column: x => x.IndicadorMunicipioId,
                        principalSchema: "painelgestor",
                        principalTable: "IndicadoresMunicipio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndicadoresMunicipioDespesaPessoalMensal_IndicadorMunicipioId_Mes",
                schema: "painelgestor",
                table: "IndicadoresMunicipioDespesaPessoalMensal",
                columns: new[] { "IndicadorMunicipioId", "Mes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicadoresMunicipioDespesaPessoalMensal",
                schema: "painelgestor");
        }
    }
}
