using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "saude");

            migrationBuilder.CreateTable(
                name: "Atendimentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Ciap = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    NivelGarantia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AssinaturaCertificado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssinaturaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssinaturaCarimbo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssinaturaNivel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ProtocoloRnds = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atendimentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "saude",
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
                name: "OutboxMessages",
                schema: "saude",
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
                name: "Pacientes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cns = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Identificacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CnsConfirmado = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesRegulacao",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoSolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalSolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Prioridade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataSolicitacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataAutorizacao = table.Column<DateOnly>(type: "date", nullable: true),
                    ProtocoloSisreg = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CotaDisponivel = table.Column<int>(type: "int", nullable: false),
                    CotaTotal = table.Column<int>(type: "int", nullable: false),
                    ProcedimentoCodigoSigtap = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcedimentoDescricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesRegulacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtendimentosEvolucoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subjetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Objetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Avaliacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Plano = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Assinada = table.Column<bool>(type: "bit", nullable: false),
                    EhAdendo = table.Column<bool>(type: "bit", nullable: false),
                    EvolucaoReferenciadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtendimentosEvolucoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtendimentosEvolucoes_Atendimentos_AtendimentoId",
                        column: x => x.AtendimentoId,
                        principalSchema: "saude",
                        principalTable: "Atendimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtendimentosPrescricoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Item = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Posologia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtendimentosPrescricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtendimentosPrescricoes_Atendimentos_AtendimentoId",
                        column: x => x.AtendimentoId,
                        principalSchema: "saude",
                        principalTable: "Atendimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtendimentosSolicitacoesExame",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Procedimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtendimentosSolicitacoesExame", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtendimentosSolicitacoesExame_Atendimentos_AtendimentoId",
                        column: x => x.AtendimentoId,
                        principalSchema: "saude",
                        principalTable: "Atendimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PacientesAlergias",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Substancia = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Gravidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataRegistro = table.Column<DateOnly>(type: "date", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacientesAlergias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacientesAlergias_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalSchema: "saude",
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PacientesCondicoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataRegistro = table.Column<DateOnly>(type: "date", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacientesCondicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacientesCondicoes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalSchema: "saude",
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Atendimentos_TenantId_PacienteId",
                schema: "saude",
                table: "Atendimentos",
                columns: new[] { "TenantId", "PacienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_AtendimentosEvolucoes_AtendimentoId",
                schema: "saude",
                table: "AtendimentosEvolucoes",
                column: "AtendimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AtendimentosPrescricoes_AtendimentoId",
                schema: "saude",
                table: "AtendimentosPrescricoes",
                column: "AtendimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AtendimentosSolicitacoesExame_AtendimentoId",
                schema: "saude",
                table: "AtendimentosSolicitacoesExame",
                column: "AtendimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "saude",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "saude",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TenantId_Cns",
                schema: "saude",
                table: "Pacientes",
                columns: new[] { "TenantId", "Cns" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PacientesAlergias_PacienteId",
                schema: "saude",
                table: "PacientesAlergias",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PacientesCondicoes_PacienteId",
                schema: "saude",
                table: "PacientesCondicoes",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesRegulacao_TenantId_PacienteId",
                schema: "saude",
                table: "SolicitacoesRegulacao",
                columns: new[] { "TenantId", "PacienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesRegulacao_TenantId_Situacao",
                schema: "saude",
                table: "SolicitacoesRegulacao",
                columns: new[] { "TenantId", "Situacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtendimentosEvolucoes",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AtendimentosPrescricoes",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AtendimentosSolicitacoesExame",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "PacientesAlergias",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "PacientesCondicoes",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "SolicitacoesRegulacao",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Atendimentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Pacientes",
                schema: "saude");
        }
    }
}
