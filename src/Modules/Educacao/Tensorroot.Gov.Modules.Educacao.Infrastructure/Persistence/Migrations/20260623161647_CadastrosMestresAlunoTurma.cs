using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CadastrosMestresAlunoTurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alunos",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    CodigoInepAluno = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeMae = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomePai = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    NomeSocial = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Sexo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EnderecoBairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EnderecoCep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    EnderecoLogradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnderecoMunicipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EnderecoNumero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnderecoUf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alunos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turmas",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscolaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnoLetivo = table.Column<int>(type: "int", nullable: false),
                    Etapa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vagas = table.Column<int>(type: "int", nullable: false),
                    Matriculados = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turmas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlunosResponsaveis",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    Parentesco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ResponsavelFinanceiro = table.Column<bool>(type: "bit", nullable: false),
                    AutorizadoBuscar = table.Column<bool>(type: "bit", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlunosResponsaveis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlunosResponsaveis_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalSchema: "educacao",
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_TenantId_Cpf",
                schema: "educacao",
                table: "Alunos",
                columns: new[] { "TenantId", "Cpf" });

            migrationBuilder.CreateIndex(
                name: "IX_AlunosResponsaveis_AlunoId",
                schema: "educacao",
                table: "AlunosResponsaveis",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_TenantId_EscolaId_AnoLetivo",
                schema: "educacao",
                table: "Turmas",
                columns: new[] { "TenantId", "EscolaId", "AnoLetivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_TenantId_EscolaId_AnoLetivo_Serie_Turno",
                schema: "educacao",
                table: "Turmas",
                columns: new[] { "TenantId", "EscolaId", "AnoLetivo", "Serie", "Turno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlunosResponsaveis",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "Turmas",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "Alunos",
                schema: "educacao");
        }
    }
}
