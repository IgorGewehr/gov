using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ObrasFisicoFinanceiroMedicaoRdo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obras",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Objeto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    GeoCodigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RegimeExecucao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValorContratado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataAssinaturaContrato = table.Column<DateOnly>(type: "date", nullable: false),
                    DataInicioOrdemServico = table.Column<DateOnly>(type: "date", nullable: true),
                    DataConclusao = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PercentualFisicoAcumulado = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ValorMedidoAcumulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FiscalDesignadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObrasDesignacoesFiscais",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Desde = table.Column<DateOnly>(type: "date", nullable: false),
                    AtoDesignacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasDesignacoesFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasDesignacoesFiscais_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasEtapas",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PercentualFisicoPrevisto = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ValorPrevisto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataPrevistaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPrevistaFim = table.Column<DateOnly>(type: "date", nullable: false),
                    PercentualFisicoExecutado = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ValorMedido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasEtapas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasEtapas_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasMedicoes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    CompetenciaAno = table.Column<int>(type: "int", nullable: false),
                    CompetenciaMes = table.Column<int>(type: "int", nullable: false),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFim = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorMedido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentualFisicoNoPeriodo = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiscalAprovadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataAprovacao = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasMedicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasMedicoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasOcorrencias",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RegistradaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasOcorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasOcorrencias_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasParalisacoes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataParalisacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataReinicio = table.Column<DateOnly>(type: "date", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasParalisacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasParalisacoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasRdos",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    CondicaoTempo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EfetivoMaoDeObra = table.Column<int>(type: "int", nullable: false),
                    EquipamentosMobilizados = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AtividadesExecutadas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Ocorrencias = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponsavelTecnicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasRdos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasRdos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "patrimonio",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObrasMedicoesItens",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PercentualFisicoNoPeriodo = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ValorNoPeriodo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasMedicoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasMedicoesItens_ObrasMedicoes_MedicaoId",
                        column: x => x.MedicaoId,
                        principalSchema: "patrimonio",
                        principalTable: "ObrasMedicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Obras_TenantId_ContratoId",
                schema: "patrimonio",
                table: "Obras",
                columns: new[] { "TenantId", "ContratoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obras_TenantId_Situacao",
                schema: "patrimonio",
                table: "Obras",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ObrasDesignacoesFiscais_ObraId",
                schema: "patrimonio",
                table: "ObrasDesignacoesFiscais",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasEtapas_ObraId",
                schema: "patrimonio",
                table: "ObrasEtapas",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasMedicoes_ObraId_Numero",
                schema: "patrimonio",
                table: "ObrasMedicoes",
                columns: new[] { "ObraId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObrasMedicoesItens_MedicaoId",
                schema: "patrimonio",
                table: "ObrasMedicoesItens",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasOcorrencias_ObraId",
                schema: "patrimonio",
                table: "ObrasOcorrencias",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasParalisacoes_ObraId",
                schema: "patrimonio",
                table: "ObrasParalisacoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasRdos_ObraId_Data",
                schema: "patrimonio",
                table: "ObrasRdos",
                columns: new[] { "ObraId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObrasDesignacoesFiscais",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasEtapas",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasMedicoesItens",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasOcorrencias",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasParalisacoes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasRdos",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ObrasMedicoes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Obras",
                schema: "patrimonio");
        }
    }
}
