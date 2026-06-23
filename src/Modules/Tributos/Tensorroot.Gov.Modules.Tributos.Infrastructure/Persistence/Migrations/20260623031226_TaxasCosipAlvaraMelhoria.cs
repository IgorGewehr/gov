using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// M6 (parte 3) — espécies tributárias parametrizáveis por lei municipal:
    /// <list type="bullet">
    /// <item>TAXAS/TLL (CTN arts. 77–80, SV 19/29): <c>TabelasTaxa</c> + <c>FaixasTaxa</c>.</item>
    /// <item>ALVARÁS (ato de polícia, M6-DESIGN §3.3): <c>Alvaras</c>.</item>
    /// <item>COSIP (CF art. 149-A, RE 573.675): <c>TabelasCosip</c> + <c>FaixasCosip</c>.</item>
    /// <item>CONTRIBUIÇÃO DE MELHORIA (CTN arts. 81–82): <c>ObrasContribuicaoMelhoria</c> +
    /// <c>ImoveisBeneficiadosMelhoria</c>.</item>
    /// </list>
    /// Também materializa a hash-chain da trilha de auditoria (<c>AuditTrail.HashAnterior/HashAtual/
    /// Sequencia</c>) ainda não capturada no schema do módulo Tributos (drift pré-existente comum aos
    /// demais módulos). Em SQLite (dev/testes) o schema vem de <c>EnsureCreated</c>/SchemaProvisioner;
    /// esta migration vale para o provedor SqlServer (produção). DbContext ctor inalterado.
    /// </summary>
    /// <inheritdoc />
    public partial class TaxasCosipAlvaraMelhoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "tributos",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "tributos",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "tributos",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Alvaras",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Especie = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NomeEstabelecimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AtividadeCnae = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InicioVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    FimVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alvaras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObrasContribuicaoMelhoria",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificacaoObra = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MemorialDescritivo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CustoTotalObra = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ParcelaCustoFinanciadaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ZonaBeneficiada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FatorAbsorcaoPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    DataPublicacaoEdital = table.Column<DateOnly>(type: "date", nullable: false),
                    FimPrazoImpugnacao = table.Column<DateOnly>(type: "date", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasContribuicaoMelhoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasCosip",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasCosip", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasTaxa",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Especie = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ModoCalculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasTaxa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImoveisBeneficiadosMelhoria",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProprietarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorizacaoIndividual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ContribuicaoRateada = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImoveisBeneficiadosMelhoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImoveisBeneficiadosMelhoria_ObrasContribuicaoMelhoria_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "tributos",
                        principalTable: "ObrasContribuicaoMelhoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaixasCosip",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabelaCosipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Classe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConsumoMinimoKwh = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumoMaximoKwh = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaixasCosip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaixasCosip_TabelasCosip_TabelaCosipId",
                        column: x => x.TabelaCosipId,
                        principalSchema: "tributos",
                        principalTable: "TabelasCosip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaixasTaxa",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabelaTaxaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LimiteInferior = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LimiteSuperior = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaixasTaxa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaixasTaxa_TabelasTaxa_TabelaTaxaId",
                        column: x => x.TabelaTaxaId,
                        principalSchema: "tributos",
                        principalTable: "TabelasTaxa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "tributos",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Alvaras_TenantId_ContribuinteId",
                schema: "tributos",
                table: "Alvaras",
                columns: new[] { "TenantId", "ContribuinteId" });

            migrationBuilder.CreateIndex(
                name: "IX_FaixasCosip_TabelaCosipId",
                schema: "tributos",
                table: "FaixasCosip",
                column: "TabelaCosipId");

            migrationBuilder.CreateIndex(
                name: "IX_FaixasTaxa_TabelaTaxaId",
                schema: "tributos",
                table: "FaixasTaxa",
                column: "TabelaTaxaId");

            migrationBuilder.CreateIndex(
                name: "IX_ImoveisBeneficiadosMelhoria_ObraId",
                schema: "tributos",
                table: "ImoveisBeneficiadosMelhoria",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObrasContribuicaoMelhoria_TenantId_IdentificacaoObra",
                schema: "tributos",
                table: "ObrasContribuicaoMelhoria",
                columns: new[] { "TenantId", "IdentificacaoObra" });

            migrationBuilder.CreateIndex(
                name: "IX_TabelasCosip_TenantId_Exercicio",
                schema: "tributos",
                table: "TabelasCosip",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_TabelasTaxa_TenantId_Codigo_Exercicio",
                schema: "tributos",
                table: "TabelasTaxa",
                columns: new[] { "TenantId", "Codigo", "Exercicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alvaras",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "FaixasCosip",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "FaixasTaxa",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ImoveisBeneficiadosMelhoria",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TabelasCosip",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TabelasTaxa",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ObrasContribuicaoMelhoria",
                schema: "tributos");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "tributos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "tributos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "tributos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "tributos",
                table: "AuditTrail");
        }
    }
}
