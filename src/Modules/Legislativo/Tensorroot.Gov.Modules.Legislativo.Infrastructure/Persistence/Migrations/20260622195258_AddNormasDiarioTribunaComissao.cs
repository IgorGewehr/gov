using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormasDiarioTribunaComissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comissoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comissoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiarioEdicoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataPublicacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HashConteudo = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EdicaoOriginalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiarioEdicoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Normas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Ementa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataPromulgacao = table.Column<DateOnly>(type: "date", nullable: false),
                    TextoArticulado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposicaoOrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SituacaoVigencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataRevogacao = table.Column<DateOnly>(type: "date", nullable: true),
                    NormaRevogadoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Normas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tribunas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TempoPadraoOrador = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tribunas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComissoesMembros",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VereadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComissaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComissoesMembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComissoesMembros_Comissoes_ComissaoOwnerId",
                        column: x => x.ComissaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Comissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiarioMaterias",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    EdicaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiarioMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiarioMaterias_DiarioEdicoes_EdicaoOwnerId",
                        column: x => x.EdicaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "DiarioEdicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NormasHistoricoVigencia",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    NormaReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormaOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormasHistoricoVigencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NormasHistoricoVigencia_Normas_NormaOwnerId",
                        column: x => x.NormaOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Normas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TribunaInscricoes",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VereadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TempoConcedido = table.Column<TimeSpan>(type: "time", nullable: false),
                    IniciadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EncerradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PausaIniciadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Apartes = table.Column<int>(type: "int", nullable: false),
                    TribunaOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TribunaInscricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TribunaInscricoes_Tribunas_TribunaOwnerId",
                        column: x => x.TribunaOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Tribunas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TribunaPausas",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InscricaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TribunaPausas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TribunaPausas_TribunaInscricoes_InscricaoOwnerId",
                        column: x => x.InscricaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "TribunaInscricoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comissoes_TenantId_Situacao",
                schema: "legislativo",
                table: "Comissoes",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesMembros_ComissaoOwnerId",
                schema: "legislativo",
                table: "ComissoesMembros",
                column: "ComissaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DiarioEdicoes_TenantId_Ano_Numero",
                schema: "legislativo",
                table: "DiarioEdicoes",
                columns: new[] { "TenantId", "Ano", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiarioEdicoes_TenantId_Situacao",
                schema: "legislativo",
                table: "DiarioEdicoes",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_DiarioMaterias_EdicaoOwnerId",
                schema: "legislativo",
                table: "DiarioMaterias",
                column: "EdicaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Normas_TenantId_SituacaoVigencia",
                schema: "legislativo",
                table: "Normas",
                columns: new[] { "TenantId", "SituacaoVigencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Normas_TenantId_Tipo_Numero_Ano",
                schema: "legislativo",
                table: "Normas",
                columns: new[] { "TenantId", "Tipo", "Numero", "Ano" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormasHistoricoVigencia_NormaOwnerId",
                schema: "legislativo",
                table: "NormasHistoricoVigencia",
                column: "NormaOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TribunaInscricoes_TribunaOwnerId",
                schema: "legislativo",
                table: "TribunaInscricoes",
                column: "TribunaOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TribunaPausas_InscricaoOwnerId",
                schema: "legislativo",
                table: "TribunaPausas",
                column: "InscricaoOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tribunas_TenantId_SessaoId",
                schema: "legislativo",
                table: "Tribunas",
                columns: new[] { "TenantId", "SessaoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComissoesMembros",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "DiarioMaterias",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "NormasHistoricoVigencia",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "TribunaPausas",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Comissoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "DiarioEdicoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Normas",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "TribunaInscricoes",
                schema: "legislativo");

            migrationBuilder.DropTable(
                name: "Tribunas",
                schema: "legislativo");
        }
    }
}
