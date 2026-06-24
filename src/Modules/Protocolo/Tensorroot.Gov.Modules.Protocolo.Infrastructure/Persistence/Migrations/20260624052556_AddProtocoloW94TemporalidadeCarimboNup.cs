using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProtocoloW94TemporalidadeCarimboNup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarimboAlgoritmo",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarimboHashCarimbado",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarimboOrigem",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarimboPolitica",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarimboSerial",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarimboTokenBase64",
                schema: "protocolo",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DestinacoesProcesso",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoClassificacao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FimGuardaCorrente = table.Column<DateOnly>(type: "date", nullable: false),
                    FimGuardaIntermediaria = table.Column<DateOnly>(type: "date", nullable: false),
                    DestinacaoFinal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataAptidaoEliminacao = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AutorizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermoEliminacaoHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EditalEliminacaoRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataEliminacao = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinacoesProcesso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanosClassificacao",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosClassificacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenciasNup",
                schema: "protocolo",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    UltimoSequencial = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciasNup", x => new { x.TenantId, x.Ano });
                });

            migrationBuilder.CreateTable(
                name: "TabelasTemporalidade",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasTemporalidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassesDocumentais",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoClassificacao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Atividade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CodigoPai = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PlanoDeClassificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassesDocumentais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassesDocumentais_PlanosClassificacao_PlanoDeClassificacaoId",
                        column: x => x.PlanoDeClassificacaoId,
                        principalSchema: "protocolo",
                        principalTable: "PlanosClassificacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegrasTemporalidade",
                schema: "protocolo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoClassificacao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PrazoGuardaCorrenteAnos = table.Column<int>(type: "int", nullable: false),
                    PrazoGuardaIntermediariaAnos = table.Column<int>(type: "int", nullable: false),
                    DestinacaoFinal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventoContagem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TabelaTemporalidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasTemporalidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegrasTemporalidade_TabelasTemporalidade_TabelaTemporalidadeId",
                        column: x => x.TabelaTemporalidadeId,
                        principalSchema: "protocolo",
                        principalTable: "TabelasTemporalidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassesDocumentais_PlanoDeClassificacaoId_CodigoClassificacao",
                schema: "protocolo",
                table: "ClassesDocumentais",
                columns: new[] { "PlanoDeClassificacaoId", "CodigoClassificacao" });

            migrationBuilder.CreateIndex(
                name: "IX_DestinacoesProcesso_TenantId_Estado",
                schema: "protocolo",
                table: "DestinacoesProcesso",
                columns: new[] { "TenantId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_DestinacoesProcesso_TenantId_ProcessoId",
                schema: "protocolo",
                table: "DestinacoesProcesso",
                columns: new[] { "TenantId", "ProcessoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanosClassificacao_TenantId_Ativo",
                schema: "protocolo",
                table: "PlanosClassificacao",
                columns: new[] { "TenantId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_RegrasTemporalidade_TabelaTemporalidadeId_CodigoClassificacao",
                schema: "protocolo",
                table: "RegrasTemporalidade",
                columns: new[] { "TabelaTemporalidadeId", "CodigoClassificacao" });

            migrationBuilder.CreateIndex(
                name: "IX_TabelasTemporalidade_TenantId_Ativa",
                schema: "protocolo",
                table: "TabelasTemporalidade",
                columns: new[] { "TenantId", "Ativa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassesDocumentais",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "DestinacoesProcesso",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "RegrasTemporalidade",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "SequenciasNup",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "PlanosClassificacao",
                schema: "protocolo");

            migrationBuilder.DropTable(
                name: "TabelasTemporalidade",
                schema: "protocolo");

            migrationBuilder.DropColumn(
                name: "CarimboAlgoritmo",
                schema: "protocolo",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarimboHashCarimbado",
                schema: "protocolo",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarimboOrigem",
                schema: "protocolo",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarimboPolitica",
                schema: "protocolo",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarimboSerial",
                schema: "protocolo",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarimboTokenBase64",
                schema: "protocolo",
                table: "Documentos");
        }
    }
}
