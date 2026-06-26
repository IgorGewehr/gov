using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParidadeSapiDesIfSimDomicilio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeclaracoesDesif",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceitaTributavelTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssqnDevidoBruto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeducoesReceita = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncentivosFiscais = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositosJudiciais = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssqnARecolher = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataEntrega = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracoesDesif", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomiciliosEletronicos",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataAdesao = table.Column<DateOnly>(type: "date", nullable: false),
                    DiasCienciaTacita = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomiciliosEletronicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitulosRegistroSim",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazaoSocialEstabelecimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnderecoEstabelecimento = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DataRequerimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroSim = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DataRegistro = table.Column<DateOnly>(type: "date", nullable: true),
                    FimVigencia = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitulosRegistroSim", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubtitulosDesif",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclaracaoDesifId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaCosif = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoTributacaoDesif = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemListaServico = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BaseCalculo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    IssqnDevido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubtitulosDesif", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubtitulosDesif_DeclaracoesDesif_DeclaracaoDesifId",
                        column: x => x.DeclaracaoDesifId,
                        principalSchema: "tributos",
                        principalTable: "DeclaracoesDesif",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MensagensFiscais",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomicilioEletronicoContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Corpo = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ReferenciaExterna = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DataDisponibilizacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataLimiteCienciaTacita = table.Column<DateOnly>(type: "date", nullable: false),
                    DiasPrazoManifestacao = table.Column<int>(type: "int", nullable: false),
                    Forma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataCiencia = table.Column<DateOnly>(type: "date", nullable: true),
                    DataLimiteManifestacao = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensFiscais_DomiciliosEletronicos_DomicilioEletronicoContribuinteId",
                        column: x => x.DomicilioEletronicoContribuinteId,
                        principalSchema: "tributos",
                        principalTable: "DomiciliosEletronicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosInspecionadosSim",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TituloRegistroSimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Denominacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Classificacao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NumeroRotulo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosInspecionadosSim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosInspecionadosSim_TitulosRegistroSim_TituloRegistroSimId",
                        column: x => x.TituloRegistroSimId,
                        principalSchema: "tributos",
                        principalTable: "TitulosRegistroSim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracoesDesif_TenantId_ContribuinteId_Competencia",
                schema: "tributos",
                table: "DeclaracoesDesif",
                columns: new[] { "TenantId", "ContribuinteId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_DomiciliosEletronicos_TenantId_ContribuinteId",
                schema: "tributos",
                table: "DomiciliosEletronicos",
                columns: new[] { "TenantId", "ContribuinteId" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensFiscais_DomicilioEletronicoContribuinteId",
                schema: "tributos",
                table: "MensagensFiscais",
                column: "DomicilioEletronicoContribuinteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosInspecionadosSim_TituloRegistroSimId",
                schema: "tributos",
                table: "ProdutosInspecionadosSim",
                column: "TituloRegistroSimId");

            migrationBuilder.CreateIndex(
                name: "IX_SubtitulosDesif_DeclaracaoDesifId",
                schema: "tributos",
                table: "SubtitulosDesif",
                column: "DeclaracaoDesifId");

            migrationBuilder.CreateIndex(
                name: "IX_TitulosRegistroSim_TenantId_NumeroSim",
                schema: "tributos",
                table: "TitulosRegistroSim",
                columns: new[] { "TenantId", "NumeroSim" });

            migrationBuilder.CreateIndex(
                name: "IX_TitulosRegistroSim_TenantId_ResponsavelId",
                schema: "tributos",
                table: "TitulosRegistroSim",
                columns: new[] { "TenantId", "ResponsavelId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensFiscais",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "ProdutosInspecionadosSim",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "SubtitulosDesif",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "DomiciliosEletronicos",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TitulosRegistroSim",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "DeclaracoesDesif",
                schema: "tributos");
        }
    }
}
