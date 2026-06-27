using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanoCarreiraProcessosTrabalhistasSicapPessoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnquadramentosCarreira",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanoCarreiraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClasseAtual = table.Column<int>(type: "int", nullable: false),
                    ReferenciaAtual = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnquadramentosCarreira", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanosCarreira",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenominacaoCarreira = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LeiInstituicao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VencimentoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumeroClasses = table.Column<int>(type: "int", nullable: false),
                    NumeroReferencias = table.Column<int>(type: "int", nullable: false),
                    PercentualEntreReferencias = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    PercentualEntreClasses = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    IntersticioMeses = table.Column<int>(type: "int", nullable: false),
                    NotaMinimaProgressao = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosCarreira", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessosTrabalhistas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroProcesso = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Vara = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reclamante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValorCausa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorAcordo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValorCondenacao = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DataAjuizamento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataEncerramento = table.Column<DateOnly>(type: "date", nullable: true),
                    Prognostico = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorProvisionado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessosTrabalhistas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemessasSicapPessoal",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoOrgao = table.Column<int>(type: "int", nullable: false),
                    SequencialLote = table.Column<int>(type: "int", nullable: false),
                    DataGeracaoLote = table.Column<DateOnly>(type: "date", nullable: false),
                    VersaoLeiaute = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProtocoloTransmissao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasSicapPessoal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesCarreira",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClasseOrigem = table.Column<int>(type: "int", nullable: true),
                    ReferenciaOrigem = table.Column<int>(type: "int", nullable: true),
                    ClasseDestino = table.Column<int>(type: "int", nullable: false),
                    ReferenciaDestino = table.Column<int>(type: "int", nullable: false),
                    VencimentoResultante = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DataEfeito = table.Column<DateOnly>(type: "date", nullable: false),
                    Fundamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PortariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnquadramentoServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesCarreira", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesCarreira_EnquadramentosCarreira_EnquadramentoServidorId",
                        column: x => x.EnquadramentoServidorId,
                        principalSchema: "recursoshumanos",
                        principalTable: "EnquadramentosCarreira",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtosAdmissaoSicap",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdentificadorAto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoAto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DescricaoCargo = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    CargaHorariaSemanal = table.Column<int>(type: "int", nullable: false),
                    ClassificacaoConcurso = table.Column<int>(type: "int", nullable: true),
                    DataAto = table.Column<DateOnly>(type: "date", nullable: false),
                    DataHistorica = table.Column<DateOnly>(type: "date", nullable: true),
                    DataTermino = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoExtincao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RemessaSicapPessoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtosAdmissaoSicap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtosAdmissaoSicap_RemessasSicapPessoal_RemessaSicapPessoalId",
                        column: x => x.RemessaSicapPessoalId,
                        principalSchema: "recursoshumanos",
                        principalTable: "RemessasSicapPessoal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtosAdmissaoSicap_RemessaSicapPessoalId",
                schema: "recursoshumanos",
                table: "AtosAdmissaoSicap",
                column: "RemessaSicapPessoalId");

            migrationBuilder.CreateIndex(
                name: "IX_EnquadramentosCarreira_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "EnquadramentosCarreira",
                columns: new[] { "TenantId", "ServidorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesCarreira_EnquadramentoServidorId",
                schema: "recursoshumanos",
                table: "MovimentacoesCarreira",
                column: "EnquadramentoServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosCarreira_TenantId_Situacao",
                schema: "recursoshumanos",
                table: "PlanosCarreira",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosTrabalhistas_TenantId_NumeroProcesso",
                schema: "recursoshumanos",
                table: "ProcessosTrabalhistas",
                columns: new[] { "TenantId", "NumeroProcesso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosTrabalhistas_TenantId_Situacao_Prognostico",
                schema: "recursoshumanos",
                table: "ProcessosTrabalhistas",
                columns: new[] { "TenantId", "Situacao", "Prognostico" });

            migrationBuilder.CreateIndex(
                name: "IX_RemessasSicapPessoal_TenantId_CodigoOrgao_SequencialLote",
                schema: "recursoshumanos",
                table: "RemessasSicapPessoal",
                columns: new[] { "TenantId", "CodigoOrgao", "SequencialLote" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemessasSicapPessoal_TenantId_Situacao",
                schema: "recursoshumanos",
                table: "RemessasSicapPessoal",
                columns: new[] { "TenantId", "Situacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtosAdmissaoSicap",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "MovimentacoesCarreira",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "PlanosCarreira",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "ProcessosTrabalhistas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "RemessasSicapPessoal",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "EnquadramentosCarreira",
                schema: "recursoshumanos");
        }
    }
}
