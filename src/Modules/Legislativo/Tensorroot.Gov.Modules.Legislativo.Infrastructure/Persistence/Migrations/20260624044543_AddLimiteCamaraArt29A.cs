using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLimiteCamaraArt29A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApuracoesArt29A",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Populacao = table.Column<int>(type: "int", nullable: false),
                    BaseExercicioReferencia = table.Column<int>(type: "int", nullable: false),
                    BaseReceitaTributaria = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseTransferencias = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RepasseRecebido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ParamPercentualFaixa = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ParamSubtetoFolha = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ParamLimiarAtencao = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ParamExercicioCorteInativos = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuracoesArt29A", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApuracoesArt29ADespesas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApuracaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuracoesArt29ADespesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApuracoesArt29ADespesas_ApuracoesArt29A_ApuracaoOwnerId",
                        column: x => x.ApuracaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "ApuracoesArt29A",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesArt29A_TenantId_Exercicio",
                schema: "legislativo",
                table: "ApuracoesArt29A",
                columns: new[] { "TenantId", "Exercicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesArt29ADespesas_ApuracaoOwnerId",
                schema: "legislativo",
                table: "ApuracoesArt29ADespesas",
                column: "ApuracaoOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApuracoesArt29ADespesas",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "ApuracoesArt29A",
                schema: "legislativo");
        }
    }
}
