using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inventarios",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Setor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Portaria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataAbertura = table.Column<DateOnly>(type: "date", nullable: false),
                    DataEncerramento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventariosComissao",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Presidente = table.Column<bool>(type: "bit", nullable: false),
                    InventarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosComissao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosComissao_Inventarios_InventarioId",
                        column: x => x.InventarioId,
                        principalSchema: "patrimonio",
                        principalTable: "Inventarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventariosItens",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroTombamento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DescricaoSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocalizacaoEsperada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValorContabilSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SituacaoEncontrada = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LocalizacaoEncontrada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Contado = table.Column<bool>(type: "bit", nullable: false),
                    InventarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosItens_Inventarios_InventarioId",
                        column: x => x.InventarioId,
                        principalSchema: "patrimonio",
                        principalTable: "Inventarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventariosDivergencias",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Recomendacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InventarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosDivergencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosDivergencias_Inventarios_InventarioId",
                        column: x => x.InventarioId,
                        principalSchema: "patrimonio",
                        principalTable: "Inventarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventarios_TenantId_Exercicio_Tipo_Setor",
                schema: "patrimonio",
                table: "Inventarios",
                columns: new[] { "TenantId", "Exercicio", "Tipo", "Setor" });

            migrationBuilder.CreateIndex(
                name: "IX_InventariosComissao_InventarioId",
                schema: "patrimonio",
                table: "InventariosComissao",
                column: "InventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosItens_InventarioId",
                schema: "patrimonio",
                table: "InventariosItens",
                column: "InventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosDivergencias_InventarioId",
                schema: "patrimonio",
                table: "InventariosDivergencias",
                column: "InventarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventariosComissao",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "InventariosDivergencias",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "InventariosItens",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Inventarios",
                schema: "patrimonio");
        }
    }
}
