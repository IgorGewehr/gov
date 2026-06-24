using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// PARIDADE-PoC (Tributos): CND/CPEN (Certidão de regularidade fiscal — CTN arts. 205/206, SW-A3) e
    /// GIA mensal de ISS (declaração do prestador — SW-A10).
    /// <list type="bullet">
    /// <item><c>CertidoesRegularidadeFiscal</c>: ato imutável da certidão (tipo, número, validade, código
    /// de autenticação para conferência pública).</item>
    /// <item><c>DeclaracoesGiaIss</c> + <c>ItensGiaIss</c>: declaração mensal do prestador (serviços
    /// prestados, base, alíquota, ISS devido) — constitui o crédito do ISS próprio (CTN art. 150).</item>
    /// </list>
    /// Em SQLite (dev/testes) o schema vem do SchemaProvisioner/EnsureCreated; esta migration vale para
    /// o provedor SqlServer (produção). DbContext ctor inalterado.
    /// </summary>
    /// <inheritdoc />
    public partial class CndCpenGiaIss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertidoesRegularidadeFiscal",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    NomeContribuinte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InscricaoMunicipal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroSequencial = table.Column<long>(type: "bigint", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodigoAutenticacao = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertidoesRegularidadeFiscal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeclaracoesGiaIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataEntrega = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalServicos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssDevido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracoesGiaIss", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensGiaIss",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclaracaoGiaIssId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemListaServico = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    RetidoNaFonte = table.Column<bool>(type: "bit", nullable: false),
                    IssApurado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensGiaIss", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensGiaIss_DeclaracoesGiaIss_DeclaracaoGiaIssId",
                        column: x => x.DeclaracaoGiaIssId,
                        principalSchema: "tributos",
                        principalTable: "DeclaracoesGiaIss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertidoesRegularidadeFiscal_TenantId_Numero",
                schema: "tributos",
                table: "CertidoesRegularidadeFiscal",
                columns: new[] { "TenantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertidoesRegularidadeFiscal_TenantId_ContribuinteId",
                schema: "tributos",
                table: "CertidoesRegularidadeFiscal",
                columns: new[] { "TenantId", "ContribuinteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracoesGiaIss_TenantId_ContribuinteId_Competencia",
                schema: "tributos",
                table: "DeclaracoesGiaIss",
                columns: new[] { "TenantId", "ContribuinteId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensGiaIss_DeclaracaoGiaIssId",
                schema: "tributos",
                table: "ItensGiaIss",
                column: "DeclaracaoGiaIssId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CertidoesRegularidadeFiscal", schema: "tributos");
            migrationBuilder.DropTable(name: "ItensGiaIss", schema: "tributos");
            migrationBuilder.DropTable(name: "DeclaracoesGiaIss", schema: "tributos");
        }
    }
}
