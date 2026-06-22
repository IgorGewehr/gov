using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "legislativo");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "legislativo",
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
                schema: "legislativo",
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
                name: "Proposicoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ementa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Autoria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Protocolo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataApresentacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroAutografo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposicoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalMembros = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Votacoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaioriaExigida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalMembros = table.Column<int>(type: "int", nullable: false),
                    Presentes = table.Column<int>(type: "int", nullable: false),
                    Turno = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProposicoesEmendas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Autoria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposicaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposicoesEmendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposicoesEmendas_Proposicoes_ProposicaoOwnerId",
                        column: x => x.ProposicaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Proposicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposicoesSubstitutivos",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Autoria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposicaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposicoesSubstitutivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposicoesSubstitutivos_Proposicoes_ProposicaoOwnerId",
                        column: x => x.ProposicaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Proposicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposicoesTramitacoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comissao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParecerFavoravel = table.Column<bool>(type: "bit", nullable: true),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposicaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposicoesTramitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposicoesTramitacoes_Proposicoes_ProposicaoOwnerId",
                        column: x => x.ProposicaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Proposicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessoesOrdemDoDia",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    SessaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesOrdemDoDia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesOrdemDoDia_Sessoes_SessaoOwnerId",
                        column: x => x.SessaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessoesPresencas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VereadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SessaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesPresencas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesPresencas_Sessoes_SessaoOwnerId",
                        column: x => x.SessaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotacoesVotos",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VereadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sentido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegistradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VotacaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotacoesVotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotacoesVotos_Votacoes_VotacaoOwnerId",
                        column: x => x.VotacaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Votacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "legislativo",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "legislativo",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Proposicoes_TenantId_Protocolo",
                schema: "legislativo",
                table: "Proposicoes",
                columns: new[] { "TenantId", "Protocolo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposicoes_TenantId_Situacao",
                schema: "legislativo",
                table: "Proposicoes",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposicoesEmendas_ProposicaoOwnerId",
                schema: "legislativo",
                table: "ProposicoesEmendas",
                column: "ProposicaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposicoesSubstitutivos_ProposicaoOwnerId",
                schema: "legislativo",
                table: "ProposicoesSubstitutivos",
                column: "ProposicaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposicoesTramitacoes_ProposicaoOwnerId",
                schema: "legislativo",
                table: "ProposicoesTramitacoes",
                column: "ProposicaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_TenantId_Situacao",
                schema: "legislativo",
                table: "Sessoes",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesOrdemDoDia_SessaoOwnerId",
                schema: "legislativo",
                table: "SessoesOrdemDoDia",
                column: "SessaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesPresencas_SessaoOwnerId",
                schema: "legislativo",
                table: "SessoesPresencas",
                column: "SessaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Votacoes_TenantId_ProposicaoId",
                schema: "legislativo",
                table: "Votacoes",
                columns: new[] { "TenantId", "ProposicaoId" });

            migrationBuilder.CreateIndex(
                name: "IX_VotacoesVotos_VotacaoOwnerId",
                schema: "legislativo",
                table: "VotacoesVotos",
                column: "VotacaoOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "ProposicoesEmendas",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "ProposicoesSubstitutivos",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "ProposicoesTramitacoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "SessoesOrdemDoDia",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "SessoesPresencas",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "VotacoesVotos",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Proposicoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Sessoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Votacoes",
                schema: "legislativo");
        }
    }
}
