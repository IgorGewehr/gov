using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FrotaPneusApolicesCondutores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apolices",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Seguradora = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroApolice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cobertura = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InicioVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    FimVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Premio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImportanciaSegurada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apolices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Condutores",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    NumeroCnh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Categorias = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ValidadeCnh = table.Column<DateOnly>(type: "date", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoSuspensao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Condutores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pneus",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroFogo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Medida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Dot = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    SulcoNovo = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    SulcoAtual = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    VidaUtilKmEstimada = table.Column<int>(type: "int", nullable: false),
                    ValorAquisicao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustoRecapagens = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataAquisicao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeiculoAtualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PosicaoAtual = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    OdometroInstalacao = table.Column<int>(type: "int", nullable: true),
                    KmAcumulado = table.Column<int>(type: "int", nullable: false),
                    Recapagens = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pneus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apolices_TenantId_FimVigencia",
                schema: "patrimonio",
                table: "Apolices",
                columns: new[] { "TenantId", "FimVigencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Apolices_TenantId_VeiculoId",
                schema: "patrimonio",
                table: "Apolices",
                columns: new[] { "TenantId", "VeiculoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Condutores_TenantId_NumeroCnh",
                schema: "patrimonio",
                table: "Condutores",
                columns: new[] { "TenantId", "NumeroCnh" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Condutores_TenantId_ValidadeCnh",
                schema: "patrimonio",
                table: "Condutores",
                columns: new[] { "TenantId", "ValidadeCnh" });

            migrationBuilder.CreateIndex(
                name: "IX_Pneus_TenantId_NumeroFogo",
                schema: "patrimonio",
                table: "Pneus",
                columns: new[] { "TenantId", "NumeroFogo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pneus_TenantId_VeiculoAtualId",
                schema: "patrimonio",
                table: "Pneus",
                columns: new[] { "TenantId", "VeiculoAtualId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apolices",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Condutores",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Pneus",
                schema: "patrimonio");
        }
    }
}
