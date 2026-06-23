using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CadastroEstabelecimentoProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estabelecimentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cnes = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estabelecimentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Cns = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Registro = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfissionaisVinculos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cbo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionaisVinculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionaisVinculos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalSchema: "saude",
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_TenantId_Cnes",
                schema: "saude",
                table: "Estabelecimentos",
                columns: new[] { "TenantId", "Cnes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_TenantId_Nome",
                schema: "saude",
                table: "Estabelecimentos",
                columns: new[] { "TenantId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_TenantId_Cpf",
                schema: "saude",
                table: "Profissionais",
                columns: new[] { "TenantId", "Cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_TenantId_Nome",
                schema: "saude",
                table: "Profissionais",
                columns: new[] { "TenantId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionaisVinculos_ProfissionalId_EstabelecimentoId",
                schema: "saude",
                table: "ProfissionaisVinculos",
                columns: new[] { "ProfissionalId", "EstabelecimentoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfissionaisVinculos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Estabelecimentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Profissionais",
                schema: "saude");
        }
    }
}
