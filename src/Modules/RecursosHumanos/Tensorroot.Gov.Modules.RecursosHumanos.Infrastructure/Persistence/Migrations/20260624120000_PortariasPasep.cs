using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortariasPasep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Portarias",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Sequencial = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataAto = table.Column<DateOnly>(type: "date", nullable: false),
                    Ementa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoRevogacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApuracoesPasep",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    BaseContribuicao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Aliquota = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuracoesPasep", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Portarias_TenantId_Exercicio_Sequencial",
                schema: "recursoshumanos",
                table: "Portarias",
                columns: new[] { "TenantId", "Exercicio", "Sequencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portarias_TenantId_Tipo_Situacao",
                schema: "recursoshumanos",
                table: "Portarias",
                columns: new[] { "TenantId", "Tipo", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Portarias_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "Portarias",
                columns: new[] { "TenantId", "ServidorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesPasep_TenantId_Competencia",
                schema: "recursoshumanos",
                table: "ApuracoesPasep",
                columns: new[] { "TenantId", "Competencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Portarias",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "ApuracoesPasep",
                schema: "recursoshumanos");
        }
    }
}
