using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Platform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0ConnectionStringWidthSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                schema: "plataforma",
                table: "Tenants",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                schema: "plataforma",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
