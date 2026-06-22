using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "financas");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "financas",
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
                name: "Dotacoes",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Orgao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UnidadeOrcamentaria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FuncionalProgramatica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoriaEconomica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FonteDeRecurso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorDotadoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorReforcado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorAnulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorEmpenhadoLiquido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dotacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empenhos",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredorNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CredorTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CredorDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorEmpenhado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorAnulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorLiquidado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    DataEmpenho = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empenhos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Liquidacoes",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpenhoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataLiquidacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DocumentoTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentoNumero = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DocumentoChaveNfse = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
                    DocumentoDataEmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdensDePagamento",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    ContaBanco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContaAgencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContaNumero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContaPix = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensDePagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "financas",
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
                name: "ReceitasArrecadadas",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitasArrecadadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestosAPagar",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpenhoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Classificacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorInscrito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorLiquidado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorCancelado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExercicioOrigem = table.Column<int>(type: "int", nullable: false),
                    ExercicioInscricao = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestosAPagar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensPagamento",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiquidacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrdemDePagamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPagamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPagamento_OrdensDePagamento_OrdemDePagamentoId",
                        column: x => x.OrdemDePagamentoId,
                        principalSchema: "financas",
                        principalTable: "OrdensDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "financas",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Dotacoes_TenantId_Exercicio",
                schema: "financas",
                table: "Dotacoes",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Empenhos_DotacaoId",
                schema: "financas",
                table: "Empenhos",
                column: "DotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empenhos_TenantId_Exercicio_Numero",
                schema: "financas",
                table: "Empenhos",
                columns: new[] { "TenantId", "Exercicio", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensPagamento_LiquidacaoId",
                schema: "financas",
                table: "ItensPagamento",
                column: "LiquidacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPagamento_OrdemDePagamentoId",
                schema: "financas",
                table: "ItensPagamento",
                column: "OrdemDePagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidacoes_EmpenhoId",
                schema: "financas",
                table: "Liquidacoes",
                column: "EmpenhoId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidacoes_TenantId_EmpenhoId",
                schema: "financas",
                table: "Liquidacoes",
                columns: new[] { "TenantId", "EmpenhoId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDePagamento_TenantId_Numero",
                schema: "financas",
                table: "OrdensDePagamento",
                columns: new[] { "TenantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "financas",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasArrecadadas_TenantId_OrigemId",
                schema: "financas",
                table: "ReceitasArrecadadas",
                columns: new[] { "TenantId", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RestosAPagar_EmpenhoId",
                schema: "financas",
                table: "RestosAPagar",
                column: "EmpenhoId");

            migrationBuilder.CreateIndex(
                name: "IX_RestosAPagar_TenantId_ExercicioInscricao",
                schema: "financas",
                table: "RestosAPagar",
                columns: new[] { "TenantId", "ExercicioInscricao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "Dotacoes",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "Empenhos",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "ItensPagamento",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "Liquidacoes",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "ReceitasArrecadadas",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "RestosAPagar",
                schema: "financas");

            migrationBuilder.DropTable(
                name: "OrdensDePagamento",
                schema: "financas");
        }
    }
}
