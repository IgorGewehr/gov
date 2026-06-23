using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VotoUnicoPorVereador_BuscaNormalizada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VotacoesVotos_VotacaoOwnerId",
                schema: "legislativo",
                table: "VotacoesVotos");

            migrationBuilder.AddColumn<bool>(
                name: "ParecerContrarioCcjSuperado",
                schema: "legislativo",
                table: "Proposicoes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "legislativo",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "legislativo",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "legislativo",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_VotacoesVotos_VotacaoOwnerId_VereadorId",
                schema: "legislativo",
                table: "VotacoesVotos",
                columns: new[] { "VotacaoOwnerId", "VereadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "legislativo",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VotacoesVotos_VotacaoOwnerId_VereadorId",
                schema: "legislativo",
                table: "VotacoesVotos");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "legislativo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "ParecerContrarioCcjSuperado",
                schema: "legislativo",
                table: "Proposicoes");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "legislativo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "legislativo",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "legislativo",
                table: "AuditTrail");

            migrationBuilder.CreateIndex(
                name: "IX_VotacoesVotos_VotacaoOwnerId",
                schema: "legislativo",
                table: "VotacoesVotos",
                column: "VotacaoOwnerId");
        }
    }
}
