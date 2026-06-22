using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "protocolo");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "protocolo",
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
                name: "Documentos",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CarimboInstanteUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CarimboAutoridade = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Criticidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NivelAcesso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FormatoPdfA = table.Column<bool>(type: "bit", nullable: false),
                    SignatarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoAssinatura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DataJuntada = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoSemEfeito = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "protocolo",
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
                name: "Processos",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nup = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Classificacao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NivelAcesso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SetorAtualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrigemModulo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequerimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataAutuacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrazoFim = table.Column<DateOnly>(type: "date", nullable: false),
                    PrazoInicio = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessosDespachos",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AutoridadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataDespacho = table.Column<DateOnly>(type: "date", nullable: false),
                    ProcessoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessosDespachos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessosDespachos_Processos_ProcessoId",
                        column: x => x.ProcessoId,
                        principalSchema: "protocolo",
                        principalTable: "Processos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessosMovimentacoes",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetorOrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SetorDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DataMovimentacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ProcessoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessosMovimentacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessosMovimentacoes_Processos_ProcessoId",
                        column: x => x.ProcessoId,
                        principalSchema: "protocolo",
                        principalTable: "Processos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "protocolo",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_TenantId_ProcessoId",
                schema: "protocolo",
                table: "Documentos",
                columns: new[] { "TenantId", "ProcessoId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "protocolo",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Processos_TenantId_Nup",
                schema: "protocolo",
                table: "Processos",
                columns: new[] { "TenantId", "Nup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Processos_TenantId_SetorAtualId",
                schema: "protocolo",
                table: "Processos",
                columns: new[] { "TenantId", "SetorAtualId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosDespachos_ProcessoId",
                schema: "protocolo",
                table: "ProcessosDespachos",
                column: "ProcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosMovimentacoes_ProcessoId",
                schema: "protocolo",
                table: "ProcessosMovimentacoes",
                column: "ProcessoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "Documentos",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "ProcessosDespachos",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "ProcessosMovimentacoes",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "Processos",
                schema: "protocolo");
        }
    }
}
