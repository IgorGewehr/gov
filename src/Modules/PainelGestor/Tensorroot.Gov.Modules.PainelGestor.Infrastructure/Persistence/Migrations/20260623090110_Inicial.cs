using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "painelgestor");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "painelgestor",
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
                name: "EventosIngeridos",
                schema: "painelgestor",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IngeridoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosIngeridos", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "IndicadoresMunicipio",
                schema: "painelgestor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    DotacaoAtualizada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Empenhado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Liquidado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Pago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ArrecadacaoTributaria = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DividaAtivaSaldoInscrito = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DividaAtivaSaldoAjuizado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DividaAtivaRecuperada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DespesaPessoal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceitaCorrenteLiquida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RclMesReferencia = table.Column<int>(type: "int", nullable: false),
                    RemessasEnviadas = table.Column<int>(type: "int", nullable: false),
                    RemessasComPrazoVencido = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicadoresMunicipio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "painelgestor",
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
                name: "ParametrosLimitePessoal",
                schema: "painelgestor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    LimiteLegal = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    FatorPrudencial = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    FatorAlerta = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosLimitePessoal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndicadoresMunicipioMinimos",
                schema: "painelgestor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Setor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceitaBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentualAplicado = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    PercentualMinimo = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IndicadorMunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicadoresMunicipioMinimos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicadoresMunicipioMinimos_IndicadoresMunicipio_IndicadorMunicipioId",
                        column: x => x.IndicadorMunicipioId,
                        principalSchema: "painelgestor",
                        principalTable: "IndicadoresMunicipio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "painelgestor",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "painelgestor",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosIngeridos_TenantId_EventId",
                schema: "painelgestor",
                table: "EventosIngeridos",
                columns: new[] { "TenantId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndicadoresMunicipio_TenantId_Exercicio",
                schema: "painelgestor",
                table: "IndicadoresMunicipio",
                columns: new[] { "TenantId", "Exercicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndicadoresMunicipioMinimos_IndicadorMunicipioId",
                schema: "painelgestor",
                table: "IndicadoresMunicipioMinimos",
                column: "IndicadorMunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptUtc",
                schema: "painelgestor",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosLimitePessoal_TenantId_VigenciaInicio",
                schema: "painelgestor",
                table: "ParametrosLimitePessoal",
                columns: new[] { "TenantId", "VigenciaInicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "painelgestor");

            migrationBuilder.DropTable(
                name: "EventosIngeridos",
                schema: "painelgestor");

            migrationBuilder.DropTable(
                name: "IndicadoresMunicipioMinimos",
                schema: "painelgestor");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "painelgestor");

            migrationBuilder.DropTable(
                name: "ParametrosLimitePessoal",
                schema: "painelgestor");

            migrationBuilder.DropTable(
                name: "IndicadoresMunicipio",
                schema: "painelgestor");
        }
    }
}
