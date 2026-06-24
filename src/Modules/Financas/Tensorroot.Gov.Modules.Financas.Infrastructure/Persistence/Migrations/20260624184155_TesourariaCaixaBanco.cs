using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TesourariaCaixaBanco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContasFinanceiras",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Agencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContaNumero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Pix = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasFinanceiras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosFinanceiros",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoApos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Historico = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    OrigemReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContraparteContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Conciliado = table.Column<bool>(type: "bit", nullable: false),
                    DataConciliacao = table.Column<DateOnly>(type: "date", nullable: true),
                    ContaFinanceiraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosFinanceiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosFinanceiros_ContasFinanceiras_ContaFinanceiraId",
                        column: x => x.ContaFinanceiraId,
                        principalSchema: "financas",
                        principalTable: "ContasFinanceiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasFinanceiras_TenantId_Nome",
                schema: "financas",
                table: "ContasFinanceiras",
                columns: new[] { "TenantId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosFinanceiros_ContaFinanceiraId",
                schema: "financas",
                table: "MovimentosFinanceiros",
                column: "ContaFinanceiraId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosFinanceiros_Data",
                schema: "financas",
                table: "MovimentosFinanceiros",
                column: "Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosFinanceiros",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "ContasFinanceiras",
                schema: "financas");
        }
    }
}
