using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assistenciasocial");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "assistenciasocial",
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
                name: "Beneficios",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MotivoIndeferimento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataDecisao = table.Column<DateOnly>(type: "date", nullable: true),
                    QuantidadeCesta = table.Column<int>(type: "int", nullable: true),
                    DataEntregaCesta = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Familias",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nis = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    CpfResponsavel = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Endereco_Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Endereco_Municipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Endereco_Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Endereco_Territorio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UnidadeAtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Territorio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RendaPerCapita = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataReferenciamento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataUltimaAtualizacaoCadastral = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "assistenciasocial",
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
                name: "ParametrosVigentes",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosVigentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prontuarios",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeAtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataAbertura = table.Column<DateOnly>(type: "date", nullable: false),
                    MotivoEncerramento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataEncerramento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prontuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesAtendimento",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TerritorioCobertura = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesAtendimento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamiliasMembros",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Parentesco = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    RendaIndividual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EhPcd = table.Column<bool>(type: "bit", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamiliasMembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamiliasMembros_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalSchema: "assistenciasocial",
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProntuariosAcessos",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotivoAcesso = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataHoraAcessoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProntuarioSuasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProntuariosAcessos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProntuariosAcessos_Prontuarios_ProntuarioSuasId",
                        column: x => x.ProntuarioSuasId,
                        principalSchema: "assistenciasocial",
                        principalTable: "Prontuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProntuariosPlanos",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objetivos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataPactuacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ProntuarioSuasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Compromissos = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProntuariosPlanos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProntuariosPlanos_Prontuarios_ProntuarioSuasId",
                        column: x => x.ProntuarioSuasId,
                        principalSchema: "assistenciasocial",
                        principalTable: "Prontuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProntuariosRegistros",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Servico = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataAtendimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProntuarioSuasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProntuariosRegistros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProntuariosRegistros_Prontuarios_ProntuarioSuasId",
                        column: x => x.ProntuarioSuasId,
                        principalSchema: "assistenciasocial",
                        principalTable: "Prontuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProntuariosViolacoes",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoViolacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnvolveCriancaAdolescente = table.Column<bool>(type: "bit", nullable: false),
                    DataIdentificacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ProntuarioSuasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProntuariosViolacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProntuariosViolacoes_Prontuarios_ProntuarioSuasId",
                        column: x => x.ProntuarioSuasId,
                        principalSchema: "assistenciasocial",
                        principalTable: "Prontuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "assistenciasocial",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficios_TenantId_FamiliaId",
                schema: "assistenciasocial",
                table: "Beneficios",
                columns: new[] { "TenantId", "FamiliaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficios_TenantId_Situacao",
                schema: "assistenciasocial",
                table: "Beneficios",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Familias_TenantId_Nis",
                schema: "assistenciasocial",
                table: "Familias",
                columns: new[] { "TenantId", "Nis" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Familias_TenantId_Territorio",
                schema: "assistenciasocial",
                table: "Familias",
                columns: new[] { "TenantId", "Territorio" });

            migrationBuilder.CreateIndex(
                name: "IX_FamiliasMembros_FamiliaId",
                schema: "assistenciasocial",
                table: "FamiliasMembros",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "assistenciasocial",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosVigentes_TenantId_Chave_VigenciaInicio",
                schema: "assistenciasocial",
                table: "ParametrosVigentes",
                columns: new[] { "TenantId", "Chave", "VigenciaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Prontuarios_TenantId_FamiliaId",
                schema: "assistenciasocial",
                table: "Prontuarios",
                columns: new[] { "TenantId", "FamiliaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProntuariosAcessos_ProntuarioSuasId",
                schema: "assistenciasocial",
                table: "ProntuariosAcessos",
                column: "ProntuarioSuasId");

            migrationBuilder.CreateIndex(
                name: "IX_ProntuariosPlanos_ProntuarioSuasId",
                schema: "assistenciasocial",
                table: "ProntuariosPlanos",
                column: "ProntuarioSuasId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProntuariosRegistros_ProntuarioSuasId",
                schema: "assistenciasocial",
                table: "ProntuariosRegistros",
                column: "ProntuarioSuasId");

            migrationBuilder.CreateIndex(
                name: "IX_ProntuariosViolacoes_ProntuarioSuasId",
                schema: "assistenciasocial",
                table: "ProntuariosViolacoes",
                column: "ProntuarioSuasId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesAtendimento_TenantId_TerritorioCobertura",
                schema: "assistenciasocial",
                table: "UnidadesAtendimento",
                columns: new[] { "TenantId", "TerritorioCobertura" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "Beneficios",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "FamiliasMembros",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ParametrosVigentes",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ProntuariosAcessos",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ProntuariosPlanos",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ProntuariosRegistros",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ProntuariosViolacoes",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "UnidadesAtendimento",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "Familias",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "Prontuarios",
                schema: "assistenciasocial");
        }
    }
}
