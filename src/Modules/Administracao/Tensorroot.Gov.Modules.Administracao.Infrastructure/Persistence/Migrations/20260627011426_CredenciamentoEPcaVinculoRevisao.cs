using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CredenciamentoEPcaVinculoRevisao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContratacaoFonte",
                schema: "administracao",
                table: "PlanosContratacoesItens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContratacaoIdentificacao",
                schema: "administracao",
                table: "PlanosContratacoesItens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContratacaoReferenciaId",
                schema: "administracao",
                table: "PlanosContratacoesItens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRevisaoAtual",
                schema: "administracao",
                table: "PlanosContratacoes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroRevisao",
                schema: "administracao",
                table: "PlanosContratacoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Credenciamentos",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Hipotese = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroEdital = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaFim = table.Column<DateOnly>(type: "date", nullable: false),
                    FundamentacaoLegal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EtpId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermoReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumeroPncp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credenciamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CredenciamentosCredenciados",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInscricao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataCredenciamento = table.Column<DateOnly>(type: "date", nullable: true),
                    DataDescredenciamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CredenciamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredenciamentosCredenciados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredenciamentosCredenciados_Credenciamentos_CredenciamentoId",
                        column: x => x.CredenciamentoId,
                        principalSchema: "administracao",
                        principalTable: "Credenciamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CredenciamentosItens",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    ItemCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrecoFixado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CredenciamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredenciamentosItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredenciamentosItens_Credenciamentos_CredenciamentoId",
                        column: x => x.CredenciamentoId,
                        principalSchema: "administracao",
                        principalTable: "Credenciamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credenciamentos_TenantId_Situacao",
                schema: "administracao",
                table: "Credenciamentos",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_CredenciamentosCredenciados_CredenciamentoId",
                schema: "administracao",
                table: "CredenciamentosCredenciados",
                column: "CredenciamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CredenciamentosCredenciados_FornecedorId",
                schema: "administracao",
                table: "CredenciamentosCredenciados",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CredenciamentosItens_CredenciamentoId",
                schema: "administracao",
                table: "CredenciamentosItens",
                column: "CredenciamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredenciamentosCredenciados",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "CredenciamentosItens",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Credenciamentos",
                schema: "administracao");

            migrationBuilder.DropColumn(
                name: "ContratacaoFonte",
                schema: "administracao",
                table: "PlanosContratacoesItens");

            migrationBuilder.DropColumn(
                name: "ContratacaoIdentificacao",
                schema: "administracao",
                table: "PlanosContratacoesItens");

            migrationBuilder.DropColumn(
                name: "ContratacaoReferenciaId",
                schema: "administracao",
                table: "PlanosContratacoesItens");

            migrationBuilder.DropColumn(
                name: "MotivoRevisaoAtual",
                schema: "administracao",
                table: "PlanosContratacoes");

            migrationBuilder.DropColumn(
                name: "NumeroRevisao",
                schema: "administracao",
                table: "PlanosContratacoes");
        }
    }
}
