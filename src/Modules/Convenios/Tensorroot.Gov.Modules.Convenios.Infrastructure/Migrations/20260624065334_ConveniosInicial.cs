using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConveniosInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "convenios");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sequencia = table.Column<long>(type: "bigint", nullable: false),
                    HashAnterior = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HashAtual = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConveniosRecebidos",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcedenteCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    ConcedenteNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConcedenteEsfera = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcedenteSistemaOrigem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroConvenioTransferegov = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ContrapartidaModalidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContrapartidaValorPactuado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ContrapartidaPercentualMinimo = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    ContrapartidaNormaFonte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContrapartidaValorEmpenhado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Vigencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MotivoInadimplencia = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveniosRecebidos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParceriasOsc",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OscCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    OscRazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OscNaturezaJuridica = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OscExperienciaPrevia = table.Column<bool>(type: "bit", nullable: false),
                    OscCapacidadeTecnica = table.Column<bool>(type: "bit", nullable: false),
                    OscCertidoesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoInstrumento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SelecaoTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SelecaoProcessoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelecaoEdital = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelecaoEditalHomologado = table.Column<bool>(type: "bit", nullable: false),
                    SelecaoFundamentoLegal = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelecaoJustificativa = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Vigencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GestorParceriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComissaoMonitoramentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MotivoInadimplencia = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParceriasOsc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConveniosRecebidosPlano",
                schema: "convenios",
                columns: table => new
                {
                    ConvenioRecebidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValorRepasse = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlanoValorContrapartida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EtapasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParcelasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanoAprovado = table.Column<bool>(type: "bit", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveniosRecebidosPlano", x => x.ConvenioRecebidoId);
                    table.ForeignKey(
                        name: "FK_ConveniosRecebidosPlano_ConveniosRecebidos_ConvenioRecebidoId",
                        column: x => x.ConvenioRecebidoId,
                        principalSchema: "convenios",
                        principalTable: "ConveniosRecebidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConveniosRecebidosPrestacoes",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompetenciaRef = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataSubmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    PrazoAnalise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrazoSaneamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SaneamentoConcedido = table.Column<bool>(type: "bit", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ValorDevolucao = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ConvenioRecebidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveniosRecebidosPrestacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConveniosRecebidosPrestacoes_ConveniosRecebidos_ConvenioRecebidoId",
                        column: x => x.ConvenioRecebidoId,
                        principalSchema: "convenios",
                        principalTable: "ConveniosRecebidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConveniosRecebidosRendimentos",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Aplicado = table.Column<bool>(type: "bit", nullable: false),
                    ConvenioRecebidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveniosRecebidosRendimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConveniosRecebidosRendimentos_ConveniosRecebidos_ConvenioRecebidoId",
                        column: x => x.ConvenioRecebidoId,
                        principalSchema: "convenios",
                        principalTable: "ConveniosRecebidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConveniosRecebidosRepasses",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOrdem = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataPrevista = table.Column<DateOnly>(type: "date", nullable: false),
                    DataLiberada = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConvenioRecebidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveniosRecebidosRepasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConveniosRecebidosRepasses_ConveniosRecebidos_ConvenioRecebidoId",
                        column: x => x.ConvenioRecebidoId,
                        principalSchema: "convenios",
                        principalTable: "ConveniosRecebidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParceriasOscPlano",
                schema: "convenios",
                columns: table => new
                {
                    ParceriaOscId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValorGlobal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MetasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParcelasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanoAprovado = table.Column<bool>(type: "bit", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParceriasOscPlano", x => x.ParceriaOscId);
                    table.ForeignKey(
                        name: "FK_ParceriasOscPlano_ParceriasOsc_ParceriaOscId",
                        column: x => x.ParceriaOscId,
                        principalSchema: "convenios",
                        principalTable: "ParceriasOsc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParceriasOscPrestacao",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PrazoEntrega = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntregaProrrogada = table.Column<bool>(type: "bit", nullable: false),
                    DataRecebimento = table.Column<DateOnly>(type: "date", nullable: true),
                    PrazoAnalise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrazoSaneamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SaneamentoConcedido = table.Column<bool>(type: "bit", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ValorDevolucao = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ParceriaOscId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParceriasOscPrestacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParceriasOscPrestacao_ParceriasOsc_ParceriaOscId",
                        column: x => x.ParceriaOscId,
                        principalSchema: "convenios",
                        principalTable: "ParceriasOsc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParceriasOscRepasses",
                schema: "convenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOrdem = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataPrevista = table.Column<DateOnly>(type: "date", nullable: false),
                    Condicionantes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataLiberada = table.Column<DateOnly>(type: "date", nullable: true),
                    EmpenhoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiquidacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PagamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParceriaOscId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParceriasOscRepasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParceriasOscRepasses_ParceriasOsc_ParceriaOscId",
                        column: x => x.ParceriaOscId,
                        principalSchema: "convenios",
                        principalTable: "ParceriasOsc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "convenios",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "convenios",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConveniosRecebidos_TenantId_Situacao",
                schema: "convenios",
                table: "ConveniosRecebidos",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ConveniosRecebidosPrestacoes_ConvenioRecebidoId",
                schema: "convenios",
                table: "ConveniosRecebidosPrestacoes",
                column: "ConvenioRecebidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConveniosRecebidosRendimentos_ConvenioRecebidoId",
                schema: "convenios",
                table: "ConveniosRecebidosRendimentos",
                column: "ConvenioRecebidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConveniosRecebidosRepasses_ConvenioRecebidoId",
                schema: "convenios",
                table: "ConveniosRecebidosRepasses",
                column: "ConvenioRecebidoId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptUtc",
                schema: "convenios",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ParceriasOsc_TenantId_Situacao",
                schema: "convenios",
                table: "ParceriasOsc",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ParceriasOscPrestacao_ParceriaOscId",
                schema: "convenios",
                table: "ParceriasOscPrestacao",
                column: "ParceriaOscId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParceriasOscRepasses_ParceriaOscId",
                schema: "convenios",
                table: "ParceriasOscRepasses",
                column: "ParceriaOscId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ConveniosRecebidosPlano",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ConveniosRecebidosPrestacoes",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ConveniosRecebidosRendimentos",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ConveniosRecebidosRepasses",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ParceriasOscPlano",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ParceriasOscPrestacao",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ParceriasOscRepasses",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ConveniosRecebidos",
                schema: "convenios");

            migrationBuilder.DropTable(
                name: "ParceriasOsc",
                schema: "convenios");
        }
    }
}
