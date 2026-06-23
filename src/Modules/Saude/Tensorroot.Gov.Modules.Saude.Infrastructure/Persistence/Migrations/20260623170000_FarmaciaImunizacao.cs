using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Onda 3c-1 (Saude operacional): Farmacia/Dispensacao (catalogo de medicamentos, estoque por lote/
    /// validade com FEFO e dispensacao ao paciente) e Imunizacao (catalogo de imunobiologicos, carteira de
    /// vacinacao com doses e aprazamento). Reusa Paciente/Estabelecimento/Profissional por Id (FK logica).
    /// </summary>
    public partial class FarmaciaImunizacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- FARMACIA: catalogo de medicamentos ----------
            migrationBuilder.CreateTable(
                name: "Medicamentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipioAtivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Apresentacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Concentracao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Forma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Controle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CodigoCatmat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Medicamentos", x => x.Id));

            // ---------- FARMACIA: estoque por estabelecimento+medicamento ----------
            migrationBuilder.CreateTable(
                name: "EstoquesMedicamento",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PontoDeRessuprimento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_EstoquesMedicamento", x => x.Id));

            migrationBuilder.CreateTable(
                name: "EstoquesMedicamentoLotes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstoqueMedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantidadeEntrada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstoquesMedicamentoLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstoquesMedicamentoLotes_EstoquesMedicamento_EstoqueMedicamentoId",
                        column: x => x.EstoqueMedicamentoId,
                        principalSchema: "saude",
                        principalTable: "EstoquesMedicamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- FARMACIA: dispensacao ao paciente ----------
            migrationBuilder.CreateTable(
                name: "Dispensacoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescricaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Dispensacoes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "DispensacoesItens",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispensacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Posologia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensacoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensacoesItens_Dispensacoes_DispensacaoId",
                        column: x => x.DispensacaoId,
                        principalSchema: "saude",
                        principalTable: "Dispensacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispensacoesItensBaixas",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemDispensadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensacoesItensBaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensacoesItensBaixas_DispensacoesItens_ItemDispensadoId",
                        column: x => x.ItemDispensadoId,
                        principalSchema: "saude",
                        principalTable: "DispensacoesItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- IMUNIZACAO: catalogo de imunobiologicos ----------
            migrationBuilder.CreateTable(
                name: "Imunobiologicos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Sigla = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalDoses = table.Column<int>(type: "int", nullable: false),
                    IntervaloDiasProximaDose = table.Column<int>(type: "int", nullable: false),
                    DoseUnica = table.Column<bool>(type: "bit", nullable: false),
                    MedicamentoEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Imunobiologicos", x => x.Id));

            // ---------- IMUNIZACAO: carteira de vacinacao ----------
            migrationBuilder.CreateTable(
                name: "CarteirasVacinacao",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CarteirasVacinacao", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CarteirasVacinacaoDoses",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarteiraVacinacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImunobiologicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroDose = table.Column<int>(type: "int", nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AplicadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataAplicacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ProximaDoseAprazada = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarteirasVacinacaoDoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarteirasVacinacaoDoses_CarteirasVacinacao_CarteiraVacinacaoId",
                        column: x => x.CarteiraVacinacaoId,
                        principalSchema: "saude",
                        principalTable: "CarteirasVacinacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- indices ----------
            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_TenantId_PrincipioAtivo",
                schema: "saude",
                table: "Medicamentos",
                columns: new[] { "TenantId", "PrincipioAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_EstoquesMedicamento_TenantId_EstabelecimentoId_MedicamentoId",
                schema: "saude",
                table: "EstoquesMedicamento",
                columns: new[] { "TenantId", "EstabelecimentoId", "MedicamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstoquesMedicamentoLotes_EstoqueMedicamentoId",
                schema: "saude",
                table: "EstoquesMedicamentoLotes",
                column: "EstoqueMedicamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispensacoes_TenantId_PacienteId",
                schema: "saude",
                table: "Dispensacoes",
                columns: new[] { "TenantId", "PacienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispensacoesItens_DispensacaoId",
                schema: "saude",
                table: "DispensacoesItens",
                column: "DispensacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DispensacoesItensBaixas_ItemDispensadoId",
                schema: "saude",
                table: "DispensacoesItensBaixas",
                column: "ItemDispensadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Imunobiologicos_TenantId_Sigla",
                schema: "saude",
                table: "Imunobiologicos",
                columns: new[] { "TenantId", "Sigla" });

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacao_TenantId_PacienteId",
                schema: "saude",
                table: "CarteirasVacinacao",
                columns: new[] { "TenantId", "PacienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacaoDoses_CarteiraVacinacaoId",
                schema: "saude",
                table: "CarteirasVacinacaoDoses",
                column: "CarteiraVacinacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacaoDoses_ProximaDoseAprazada",
                schema: "saude",
                table: "CarteirasVacinacaoDoses",
                column: "ProximaDoseAprazada");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DispensacoesItensBaixas", schema: "saude");
            migrationBuilder.DropTable(name: "CarteirasVacinacaoDoses", schema: "saude");
            migrationBuilder.DropTable(name: "EstoquesMedicamentoLotes", schema: "saude");
            migrationBuilder.DropTable(name: "DispensacoesItens", schema: "saude");
            migrationBuilder.DropTable(name: "Imunobiologicos", schema: "saude");
            migrationBuilder.DropTable(name: "CarteirasVacinacao", schema: "saude");
            migrationBuilder.DropTable(name: "EstoquesMedicamento", schema: "saude");
            migrationBuilder.DropTable(name: "Dispensacoes", schema: "saude");
            migrationBuilder.DropTable(name: "Medicamentos", schema: "saude");
        }
    }
}
