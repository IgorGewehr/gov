using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// M6 (parte 4) — Dívida Ativa → CDA → Cobrança/Protesto → Execução Fiscal → Prescrição.
    /// <list type="bullet">
    /// <item>INSCRIÇÃO enriquecida com os dados do crédito exigidos pela CDA (origem/natureza, fundamento
    /// legal, valor originário) + marco de constituição definitiva (início da prescrição — CTN art. 174)
    /// + número sequencial da inscrição + regra de encargos PARAMETRIZÁVEL (multa/juros/correção, owned).</item>
    /// <item>CDA com requisitos legais obrigatórios (LEF art. 2º §5º) — validação no domínio.</item>
    /// <item>PROTESTO extrajudicial (Lei 9.492/97) modelado como ato: tabela <c>RemessasProtesto</c>
    /// (remessa/retorno atrás de ACL versionada por CRA).</item>
    /// <item>PRESCRIÇÃO real (CTN art. 174): termo inicial = constituição definitiva ou última interrupção;
    /// prazo parametrizável.</item>
    /// </list>
    /// Renomeia <c>ValorInscrito</c> → <c>ValorOriginario</c>. Em SQLite (dev/testes) o schema vem de
    /// EnsureCreated/SchemaProvisioner; esta migration vale para o provedor SqlServer (produção).
    /// DbContext ctor inalterado.
    /// </summary>
    /// <inheritdoc />
    public partial class DividaAtivaCdaProtestoPrescricao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValorInscrito",
                schema: "tributos",
                table: "DividasAtivas",
                newName: "ValorOriginario");

            migrationBuilder.AddColumn<string>(
                name: "TipoTributo",
                schema: "tributos",
                table: "DividasAtivas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "VencimentoOrigem",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataConstituicaoDefinitiva",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataUltimaInterrupcaoPrescricao",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumeroInscricao",
                schema: "tributos",
                table: "DividasAtivas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "AnosPrescricaoParametrizado",
                schema: "tributos",
                table: "DividasAtivas",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "OrigemNatureza",
                schema: "tributos",
                table: "DividasAtivas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FundamentoLegal",
                schema: "tributos",
                table: "DividasAtivas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EncargosMultaMoraPercentual",
                schema: "tributos",
                table: "DividasAtivas",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EncargosJurosMoraPercentualMensal",
                schema: "tributos",
                table: "DividasAtivas",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EncargosCorrecaoPercentualMensal",
                schema: "tributos",
                table: "DividasAtivas",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EncargosFundamentoLegal",
                schema: "tributos",
                table: "DividasAtivas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RemessasProtesto",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DividaAtivaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroCda = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IdentificadorCra = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataGeracao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataTransmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataRetorno = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ocorrencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProtocoloCartorio = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasProtesto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasProtesto_DividasAtivas_DividaAtivaId",
                        column: x => x.DividaAtivaId,
                        principalSchema: "tributos",
                        principalTable: "DividasAtivas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DividasAtivas_TenantId_NumeroInscricao",
                schema: "tributos",
                table: "DividasAtivas",
                columns: new[] { "TenantId", "NumeroInscricao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemessasProtesto_DividaAtivaId",
                schema: "tributos",
                table: "RemessasProtesto",
                column: "DividaAtivaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemessasProtesto",
                schema: "tributos");

            migrationBuilder.DropIndex(
                name: "IX_DividasAtivas_TenantId_NumeroInscricao",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(name: "TipoTributo", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "VencimentoOrigem", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "DataConstituicaoDefinitiva", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "DataUltimaInterrupcaoPrescricao", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "NumeroInscricao", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "AnosPrescricaoParametrizado", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "OrigemNatureza", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "FundamentoLegal", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "EncargosMultaMoraPercentual", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "EncargosJurosMoraPercentualMensal", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "EncargosCorrecaoPercentualMensal", schema: "tributos", table: "DividasAtivas");
            migrationBuilder.DropColumn(name: "EncargosFundamentoLegal", schema: "tributos", table: "DividasAtivas");

            migrationBuilder.RenameColumn(
                name: "ValorOriginario",
                schema: "tributos",
                table: "DividasAtivas",
                newName: "ValorInscrito");
        }
    }
}
