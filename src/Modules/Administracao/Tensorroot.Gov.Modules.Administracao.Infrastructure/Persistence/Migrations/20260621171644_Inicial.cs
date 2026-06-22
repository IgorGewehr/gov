using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administracao");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "administracao",
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
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemContratacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValorContratado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorAtual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaFim = table.Column<DateOnly>(type: "date", nullable: false),
                    EmpenhoRef = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DotacaoConfirmada = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroContratoPncp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PublicadoNoPncp = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NivelCadastralSICAF = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Licitacoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CriterioJulgamento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValorEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EtpId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermoReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroEditalPncp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PropostaVencedoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licitacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContratosAditivos",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PercentualSobreValorOriginal = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ValorDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NovaVigenciaFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PublicadoNoPncp = table.Column<bool>(type: "bit", nullable: false),
                    DataCelebracao = table.Column<DateOnly>(type: "date", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosAditivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratosAditivos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "administracao",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContratosApostilamentos",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataRegistro = table.Column<DateOnly>(type: "date", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosApostilamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratosApostilamentos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "administracao",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContratosGarantias",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidadeFim = table.Column<DateOnly>(type: "date", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosGarantias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratosGarantias_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "administracao",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FornecedoresSancoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    ProcessoAdministrativo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Fundamentacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValorMulta = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedoresSancoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedoresSancoes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "administracao",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicitacoesHabilitacoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicitacoesHabilitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicitacoesHabilitacoes_Licitacoes_LicitacaoId",
                        column: x => x.LicitacaoId,
                        principalSchema: "administracao",
                        principalTable: "Licitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicitacoesLotes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValorEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicitacoesLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicitacoesLotes_Licitacoes_LicitacaoId",
                        column: x => x.LicitacaoId,
                        principalSchema: "administracao",
                        principalTable: "Licitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicitacoesPropostas",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Classificacao = table.Column<int>(type: "int", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicitacoesPropostas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicitacoesPropostas_Licitacoes_LicitacaoId",
                        column: x => x.LicitacaoId,
                        principalSchema: "administracao",
                        principalTable: "Licitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicitacoesRecursos",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fundamentacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataInterposicao = table.Column<DateOnly>(type: "date", nullable: false),
                    Provido = table.Column<bool>(type: "bit", nullable: true),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicitacoesRecursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicitacoesRecursos_Licitacoes_LicitacaoId",
                        column: x => x.LicitacaoId,
                        principalSchema: "administracao",
                        principalTable: "Licitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "administracao",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_TenantId_FornecedorId",
                schema: "administracao",
                table: "Contratos",
                columns: new[] { "TenantId", "FornecedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_TenantId_Situacao",
                schema: "administracao",
                table: "Contratos",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosAditivos_ContratoId",
                schema: "administracao",
                table: "ContratosAditivos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosApostilamentos_ContratoId",
                schema: "administracao",
                table: "ContratosApostilamentos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosGarantias_ContratoId",
                schema: "administracao",
                table: "ContratosGarantias",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_TenantId_Cnpj",
                schema: "administracao",
                table: "Fornecedores",
                columns: new[] { "TenantId", "Cnpj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedoresSancoes_FornecedorId",
                schema: "administracao",
                table: "FornecedoresSancoes",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Licitacoes_TenantId_Situacao",
                schema: "administracao",
                table: "Licitacoes",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_LicitacoesHabilitacoes_LicitacaoId",
                schema: "administracao",
                table: "LicitacoesHabilitacoes",
                column: "LicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LicitacoesLotes_LicitacaoId",
                schema: "administracao",
                table: "LicitacoesLotes",
                column: "LicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LicitacoesPropostas_LicitacaoId",
                schema: "administracao",
                table: "LicitacoesPropostas",
                column: "LicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LicitacoesRecursos_LicitacaoId",
                schema: "administracao",
                table: "LicitacoesRecursos",
                column: "LicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "administracao",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "ContratosAditivos",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "ContratosApostilamentos",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "ContratosGarantias",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "FornecedoresSancoes",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "LicitacoesHabilitacoes",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "LicitacoesLotes",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "LicitacoesPropostas",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "LicitacoesRecursos",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Contratos",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Fornecedores",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Licitacoes",
                schema: "administracao");
        }
    }
}
