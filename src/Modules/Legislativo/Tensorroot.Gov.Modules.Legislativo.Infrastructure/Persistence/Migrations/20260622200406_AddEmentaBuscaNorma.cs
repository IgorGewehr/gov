using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmentaBuscaNorma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmentaBusca",
                schema: "legislativo",
                table: "Normas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Normas_TenantId_EmentaBusca",
                schema: "legislativo",
                table: "Normas",
                columns: new[] { "TenantId", "EmentaBusca" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Normas_TenantId_EmentaBusca",
                schema: "legislativo",
                table: "Normas");

            migrationBuilder.DropColumn(
                name: "EmentaBusca",
                schema: "legislativo",
                table: "Normas");
        }
    }
}
