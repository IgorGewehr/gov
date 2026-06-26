using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DispensaEletronica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dispensas",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Fundamento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CriterioJulgamento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LimiteLegalVigente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LimiteLegalNormaFonte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EtpId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermoReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroAviso = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    AberturaDisputa = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CotacaoVencedoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumeroPncp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispensas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispensasCotacoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Sequencia = table.Column<long>(type: "bigint", nullable: false),
                    Classificacao = table.Column<int>(type: "int", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DispensaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensasCotacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensasCotacoes_Dispensas_DispensaId",
                        column: x => x.DispensaId,
                        principalSchema: "administracao",
                        principalTable: "Dispensas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispensasItens",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    ItemCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorUnitarioEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DispensaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensasItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensasItens_Dispensas_DispensaId",
                        column: x => x.DispensaId,
                        principalSchema: "administracao",
                        principalTable: "Dispensas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispensas_TenantId_Situacao",
                schema: "administracao",
                table: "Dispensas",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_DispensasCotacoes_DispensaId",
                schema: "administracao",
                table: "DispensasCotacoes",
                column: "DispensaId");

            migrationBuilder.CreateIndex(
                name: "IX_DispensasItens_DispensaId",
                schema: "administracao",
                table: "DispensasItens",
                column: "DispensaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispensasCotacoes",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "DispensasItens",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Dispensas",
                schema: "administracao");
        }
    }
}
