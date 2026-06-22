using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MscGerada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "msc_gerada",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    TipoMatriz = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadeLinhas = table.Column<int>(type: "int", nullable: false),
                    GeradaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_msc_gerada", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_msc_gerada_TenantId_Exercicio_Mes_TipoMatriz",
                schema: "financas",
                table: "msc_gerada",
                columns: ["TenantId", "Exercicio", "Mes", "TipoMatriz"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DropTable(name: "msc_gerada", schema: "financas");
    }
}
