using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Onda 3c-2 (Saude — Vigilancia Sanitaria): estabelecimentos sujeitos a VISA (ramo/risco), inspecoes/
    /// vistorias (roteiro/checklist + itens conformes/nao conformes + resultado derivado), autos de
    /// infracao/intimacao (prazos, defesa, multa) e licencas/alvaras sanitarios (emissao/validade/renovacao).
    /// Independente do PEP; operacao 100% local — SINAVISA/e-SUS VS = M10 atras de ACL.
    /// </summary>
    public partial class VigilanciaSanitaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- Estabelecimentos sujeitos a VISA ----------
            migrationBuilder.CreateTable(
                name: "EstabelecimentosFiscalizaveis",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoPersistido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ramo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Risco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_EstabelecimentosFiscalizaveis", x => x.Id));

            // ---------- Inspecoes/vistorias ----------
            migrationBuilder.CreateTable(
                name: "Inspecoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInspecao = table.Column<DateOnly>(type: "date", nullable: false),
                    FiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Roteiro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Inspecoes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "InspecoesItens",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Requisito = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Conformidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspecoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspecoesItens_Inspecoes_InspecaoId",
                        column: x => x.InspecaoId,
                        principalSchema: "saude",
                        principalTable: "Inspecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- Autos (infracao/intimacao) ----------
            migrationBuilder.CreateTable(
                name: "AutosVisa",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fundamentacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataLavratura = table.Column<DateOnly>(type: "date", nullable: false),
                    PrazoFinal = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorMulta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Defesa = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AutosVisa", x => x.Id));

            // ---------- Licencas/alvaras sanitarios ----------
            migrationBuilder.CreateTable(
                name: "LicencasSanitarias",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmitidaEm = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidadeAte = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LicencasSanitarias", x => x.Id));

            // ---------- indices ----------
            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentosFiscalizaveis_TenantId_DocumentoPersistido_RazaoSocial",
                schema: "saude",
                table: "EstabelecimentosFiscalizaveis",
                columns: new[] { "TenantId", "DocumentoPersistido", "RazaoSocial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentosFiscalizaveis_TenantId_Situacao",
                schema: "saude",
                table: "EstabelecimentosFiscalizaveis",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecoes_TenantId_EstabelecimentoFiscalizavelId",
                schema: "saude",
                table: "Inspecoes",
                columns: new[] { "TenantId", "EstabelecimentoFiscalizavelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecoes_TenantId_DataInspecao",
                schema: "saude",
                table: "Inspecoes",
                columns: new[] { "TenantId", "DataInspecao" });

            migrationBuilder.CreateIndex(
                name: "IX_InspecoesItens_InspecaoId",
                schema: "saude",
                table: "InspecoesItens",
                column: "InspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_Numero",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_Situacao",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_PrazoFinal",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "PrazoFinal" });

            migrationBuilder.CreateIndex(
                name: "IX_LicencasSanitarias_TenantId_EstabelecimentoFiscalizavelId",
                schema: "saude",
                table: "LicencasSanitarias",
                columns: new[] { "TenantId", "EstabelecimentoFiscalizavelId" });

            migrationBuilder.CreateIndex(
                name: "IX_LicencasSanitarias_TenantId_Situacao_ValidadeAte",
                schema: "saude",
                table: "LicencasSanitarias",
                columns: new[] { "TenantId", "Situacao", "ValidadeAte" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InspecoesItens", schema: "saude");
            migrationBuilder.DropTable(name: "AutosVisa", schema: "saude");
            migrationBuilder.DropTable(name: "LicencasSanitarias", schema: "saude");
            migrationBuilder.DropTable(name: "Inspecoes", schema: "saude");
            migrationBuilder.DropTable(name: "EstabelecimentosFiscalizaveis", schema: "saude");
        }
    }
}
