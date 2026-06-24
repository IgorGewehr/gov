using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W91PncpPrazoEficacia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataAssinatura",
                schema: "administracao",
                table: "Contratos",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "PrazoPublicacaoPncp",
                schema: "administracao",
                table: "Contratos",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicacaoPncpVencida",
                schema: "administracao",
                table: "Contratos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataAssinatura",
                schema: "administracao",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PrazoPublicacaoPncp",
                schema: "administracao",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PublicacaoPncpVencida",
                schema: "administracao",
                table: "Contratos");
        }
    }
}
