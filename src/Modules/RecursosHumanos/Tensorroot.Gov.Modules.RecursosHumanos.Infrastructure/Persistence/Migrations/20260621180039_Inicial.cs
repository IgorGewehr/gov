using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "recursoshumanos");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "recursoshumanos",
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
                name: "Cargos",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Denominacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vencimento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QuantidadeVagas = table.Column<int>(type: "int", nullable: false),
                    VagasOcupadas = table.Column<int>(type: "int", nullable: false),
                    LeiCriacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlanoDeCargosId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LotacaoCodigoTributaria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LotacaoDenominacaoUnidade = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LotacaoInscricaoEstabelecimento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolhasDePagamento",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataCalculo = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFechamento = table.Column<DateOnly>(type: "date", nullable: true),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalProventos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDescontos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLiquido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolhasDePagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "recursoshumanos",
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
                name: "Servidores",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CargoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DataNomeacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPosse = table.Column<DateOnly>(type: "date", nullable: true),
                    DataExercicio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataEstabilidade = table.Column<DateOnly>(type: "date", nullable: true),
                    DataDesligamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servidores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolhasEventos",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rubrica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegimePrevidenciario = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FolhaDePagamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolhasEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolhasEventos_FolhasDePagamento_FolhaDePagamentoId",
                        column: x => x.FolhaDePagamentoId,
                        principalSchema: "recursoshumanos",
                        principalTable: "FolhasDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServidoresDependentes",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Parentesco = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServidoresDependentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServidoresDependentes_Servidores_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "recursoshumanos",
                        principalTable: "Servidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "recursoshumanos",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_TenantId_Situacao_Tipo",
                schema: "recursoshumanos",
                table: "Cargos",
                columns: new[] { "TenantId", "Situacao", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_FolhasDePagamento_TenantId_Competencia",
                schema: "recursoshumanos",
                table: "FolhasDePagamento",
                columns: new[] { "TenantId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolhasEventos_FolhaDePagamentoId",
                schema: "recursoshumanos",
                table: "FolhasEventos",
                column: "FolhaDePagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "recursoshumanos",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Servidores_TenantId_Matricula",
                schema: "recursoshumanos",
                table: "Servidores",
                columns: new[] { "TenantId", "Matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServidoresDependentes_ServidorId",
                schema: "recursoshumanos",
                table: "ServidoresDependentes",
                column: "ServidorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "Cargos",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "FolhasEventos",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "ServidoresDependentes",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "FolhasDePagamento",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "Servidores",
                schema: "recursoshumanos");
        }
    }
}
