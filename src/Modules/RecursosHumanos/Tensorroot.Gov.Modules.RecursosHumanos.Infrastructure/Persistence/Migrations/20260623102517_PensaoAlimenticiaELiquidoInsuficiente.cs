using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PensaoAlimenticiaELiquidoInsuficiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TemLiquidoInsuficiente",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ServidoresPensoesAlimenticias",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Beneficiario = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(7,6)", precision: 7, scale: 6, nullable: false),
                    BaseIncidencia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValorFixo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProcessoJudicial = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServidoresPensoesAlimenticias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServidoresPensoesAlimenticias_Servidores_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "recursoshumanos",
                        principalTable: "Servidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServidoresPensoesAlimenticias_ServidorId",
                schema: "recursoshumanos",
                table: "ServidoresPensoesAlimenticias",
                column: "ServidorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServidoresPensoesAlimenticias",
                schema: "recursoshumanos");

            migrationBuilder.DropColumn(
                name: "TemLiquidoInsuficiente",
                schema: "recursoshumanos",
                table: "FolhasDePagamento");
        }
    }
}
