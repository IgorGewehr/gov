using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail");

            migrationBuilder.AlterColumn<string>(
                name: "RespostaTexto",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RecursoDecisao",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail");

            migrationBuilder.AlterColumn<string>(
                name: "RespostaTexto",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                type: "nvarchar(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RecursoDecisao",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                type: "nvarchar(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });
        }
    }
}
