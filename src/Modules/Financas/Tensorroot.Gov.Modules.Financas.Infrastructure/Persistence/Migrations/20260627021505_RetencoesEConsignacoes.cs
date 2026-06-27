using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetencoesEConsignacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuiasRecolhimento",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoReceita = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FavorecidoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataRecolhimento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiasRecolhimento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Retencoes",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodigoReceita = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Aliquota = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FavorecidoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Recolhida = table.Column<bool>(type: "bit", nullable: false),
                    GuiaRecolhimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiquidacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retencoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Retencoes_Liquidacoes_LiquidacaoId",
                        column: x => x.LiquidacaoId,
                        principalSchema: "financas",
                        principalTable: "Liquidacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TabelasIrrfServicos",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaFim = table.Column<DateOnly>(type: "date", nullable: true),
                    ValorMinimoRetencao = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasIrrfServicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensGuiaRecolhimento",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LiquidacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetencaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GuiaRecolhimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensGuiaRecolhimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensGuiaRecolhimento_GuiasRecolhimento_GuiaRecolhimentoId",
                        column: x => x.GuiaRecolhimentoId,
                        principalSchema: "financas",
                        principalTable: "GuiasRecolhimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaixasIrrfServicos",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aliquota = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    CodigoReceitaDarf = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TabelaIrrfServicosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaixasIrrfServicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaixasIrrfServicos_TabelasIrrfServicos_TabelaIrrfServicosId",
                        column: x => x.TabelaIrrfServicosId,
                        principalSchema: "financas",
                        principalTable: "TabelasIrrfServicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaixasIrrfServicos_TabelaIrrfServicosId",
                schema: "financas",
                table: "FaixasIrrfServicos",
                column: "TabelaIrrfServicosId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRecolhimento_TenantId_Situacao",
                schema: "financas",
                table: "GuiasRecolhimento",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensGuiaRecolhimento_GuiaRecolhimentoId",
                schema: "financas",
                table: "ItensGuiaRecolhimento",
                column: "GuiaRecolhimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensGuiaRecolhimento_RetencaoId",
                schema: "financas",
                table: "ItensGuiaRecolhimento",
                column: "RetencaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Retencoes_GuiaRecolhimentoId",
                schema: "financas",
                table: "Retencoes",
                column: "GuiaRecolhimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Retencoes_LiquidacaoId",
                schema: "financas",
                table: "Retencoes",
                column: "LiquidacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasIrrfServicos_TenantId_VigenciaInicio",
                schema: "financas",
                table: "TabelasIrrfServicos",
                columns: new[] { "TenantId", "VigenciaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaixasIrrfServicos",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "ItensGuiaRecolhimento",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "Retencoes",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "TabelasIrrfServicos",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "GuiasRecolhimento",
                schema: "financas");
        }
    }
}
