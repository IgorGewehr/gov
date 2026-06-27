using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CertidaoTempoServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertidoesTempoServico",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Sequencial = table.Column<int>(type: "int", nullable: false),
                    Finalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    OrgaoEmissor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FinalidadeDescrita = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoAutenticacao = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MotivoAnulacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertidoesTempoServico", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertidoesTempoServicoPeriodos",
                schema: "recursoshumanos",
                columns: table => new
                {
                    CertidaoTempoServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    DiasNaoComputaveis = table.Column<int>(type: "int", nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fator = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RegimeOrigem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertidoesTempoServicoPeriodos", x => new { x.CertidaoTempoServicoId, x.Id });
                    table.ForeignKey(
                        name: "FK_CertidoesTempoServicoPeriodos_CertidoesTempoServico_CertidaoTempoServicoId",
                        column: x => x.CertidaoTempoServicoId,
                        principalSchema: "recursoshumanos",
                        principalTable: "CertidoesTempoServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertidoesTempoServico_TenantId_CodigoAutenticacao",
                schema: "recursoshumanos",
                table: "CertidoesTempoServico",
                columns: new[] { "TenantId", "CodigoAutenticacao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertidoesTempoServico_TenantId_Exercicio_Sequencial",
                schema: "recursoshumanos",
                table: "CertidoesTempoServico",
                columns: new[] { "TenantId", "Exercicio", "Sequencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertidoesTempoServico_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "CertidoesTempoServico",
                columns: new[] { "TenantId", "ServidorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertidoesTempoServicoPeriodos",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "CertidoesTempoServico",
                schema: "recursoshumanos");
        }
    }
}
