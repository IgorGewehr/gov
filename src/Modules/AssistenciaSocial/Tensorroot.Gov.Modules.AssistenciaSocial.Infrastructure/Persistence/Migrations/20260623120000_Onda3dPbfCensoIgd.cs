using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Onda 3d (Assistencia): PBF — acompanhamento de condicionalidades (3d.1), Censo SUAS —
    /// unidades socioassistenciais + formulario consolidado (3d.2). A EstimativaIgd (3d.3) e uma
    /// query/servico de dominio sobre dados existentes — nao cria tabela.
    /// </summary>
    public partial class Onda3dPbfCensoIgd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 3d.1: acompanhamento de condicionalidades do PBF.
            migrationBuilder.CreateTable(
                name: "AcompanhamentosCondicionalidade",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    Efeito = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcompanhamentosCondicionalidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCondicionalidade",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcompanhamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MembroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCondicionalidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosCondicionalidade_AcompanhamentosCondicionalidade_AcompanhamentoId",
                        column: x => x.AcompanhamentoId,
                        principalSchema: "assistenciasocial",
                        principalTable: "AcompanhamentosCondicionalidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3d.2: unidades socioassistenciais + servicos + formulario do Censo.
            migrationBuilder.CreateTable(
                name: "UnidadesSocioassistenciais",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TerritorioCobertura = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Endereco = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    QuantidadeProfissionais = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesSocioassistenciais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicosOfertados",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Servico = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapacidadeMensal = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicosOfertados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicosOfertados_UnidadesSocioassistenciais_UnidadeId",
                        column: x => x.UnidadeId,
                        principalSchema: "assistenciasocial",
                        principalTable: "UnidadesSocioassistenciais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormulariosCensoSuas",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuantidadeProfissionais = table.Column<int>(type: "int", nullable: false),
                    QuantidadeServicosOfertados = table.Column<int>(type: "int", nullable: false),
                    FamiliasReferenciadas = table.Column<int>(type: "int", nullable: false),
                    VolumeAtendimentosAno = table.Column<int>(type: "int", nullable: false),
                    FechadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulariosCensoSuas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcompCondic_Tenant_Familia_Competencia",
                schema: "assistenciasocial",
                table: "AcompanhamentosCondicionalidade",
                columns: new[] { "TenantId", "FamiliaId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCondicionalidade_AcompanhamentoId",
                schema: "assistenciasocial",
                table: "RegistrosCondicionalidade",
                column: "AcompanhamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosOfertados_UnidadeId_Servico",
                schema: "assistenciasocial",
                table: "ServicosOfertados",
                columns: new[] { "UnidadeId", "Servico" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesSocioassistenciais_TenantId",
                schema: "assistenciasocial",
                table: "UnidadesSocioassistenciais",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Censo_Tenant_Unidade_Exercicio",
                schema: "assistenciasocial",
                table: "FormulariosCensoSuas",
                columns: new[] { "TenantId", "UnidadeId", "Exercicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosCondicionalidade",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "ServicosOfertados",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "FormulariosCensoSuas",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "AcompanhamentosCondicionalidade",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "UnidadesSocioassistenciais",
                schema: "assistenciasocial");
        }
    }
}
