using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tributos");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "tributos",
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
                name: "Contribuintes",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoPessoa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InscricaoMunicipal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contribuintes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DividasAtivas",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LancamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorInscrito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataInscricao = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroCda = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividasAtivas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lancamentos",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoTributo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    ValorPrincipal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lancamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscaisServico",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PrestadorCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    TomadorDocumento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    ValorServico = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorIss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscaisServico", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "tributos",
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

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "tributos",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contribuintes_TenantId_Documento",
                schema: "tributos",
                table: "Contribuintes",
                columns: new[] { "TenantId", "Documento" });

            migrationBuilder.CreateIndex(
                name: "IX_DividasAtivas_TenantId_ContribuinteId",
                schema: "tributos",
                table: "DividasAtivas",
                columns: new[] { "TenantId", "ContribuinteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_TenantId_ContribuinteId",
                schema: "tributos",
                table: "Lancamentos",
                columns: new[] { "TenantId", "ContribuinteId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscaisServico_TenantId_ChaveAcesso",
                schema: "tributos",
                table: "NotasFiscaisServico",
                columns: new[] { "TenantId", "ChaveAcesso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "tributos",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "Contribuintes",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "DividasAtivas",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "Lancamentos",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "NotasFiscaisServico",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "tributos");
        }
    }
}
