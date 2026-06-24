using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoAtaPca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Atas",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    LicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogoItens",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadeFornecimento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Classe = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogoItens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanosContratacoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroPncp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosContratacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtasAdesoes",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgaoAderente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    AtaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtasAdesoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtasAdesoes_Atas_AtaId",
                        column: x => x.AtaId,
                        principalSchema: "administracao",
                        principalTable: "Atas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtasItens",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorBeneficiarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecoRegistrado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeRegistrada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantidadeContratada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AtaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtasItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtasItens_Atas_AtaId",
                        column: x => x.AtaId,
                        principalSchema: "administracao",
                        principalTable: "Atas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanosContratacoesItens",
                schema: "administracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrimestreDesejado = table.Column<int>(type: "int", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PlanoContratacoesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosContratacoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosContratacoesItens_PlanosContratacoes_PlanoContratacoesId",
                        column: x => x.PlanoContratacoesId,
                        principalSchema: "administracao",
                        principalTable: "PlanosContratacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Atas_TenantId_Numero",
                schema: "administracao",
                table: "Atas",
                columns: new[] { "TenantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atas_TenantId_Situacao",
                schema: "administracao",
                table: "Atas",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_AtasAdesoes_AtaId",
                schema: "administracao",
                table: "AtasAdesoes",
                column: "AtaId");

            migrationBuilder.CreateIndex(
                name: "IX_AtasItens_AtaId",
                schema: "administracao",
                table: "AtasItens",
                column: "AtaId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogoItens_TenantId_Codigo",
                schema: "administracao",
                table: "CatalogoItens",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogoItens_TenantId_Natureza_Situacao",
                schema: "administracao",
                table: "CatalogoItens",
                columns: new[] { "TenantId", "Natureza", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContratacoes_TenantId_Exercicio",
                schema: "administracao",
                table: "PlanosContratacoes",
                columns: new[] { "TenantId", "Exercicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContratacoesItens_PlanoContratacoesId",
                schema: "administracao",
                table: "PlanosContratacoesItens",
                column: "PlanoContratacoesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtasAdesoes",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "AtasItens",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "CatalogoItens",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "PlanosContratacoesItens",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "Atas",
                schema: "administracao");

            migrationBuilder.DropTable(
                name: "PlanosContratacoes",
                schema: "administracao");
        }
    }
}
