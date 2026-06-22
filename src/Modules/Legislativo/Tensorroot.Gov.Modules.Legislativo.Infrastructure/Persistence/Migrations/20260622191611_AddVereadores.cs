using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVereadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vereadores",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeCivil = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomeParlamentar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Partido = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LegislaturaInicio = table.Column<int>(type: "int", nullable: false),
                    LegislaturaFim = table.Column<int>(type: "int", nullable: false),
                    CargoMesa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vereadores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vereadores_TenantId_NomeParlamentar",
                schema: "legislativo",
                table: "Vereadores",
                columns: new[] { "TenantId", "NomeParlamentar" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vereadores",
                schema: "legislativo");
        }
    }
}
