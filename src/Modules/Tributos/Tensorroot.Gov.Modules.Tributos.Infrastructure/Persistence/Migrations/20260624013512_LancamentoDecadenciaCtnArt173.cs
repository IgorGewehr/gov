using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LancamentoDecadenciaCtnArt173 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnosDecadencia",
                schema: "tributos",
                table: "Lancamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataConstituicao",
                schema: "tributos",
                table: "Lancamentos",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFatoGerador",
                schema: "tributos",
                table: "Lancamentos",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnosDecadencia",
                schema: "tributos",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "DataConstituicao",
                schema: "tributos",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "DataFatoGerador",
                schema: "tributos",
                table: "Lancamentos");
        }
    }
}
