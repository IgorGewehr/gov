using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrescricaoIntercorrenteESuspensaoR5Ctn151 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CausaSuspensao",
                schema: "tributos",
                table: "DividasAtivas",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataArquivamentoExecucao",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataSuspensaoExecucao",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataSuspensaoExigibilidade",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasPrescricaoSuspensos",
                schema: "tributos",
                table: "DividasAtivas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CausaSuspensao",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(
                name: "DataArquivamentoExecucao",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(
                name: "DataSuspensaoExecucao",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(
                name: "DataSuspensaoExigibilidade",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(
                name: "DiasPrescricaoSuspensos",
                schema: "tributos",
                table: "DividasAtivas");
        }
    }
}
