using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MerendaTransporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Merenda (PNAE): cardapio semanal + itens (genero por refeicao/dia).
            migrationBuilder.CreateTable(
                name: "Cardapios",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscolaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FaixaEtaria = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Semana = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cardapios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistribuicoesMerenda",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscolaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardapioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Refeicao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comensais = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribuicoesMerenda", x => x.Id);
                });

            // Transporte (PNATE): rota + alunos transportados.
            migrationBuilder.CreateTable(
                name: "RotasTransporte",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscolaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quilometragem = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotasTransporte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardapiosItens",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Refeicao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneroEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadePerCapita = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CardapioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardapiosItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardapiosItens_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalSchema: "educacao",
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistribuicoesMerendaConsumos",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneroEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(14,4)", nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DistribuicaoMerendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribuicoesMerendaConsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistribuicoesMerendaConsumos_DistribuicoesMerenda_DistribuicaoMerendaId",
                        column: x => x.DistribuicaoMerendaId,
                        principalSchema: "educacao",
                        principalTable: "DistribuicoesMerenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RotasTransporteAlunos",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatriculaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PontoEmbarque = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    RotaTransporteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotasTransporteAlunos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotasTransporteAlunos_RotasTransporte_RotaTransporteId",
                        column: x => x.RotaTransporteId,
                        principalSchema: "educacao",
                        principalTable: "RotasTransporte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cardapios_TenantId_EscolaId_Semana_FaixaEtaria",
                schema: "educacao",
                table: "Cardapios",
                columns: new[] { "TenantId", "EscolaId", "Semana", "FaixaEtaria" });

            migrationBuilder.CreateIndex(
                name: "IX_CardapiosItens_CardapioId",
                schema: "educacao",
                table: "CardapiosItens",
                column: "CardapioId");

            migrationBuilder.CreateIndex(
                name: "IX_DistribuicoesMerenda_TenantId_EscolaId_Data",
                schema: "educacao",
                table: "DistribuicoesMerenda",
                columns: new[] { "TenantId", "EscolaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_DistribuicoesMerendaConsumos_DistribuicaoMerendaId",
                schema: "educacao",
                table: "DistribuicoesMerendaConsumos",
                column: "DistribuicaoMerendaId");

            migrationBuilder.CreateIndex(
                name: "IX_RotasTransporte_TenantId_EscolaId",
                schema: "educacao",
                table: "RotasTransporte",
                columns: new[] { "TenantId", "EscolaId" });

            migrationBuilder.CreateIndex(
                name: "IX_RotasTransporteAlunos_RotaTransporteId",
                schema: "educacao",
                table: "RotasTransporteAlunos",
                column: "RotaTransporteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardapiosItens",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DistribuicoesMerendaConsumos",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "RotasTransporteAlunos",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "Cardapios",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DistribuicoesMerenda",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "RotasTransporte",
                schema: "educacao");
        }
    }
}
