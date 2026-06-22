using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Ponto671 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PontoApuracoes",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    MinutosTrabalhados = table.Column<int>(type: "int", nullable: false),
                    MinutosDevidos = table.Column<int>(type: "int", nullable: false),
                    MinutosExtras = table.Column<int>(type: "int", nullable: false),
                    MinutosFalta = table.Column<int>(type: "int", nullable: false),
                    SaldoBancoHorasAnteriorMinutos = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DataFechamento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoApuracoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PontoJornadas",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargaDiariaMinutos = table.Column<int>(type: "int", nullable: false),
                    IntervaloMinutos = table.Column<int>(type: "int", nullable: false),
                    ToleranciaMinutos = table.Column<int>(type: "int", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoJornadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PontoMarcacoes",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Nsr = table.Column<long>(type: "bigint", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Sentido = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoMarcacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PontoApuracoes_TenantId_ServidorId_Competencia",
                schema: "recursoshumanos",
                table: "PontoApuracoes",
                columns: new[] { "TenantId", "ServidorId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PontoJornadas_TenantId_ServidorId_Ativa",
                schema: "recursoshumanos",
                table: "PontoJornadas",
                columns: new[] { "TenantId", "ServidorId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_PontoMarcacoes_TenantId_Nsr",
                schema: "recursoshumanos",
                table: "PontoMarcacoes",
                columns: new[] { "TenantId", "Nsr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PontoMarcacoes_TenantId_ServidorId_DataHora",
                schema: "recursoshumanos",
                table: "PontoMarcacoes",
                columns: new[] { "TenantId", "ServidorId", "DataHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PontoApuracoes",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "PontoJornadas",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "PontoMarcacoes",
                schema: "recursoshumanos");
        }
    }
}
