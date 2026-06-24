using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvenioPcParcialNumeroEtapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroEtapa",
                schema: "convenios",
                table: "ConveniosRecebidosPrestacoes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroEtapa",
                schema: "convenios",
                table: "ConveniosRecebidosPrestacoes");
        }
    }
}
