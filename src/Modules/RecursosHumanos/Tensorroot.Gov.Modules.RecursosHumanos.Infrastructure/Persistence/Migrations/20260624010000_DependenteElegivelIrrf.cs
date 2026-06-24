using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// RH-D1: adiciona a flag de elegibilidade fiscal de IRRF ao dependente (Lei 9.250/1995 art. 35).
    /// Apenas dependentes elegiveis compoem a deducao por dependente na base do IRRF; o valor padrao
    /// e <c>false</c> (fail-closed: nao se deduz sem decisao explicita de elegibilidade), o que evita
    /// deducao indevida e sub-recolhimento de IRRF retido na fonte na folha existente.
    /// </summary>
    /// <inheritdoc />
    public partial class DependenteElegivelIrrf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ElegivelIrrf",
                schema: "recursoshumanos",
                table: "ServidoresDependentes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElegivelIrrf",
                schema: "recursoshumanos",
                table: "ServidoresDependentes");
        }
    }
}
