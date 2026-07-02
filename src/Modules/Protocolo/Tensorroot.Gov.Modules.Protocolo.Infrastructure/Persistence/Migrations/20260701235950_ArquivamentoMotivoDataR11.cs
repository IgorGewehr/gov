using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArquivamentoMotivoDataR11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataArquivamento",
                schema: "protocolo",
                table: "Processos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoArquivamento",
                schema: "protocolo",
                table: "Processos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataArquivamento",
                schema: "protocolo",
                table: "Processos");

            migrationBuilder.DropColumn(
                name: "MotivoArquivamento",
                schema: "protocolo",
                table: "Processos");
        }
    }
}
