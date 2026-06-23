using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequisicaoAlmoxarifado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidosRequisicao",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetorSolicitante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AprovadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataAprovacao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataAtendimento = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosRequisicao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PedidosRequisicaoItens",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadeSolicitada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeAtendida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PedidoRequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosRequisicaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosRequisicaoItens_PedidosRequisicao_PedidoRequisicaoId",
                        column: x => x.PedidoRequisicaoId,
                        principalSchema: "patrimonio",
                        principalTable: "PedidosRequisicao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosRequisicao_TenantId_Situacao",
                schema: "patrimonio",
                table: "PedidosRequisicao",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosRequisicao_TenantId_UnidadeId",
                schema: "patrimonio",
                table: "PedidosRequisicao",
                columns: new[] { "TenantId", "UnidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosRequisicaoItens_PedidoRequisicaoId",
                schema: "patrimonio",
                table: "PedidosRequisicaoItens",
                column: "PedidoRequisicaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosRequisicaoItens",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "PedidosRequisicao",
                schema: "patrimonio");
        }
    }
}
