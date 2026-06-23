using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Onda2_Consignacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consignatarias",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consignatarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RubricasConsignaveis",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrupoMargem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContaParaMargem = table.Column<bool>(type: "bit", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricasConsignaveis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContratosConsignacao",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsignatariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoRubrica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrupoMargem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroContratoExterno = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ValorParcela = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "int", nullable: false),
                    ParcelasPagas = table.Column<int>(type: "int", nullable: false),
                    DataAverbacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosConsignacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosMargem",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    PercentualGeral = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    PercentualCartaoConsignado = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    PercentualCartaoBeneficio = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosMargem", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consignatarias_TenantId_Cnpj",
                schema: "recursoshumanos",
                table: "Consignatarias",
                columns: new[] { "TenantId", "Cnpj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubricasConsignaveis_TenantId_Codigo",
                schema: "recursoshumanos",
                table: "RubricasConsignaveis",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContratosConsignacao_TenantId_ServidorId_Situacao",
                schema: "recursoshumanos",
                table: "ContratosConsignacao",
                columns: new[] { "TenantId", "ServidorId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosConsignacao_TenantId_ConsignatariaId",
                schema: "recursoshumanos",
                table: "ContratosConsignacao",
                columns: new[] { "TenantId", "ConsignatariaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosMargem_TenantId_VigenciaInicio",
                schema: "recursoshumanos",
                table: "ParametrosMargem",
                columns: new[] { "TenantId", "VigenciaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContratosConsignacao", schema: "recursoshumanos");
            migrationBuilder.DropTable(name: "Consignatarias", schema: "recursoshumanos");
            migrationBuilder.DropTable(name: "RubricasConsignaveis", schema: "recursoshumanos");
            migrationBuilder.DropTable(name: "ParametrosMargem", schema: "recursoshumanos");
        }
    }
}
