using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataVerificacao",
                schema: "administracao",
                table: "LicitacoesHabilitacoes",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "administracao",
                table: "LicitacoesHabilitacoes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "administracao",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "administracao",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "administracao",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "administracao",
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
                schema: "administracao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "DataVerificacao",
                schema: "administracao",
                table: "LicitacoesHabilitacoes");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "administracao",
                table: "LicitacoesHabilitacoes");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "administracao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "administracao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "administracao",
                table: "AuditTrail");
        }
    }
}
