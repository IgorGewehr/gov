using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PontoColeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NsrEquipamento",
                schema: "recursoshumanos",
                table: "PontoMarcacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepId",
                schema: "recursoshumanos",
                table: "PontoMarcacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PontoReps",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificacaoEquipamento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Modos = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EnderecoOuReferencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferenciaCredencialCofre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UltimoNsrColetado = table.Column<long>(type: "bigint", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoReps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PontoMarcacoes_TenantId_RepId_NsrEquipamento",
                schema: "recursoshumanos",
                table: "PontoMarcacoes",
                columns: new[] { "TenantId", "RepId", "NsrEquipamento" },
                unique: true,
                filter: "[RepId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PontoReps_TenantId_Ativo",
                schema: "recursoshumanos",
                table: "PontoReps",
                columns: new[] { "TenantId", "Ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PontoReps",
                schema: "recursoshumanos");

            migrationBuilder.DropIndex(
                name: "IX_PontoMarcacoes_TenantId_RepId_NsrEquipamento",
                schema: "recursoshumanos",
                table: "PontoMarcacoes");

            migrationBuilder.DropColumn(
                name: "NsrEquipamento",
                schema: "recursoshumanos",
                table: "PontoMarcacoes");

            migrationBuilder.DropColumn(
                name: "RepId",
                schema: "recursoshumanos",
                table: "PontoMarcacoes");
        }
    }
}
