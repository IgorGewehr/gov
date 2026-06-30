using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DecadenciaIssHomologacaoR4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoloFraudeSimulacao",
                schema: "tributos",
                table: "Lancamentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HouvePagamentoAntecipado",
                schema: "tributos",
                table: "Lancamentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoLancamento",
                schema: "tributos",
                table: "Lancamentos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Oficio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoloFraudeSimulacao",
                schema: "tributos",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "HouvePagamentoAntecipado",
                schema: "tributos",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "TipoLancamento",
                schema: "tributos",
                table: "Lancamentos");
        }
    }
}
