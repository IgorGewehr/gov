using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FolhaTabelasLegaisRubricas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rubricas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IncideInss = table.Column<bool>(type: "bit", nullable: false),
                    IncideRpps = table.Column<bool>(type: "bit", nullable: false),
                    IncideIrrf = table.Column<bool>(type: "bit", nullable: false),
                    IncideFgts = table.Column<bool>(type: "bit", nullable: false),
                    ValorFixo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Percentual = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubricas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasInss",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    Teto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseLegal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasInss", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasIrrf",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    DeducaoPorDependente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DescontoSimplificado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseLegal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasIrrf", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasRpps",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    Teto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseLegal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasRpps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasInssFaixas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LimiteInferior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LimiteSuperior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Aliquota = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    TabelaInssId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasInssFaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabelasInssFaixas_TabelasInss_TabelaInssId",
                        column: x => x.TabelaInssId,
                        principalSchema: "recursoshumanos",
                        principalTable: "TabelasInss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TabelasIrrfFaixas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LimiteSuperior = table.Column<decimal>(type: "decimal(28,2)", nullable: false),
                    Aliquota = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    ParcelaDeduzir = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TabelaIrrfId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasIrrfFaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabelasIrrfFaixas_TabelasIrrf_TabelaIrrfId",
                        column: x => x.TabelaIrrfId,
                        principalSchema: "recursoshumanos",
                        principalTable: "TabelasIrrf",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TabelasRppsFaixas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LimiteInferior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LimiteSuperior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Aliquota = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    TabelaRppsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasRppsFaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabelasRppsFaixas_TabelasRpps_TabelaRppsId",
                        column: x => x.TabelaRppsId,
                        principalSchema: "recursoshumanos",
                        principalTable: "TabelasRpps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rubricas_TenantId_Codigo",
                schema: "recursoshumanos",
                table: "Rubricas",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TabelasInss_TenantId_VigenciaInicio",
                schema: "recursoshumanos",
                table: "TabelasInss",
                columns: new[] { "TenantId", "VigenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TabelasInssFaixas_TabelaInssId",
                schema: "recursoshumanos",
                table: "TabelasInssFaixas",
                column: "TabelaInssId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasIrrf_TenantId_VigenciaInicio",
                schema: "recursoshumanos",
                table: "TabelasIrrf",
                columns: new[] { "TenantId", "VigenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TabelasIrrfFaixas_TabelaIrrfId",
                schema: "recursoshumanos",
                table: "TabelasIrrfFaixas",
                column: "TabelaIrrfId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasRpps_TenantId_VigenciaInicio",
                schema: "recursoshumanos",
                table: "TabelasRpps",
                columns: new[] { "TenantId", "VigenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TabelasRppsFaixas_TabelaRppsId",
                schema: "recursoshumanos",
                table: "TabelasRppsFaixas",
                column: "TabelaRppsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rubricas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasInssFaixas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasIrrfFaixas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasRppsFaixas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasInss",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasIrrf",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "TabelasRpps",
                schema: "recursoshumanos");
        }
    }
}
