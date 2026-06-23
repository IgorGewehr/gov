using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Planejamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcaoPpaId",
                schema: "financas",
                table: "Dotacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemDespesaFixadaId",
                schema: "financas",
                table: "Dotacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LoaId",
                schema: "financas",
                table: "Dotacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "financas",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "financas",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "financas",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CreditosAdicionais",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Especie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AtoAutorizador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AtoAbertura = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DotacaoAlvoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DotacaoAnuladaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PorDecreto = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditosAdicionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeisDiretrizes",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    PpaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLei = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AnoLei = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeisDiretrizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeisOrcamentarias",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    LdoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PpaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LimiteSuplementacaoPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    NumeroLei = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AnoLei = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeisOrcamentarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanosPlurianuais",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnoInicio = table.Column<int>(type: "int", nullable: false),
                    AnoFim = table.Column<int>(type: "int", nullable: false),
                    NumeroLei = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AnoLei = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosPlurianuais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LdoAnexos",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LdoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenciaDocumento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Obrigatorio = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdoAnexos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LdoAnexos_LeisDiretrizes_LdoId",
                        column: x => x.LdoId,
                        principalSchema: "financas",
                        principalTable: "LeisDiretrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LdoMetasFiscais",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LdoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    ReceitaTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DespesaTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultadoPrimario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultadoNominal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DividaConsolidada = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdoMetasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LdoMetasFiscais_LeisDiretrizes_LdoId",
                        column: x => x.LdoId,
                        principalSchema: "financas",
                        principalTable: "LeisDiretrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LdoPrioridades",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LdoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcaoPpaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdoPrioridades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LdoPrioridades_LeisDiretrizes_LdoId",
                        column: x => x.LdoId,
                        principalSchema: "financas",
                        principalTable: "LeisDiretrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoaItensDespesaFixada",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orgao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UnidadeOrcamentaria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FuncionalProgramatica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoriaEconomica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FonteDeRecurso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcaoPpaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NaturezaDespesa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValorFixado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrigemCreditoEspecial = table.Column<bool>(type: "bit", nullable: false),
                    DotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaItensDespesaFixada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoaItensDespesaFixada_LeisOrcamentarias_LoaId",
                        column: x => x.LoaId,
                        principalSchema: "financas",
                        principalTable: "LeisOrcamentarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoaReceitasPrevistas",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceitaCategoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceitaOrigem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceitaEspecie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceitaRubrica = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FonteDeRecurso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorPrevisto = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaReceitasPrevistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoaReceitasPrevistas_LeisOrcamentarias_LoaId",
                        column: x => x.LoaId,
                        principalSchema: "financas",
                        principalTable: "LeisOrcamentarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PpaProgramas",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PpaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Objetivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublicoAlvo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Indicador = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IndicadorLinhaBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IndicadorMeta = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PpaProgramas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PpaProgramas_PlanosPlurianuais_PpaId",
                        column: x => x.PpaId,
                        principalSchema: "financas",
                        principalTable: "PlanosPlurianuais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PpaAcoes",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FuncionalProgramatica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Produto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PpaAcoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PpaAcoes_PpaProgramas_ProgramaId",
                        column: x => x.ProgramaId,
                        principalSchema: "financas",
                        principalTable: "PpaProgramas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PpaMetas",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    MetaFisica = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetaFinanceira = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Regiao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PpaMetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PpaMetas_PpaAcoes_AcaoId",
                        column: x => x.AcaoId,
                        principalSchema: "financas",
                        principalTable: "PpaAcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "financas",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditosAdicionais_TenantId_LoaId",
                schema: "financas",
                table: "CreditosAdicionais",
                columns: new[] { "TenantId", "LoaId" });

            migrationBuilder.CreateIndex(
                name: "IX_LdoAnexos_LdoId",
                schema: "financas",
                table: "LdoAnexos",
                column: "LdoId");

            migrationBuilder.CreateIndex(
                name: "IX_LdoMetasFiscais_LdoId",
                schema: "financas",
                table: "LdoMetasFiscais",
                column: "LdoId");

            migrationBuilder.CreateIndex(
                name: "IX_LdoPrioridades_LdoId",
                schema: "financas",
                table: "LdoPrioridades",
                column: "LdoId");

            migrationBuilder.CreateIndex(
                name: "IX_LeisDiretrizes_TenantId_Exercicio",
                schema: "financas",
                table: "LeisDiretrizes",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_LeisOrcamentarias_TenantId_Exercicio",
                schema: "financas",
                table: "LeisOrcamentarias",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_LoaItensDespesaFixada_DotacaoId",
                schema: "financas",
                table: "LoaItensDespesaFixada",
                column: "DotacaoId",
                unique: true,
                filter: "[DotacaoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoaItensDespesaFixada_LoaId",
                schema: "financas",
                table: "LoaItensDespesaFixada",
                column: "LoaId");

            migrationBuilder.CreateIndex(
                name: "IX_LoaReceitasPrevistas_LoaId",
                schema: "financas",
                table: "LoaReceitasPrevistas",
                column: "LoaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosPlurianuais_TenantId_AnoInicio",
                schema: "financas",
                table: "PlanosPlurianuais",
                columns: new[] { "TenantId", "AnoInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PpaAcoes_ProgramaId",
                schema: "financas",
                table: "PpaAcoes",
                column: "ProgramaId");

            migrationBuilder.CreateIndex(
                name: "IX_PpaMetas_AcaoId",
                schema: "financas",
                table: "PpaMetas",
                column: "AcaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PpaProgramas_PpaId",
                schema: "financas",
                table: "PpaProgramas",
                column: "PpaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditosAdicionais",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LdoAnexos",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LdoMetasFiscais",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LdoPrioridades",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LoaItensDespesaFixada",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LoaReceitasPrevistas",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "PpaMetas",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LeisDiretrizes",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "LeisOrcamentarias",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "PpaAcoes",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "PpaProgramas",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "PlanosPlurianuais",
                schema: "financas");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "financas",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "AcaoPpaId",
                schema: "financas",
                table: "Dotacoes");

            migrationBuilder.DropColumn(
                name: "ItemDespesaFixadaId",
                schema: "financas",
                table: "Dotacoes");

            migrationBuilder.DropColumn(
                name: "LoaId",
                schema: "financas",
                table: "Dotacoes");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "financas",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "financas",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "financas",
                table: "AuditTrail");
        }
    }
}
