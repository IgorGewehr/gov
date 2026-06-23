using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NavegabilidadeBuscaPatrimonio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlacaBusca",
                schema: "patrimonio",
                table: "Veiculos",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenavamBusca",
                schema: "patrimonio",
                table: "Veiculos",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TombamentoBusca",
                schema: "patrimonio",
                table: "Bens",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "patrimonio",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "patrimonio",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "patrimonio",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "VeiculosHistoricosDepreciacao",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorDepreciado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorContabilResultante = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosHistoricosDepreciacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosHistoricosDepreciacao_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_TenantId_PlacaBusca",
                schema: "patrimonio",
                table: "Veiculos",
                columns: new[] { "TenantId", "PlacaBusca" });

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_TenantId_RenavamBusca",
                schema: "patrimonio",
                table: "Veiculos",
                columns: new[] { "TenantId", "RenavamBusca" });

            migrationBuilder.CreateIndex(
                name: "IX_Bens_TenantId_TombamentoBusca",
                schema: "patrimonio",
                table: "Bens",
                columns: new[] { "TenantId", "TombamentoBusca" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "patrimonio",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosHistoricosDepreciacao_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosHistoricosDepreciacao",
                column: "VeiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeiculosHistoricosDepreciacao",
                schema: "patrimonio");

            migrationBuilder.DropIndex(
                name: "IX_Veiculos_TenantId_PlacaBusca",
                schema: "patrimonio",
                table: "Veiculos");

            migrationBuilder.DropIndex(
                name: "IX_Veiculos_TenantId_RenavamBusca",
                schema: "patrimonio",
                table: "Veiculos");

            migrationBuilder.DropIndex(
                name: "IX_Bens_TenantId_TombamentoBusca",
                schema: "patrimonio",
                table: "Bens");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "patrimonio",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "PlacaBusca",
                schema: "patrimonio",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "RenavamBusca",
                schema: "patrimonio",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "TombamentoBusca",
                schema: "patrimonio",
                table: "Bens");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "patrimonio",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "patrimonio",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "patrimonio",
                table: "AuditTrail");
        }
    }
}
