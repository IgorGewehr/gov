using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IssApuracaoItbi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IssRetidoNaFonte",
                schema: "tributos",
                table: "NotasFiscaisServico",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemListaServico",
                schema: "tributos",
                table: "NotasFiscaisServico",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MunicipioIncidenciaIbge",
                schema: "tributos",
                table: "NotasFiscaisServico",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Situacao",
                schema: "tributos",
                table: "NotasFiscaisServico",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AliquotasItbi",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    AliquotaGeralPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    AliquotaSfhFinanciadaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AliquotasItbi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApuracoesIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    IssProprio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssRetido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssSubstituicao = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuracoesIss", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasAliquotaIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicioAaaaMm = table.Column<int>(type: "int", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasAliquotaIss", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransmissoesImobiliarias",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransmitenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdquirenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    ValorDeclarado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorVenalReferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseFoiValorVenal = table.Column<bool>(type: "bit", nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ImpostoDevido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransmissoesImobiliarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensApuracaoIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApuracaoIssId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ItemListaServico = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IssApurado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensApuracaoIss", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensApuracaoIss_ApuracoesIss_ApuracaoIssId",
                        column: x => x.ApuracaoIssId,
                        principalSchema: "tributos",
                        principalTable: "ApuracoesIss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensAliquotaIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabelaAliquotaIssId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemListaServico = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    RetencaoObrigatoria = table.Column<bool>(type: "bit", nullable: false),
                    SubstituicaoTributaria = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensAliquotaIss", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensAliquotaIss_TabelasAliquotaIss_TabelaAliquotaIssId",
                        column: x => x.TabelaAliquotaIssId,
                        principalSchema: "tributos",
                        principalTable: "TabelasAliquotaIss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscaisServico_TenantId_PrestadorCnpj_Competencia",
                schema: "tributos",
                table: "NotasFiscaisServico",
                columns: new[] { "TenantId", "PrestadorCnpj", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_AliquotasItbi_TenantId_Exercicio",
                schema: "tributos",
                table: "AliquotasItbi",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesIss_TenantId_ContribuinteId_Competencia",
                schema: "tributos",
                table: "ApuracoesIss",
                columns: new[] { "TenantId", "ContribuinteId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensAliquotaIss_TabelaAliquotaIssId",
                schema: "tributos",
                table: "ItensAliquotaIss",
                column: "TabelaAliquotaIssId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensApuracaoIss_ApuracaoIssId",
                schema: "tributos",
                table: "ItensApuracaoIss",
                column: "ApuracaoIssId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasAliquotaIss_TenantId_VigenciaInicioAaaaMm",
                schema: "tributos",
                table: "TabelasAliquotaIss",
                columns: new[] { "TenantId", "VigenciaInicioAaaaMm" });

            migrationBuilder.CreateIndex(
                name: "IX_TransmissoesImobiliarias_TenantId_ImovelId",
                schema: "tributos",
                table: "TransmissoesImobiliarias",
                columns: new[] { "TenantId", "ImovelId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AliquotasItbi",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ItensAliquotaIss",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ItensApuracaoIss",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TransmissoesImobiliarias",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TabelasAliquotaIss",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ApuracoesIss",
                schema: "tributos");

            migrationBuilder.DropIndex(
                name: "IX_NotasFiscaisServico_TenantId_PrestadorCnpj_Competencia",
                schema: "tributos",
                table: "NotasFiscaisServico");

            migrationBuilder.DropColumn(
                name: "IssRetidoNaFonte",
                schema: "tributos",
                table: "NotasFiscaisServico");

            migrationBuilder.DropColumn(
                name: "ItemListaServico",
                schema: "tributos",
                table: "NotasFiscaisServico");

            migrationBuilder.DropColumn(
                name: "MunicipioIncidenciaIbge",
                schema: "tributos",
                table: "NotasFiscaisServico");

            migrationBuilder.DropColumn(
                name: "Situacao",
                schema: "tributos",
                table: "NotasFiscaisServico");
        }
    }
}
