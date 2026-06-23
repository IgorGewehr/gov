using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AfastamentosTipados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasNaoComputaveis",
                schema: "recursoshumanos",
                table: "Servidores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Afastamentos",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FimPrevisto = table.Column<DateOnly>(type: "date", nullable: true),
                    FimEfetivo = table.Column<DateOnly>(type: "date", nullable: true),
                    Documento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SuspendeProventos = table.Column<bool>(type: "bit", nullable: false),
                    PercentualRemuneracao = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiasPagosPeloEnte = table.Column<int>(type: "int", nullable: false),
                    ContaTempo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoEventoESocial = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afastamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasAfastamento",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    SuspendeProventos = table.Column<bool>(type: "bit", nullable: false),
                    PercentualRemuneracao = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiasPagosPeloEnte = table.Column<int>(type: "int", nullable: false),
                    ContaTempo = table.Column<bool>(type: "bit", nullable: false),
                    DuracaoPadraoDias = table.Column<int>(type: "int", nullable: true),
                    CodigoEventoESocial = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasAfastamento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afastamentos_TenantId_ServidorId_Situacao",
                schema: "recursoshumanos",
                table: "Afastamentos",
                columns: new[] { "TenantId", "ServidorId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Afastamentos_TenantId_Tipo",
                schema: "recursoshumanos",
                table: "Afastamentos",
                columns: new[] { "TenantId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_RegrasAfastamento_TenantId_Tipo_VigenciaInicio",
                schema: "recursoshumanos",
                table: "RegrasAfastamento",
                columns: new[] { "TenantId", "Tipo", "VigenciaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Afastamentos",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "RegrasAfastamento",
                schema: "recursoshumanos");

            migrationBuilder.DropColumn(
                name: "DiasNaoComputaveis",
                schema: "recursoshumanos",
                table: "Servidores");
        }
    }
}
