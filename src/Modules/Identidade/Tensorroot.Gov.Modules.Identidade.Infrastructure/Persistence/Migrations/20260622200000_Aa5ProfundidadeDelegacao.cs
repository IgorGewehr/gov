using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Aa5ProfundidadeDelegacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AA-5/D3: profundidade da cadeia de (sub)delegacao por atribuicao de papel. Default 0
            // (= direta) cobre as linhas legadas; novas delegacoes herdam profundidade do concedente + 1.
            migrationBuilder.AddColumn<int>(
                name: "ProfundidadeDelegacao",
                schema: "identidade",
                table: "AtribuicoesPapel",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfundidadeDelegacao",
                schema: "identidade",
                table: "AtribuicoesPapel");
        }
    }
}
