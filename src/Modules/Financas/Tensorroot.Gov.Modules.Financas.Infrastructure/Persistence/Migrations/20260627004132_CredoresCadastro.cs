using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CredoresCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Credores",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Agencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContaNumero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Pix = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credores_TenantId_Documento",
                schema: "financas",
                table: "Credores",
                columns: new[] { "TenantId", "Documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credores_TenantId_Nome",
                schema: "financas",
                table: "Credores",
                columns: new[] { "TenantId", "Nome" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Credores",
                schema: "financas");
        }
    }
}
