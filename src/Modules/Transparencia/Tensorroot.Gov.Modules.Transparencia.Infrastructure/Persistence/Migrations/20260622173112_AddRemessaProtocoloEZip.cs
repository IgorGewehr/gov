using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRemessaProtocoloEZip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeArquivoZip",
                schema: "transparencia",
                table: "RemessasTce",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocoloTce",
                schema: "transparencia",
                table: "RemessasTce",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeArquivoZip",
                schema: "transparencia",
                table: "RemessasTce");

            migrationBuilder.DropColumn(
                name: "ProtocoloTce",
                schema: "transparencia",
                table: "RemessasTce");
        }
    }
}
