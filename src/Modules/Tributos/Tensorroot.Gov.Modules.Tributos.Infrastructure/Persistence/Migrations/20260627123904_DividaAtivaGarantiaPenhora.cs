using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DividaAtivaGarantiaPenhora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataGarantiaPenhora",
                schema: "tributos",
                table: "DividasAtivas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Garantida",
                schema: "tributos",
                table: "DividasAtivas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataGarantiaPenhora",
                schema: "tributos",
                table: "DividasAtivas");

            migrationBuilder.DropColumn(
                name: "Garantida",
                schema: "tributos",
                table: "DividasAtivas");
        }
    }
}
